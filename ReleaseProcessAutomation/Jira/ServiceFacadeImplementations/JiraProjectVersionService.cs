// Copyright (c) rubicon IT GmbH, www.rubicon.eu
//
// See the NOTICE file distributed with this work for additional information
// regarding copyright ownership.  rubicon licenses this file to you under 
// the Apache License, Version 2.0 (the "License"); you may not use this 
// file except in compliance with the License.  You may obtain a copy of the 
// License at
//
//   http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software 
// distributed under the License is distributed on an "AS IS" BASIS, WITHOUT 
// WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.  See the 
// License for the specific language governing permissions and limitations
// under the License.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using ReleaseProcessAutomation.Jira.ServiceFacadeInterfaces;
using ReleaseProcessAutomation.Jira.Utility;
using ReleaseProcessAutomation.SemanticVersioning;
using RestSharp;

namespace ReleaseProcessAutomation.Jira.ServiceFacadeImplementations;

public class JiraProjectVersionService : IJiraProjectVersionService
{
  private readonly IJiraRestClientProvider _jiraRestClientProvider;
  private readonly IJiraIssueService _jiraIssueService;
  private readonly IJiraProjectVersionFinder _jiraProjectVersionFinder;

  public JiraProjectVersionService (
      IJiraRestClientProvider jiraRestClientProvider,
      IJiraIssueService jiraIssueService,
      IJiraProjectVersionFinder jiraProjectVersionFinder)
  {
    _jiraRestClientProvider = jiraRestClientProvider;
    _jiraIssueService = jiraIssueService;
    _jiraProjectVersionFinder = jiraProjectVersionFinder;
  }

  public string CreateVersion (string projectKey, string versionName, DateTime? releaseDate)
  {
    var request = _jiraRestClientProvider.GetJiraRestClient().CreateRestRequest("version", Method.POST);

    if (releaseDate != null)
      releaseDate = AdjustReleaseDateForJira(releaseDate.Value);

    var projectVersion = new JiraProjectVersion { name = versionName, project = projectKey, releaseDate = releaseDate };
    request.AddBody(projectVersion);

    var newProjectVersion = _jiraRestClientProvider.GetJiraRestClient().DoRequest<JiraProjectVersion>(request, HttpStatusCode.Created);

    return newProjectVersion.Data.id ?? throw new InvalidOperationException("The created version id was not assigned.");
  }

  public string CreateSubsequentVersion (string projectKey, string versionPattern, int versionComponentToIncrement, DayOfWeek versionReleaseWeekday)
  {
    // Determine next version name
    var lastUnreleasedVersion = _jiraProjectVersionFinder.FindUnreleasedVersions(projectKey, versionPattern).Last();
    if (lastUnreleasedVersion.name == null)
      throw new InvalidOperationException("The last found unreleased version did not have a name assigned.");

    var nextVersionName = IncrementVersion(lastUnreleasedVersion.name, versionComponentToIncrement);

    // Determine next release day
    if (lastUnreleasedVersion.releaseDate == null)
      throw new JiraException("releaseDate of lastUnreleasedVersion must have a value but is null");

    var nextReleaseDay = lastUnreleasedVersion.releaseDate.Value.AddDays(1);
    while (nextReleaseDay.DayOfWeek != versionReleaseWeekday)
      nextReleaseDay = nextReleaseDay.AddDays(1);

    var newVersionId = CreateVersion(projectKey, nextVersionName, nextReleaseDay);
    MoveVersion(
        newVersionId,
        lastUnreleasedVersion.self ?? throw new InvalidOperationException("The last found unreleased version did not have a 'self' assigned."));

    return newVersionId;
  }

  public void MoveVersion (string versionId, string afterVersionUrl)
  {
    var request = _jiraRestClientProvider.GetJiraRestClient().CreateRestRequest($"version/{versionId}/move", Method.POST);

    request.AddBody(new { after = afterVersionUrl });

    _jiraRestClientProvider.GetJiraRestClient().DoRequest(request, HttpStatusCode.OK);
  }

  public void MoveVersionByPosition (string versionId, string position)
  {
    var request = _jiraRestClientProvider.GetJiraRestClient().CreateRestRequest($"version/{versionId}/move", Method.POST);

    request.AddBody(new { position = position });

    _jiraRestClientProvider.GetJiraRestClient().DoRequest(request, HttpStatusCode.OK);
  }

  public JiraProjectVersion GetVersionById (string versionId)
  {
    var resource = $"version/{versionId}";
    var request = _jiraRestClientProvider.GetJiraRestClient().CreateRestRequest(resource, Method.GET);

    var response = _jiraRestClientProvider.GetJiraRestClient().DoRequest<JiraProjectVersion>(request, HttpStatusCode.OK);
    return response.Data;
  }

  private string IncrementVersion (string version, int componentToIncrement)
  {
    if (componentToIncrement < 1 || componentToIncrement > 4)
      throw new ArgumentException("componentToIncrement must be between 1 and 4");

    var versionParts = version.Split('.').Select(int.Parse).ToArray();
    if (versionParts.Length < componentToIncrement)
      throw new ArgumentException(string.Format("version must have at least {0} components", componentToIncrement));

    ++versionParts[componentToIncrement - 1];
    return versionParts.Select(p => p.ToString()).Aggregate((l, r) => l + "." + r);
  }

  public void ReleaseVersionAndSquashUnreleased (string versionID, string nextVersionID, string projectKey)
  {
    if (versionID != nextVersionID)
    {
      var versions = _jiraProjectVersionFinder.GetVersions(projectKey);

      var _semanticVersionParser = new SemanticVersionParser();
      var versionList = new List<JiraProjectVersionSemVerAdapter>();

      foreach (var version in versions)
        try
        {
          versionList.Add(
              new JiraProjectVersionSemVerAdapter()
              {
                  JiraProjectVersion =
                      version ?? throw new InvalidOperationException("The version did not have a proper jira project version assigned."),
                  SemanticVersion = _semanticVersionParser.ParseVersion(
                      version.name ?? throw new InvalidOperationException("The version did not have a name assigned."))
              });
        }
        catch (ArgumentException)
        {
          //Empty Catch. Invalid versions are not interesting for us
        }

      var currentVersion = versionList.Single(v => v.JiraProjectVersion.id == versionID).JiraProjectVersion;
      var nextVersion = versionList.Single(v => v.JiraProjectVersion.id == nextVersionID).JiraProjectVersion;

      var orderedVersions = versionList.OrderBy(x => x.SemanticVersion).ToList();
      var currentVersionIndex = orderedVersions.IndexOf(orderedVersions.Single(x => x.JiraProjectVersion.id == versionID));
      var nextVersionIndex = orderedVersions.IndexOf(orderedVersions.Single(x => x.JiraProjectVersion.id == nextVersionID));

      //There are versions between the currentVersion and the next version
      if (nextVersionIndex != currentVersionIndex + 1)
      {
        //We want all Elements "(startVersion, nextVersion)" as we are interested in all versions after the currently to be released Version
        var toBeSquashedVersions = orderedVersions.Skip(currentVersionIndex + 1).Take(nextVersionIndex - currentVersionIndex - 1).ToList();

        if (toBeSquashedVersions.Any(IsReleased))
          throw new JiraException(
              $"Version '{currentVersion.name}' cannot be released, as there is already one or multiple released version(s) ({string.Join(",", toBeSquashedVersions.Where(IsReleased).Select(t => t.JiraProjectVersion.name))}) before the next version '{nextVersion!.name}'.");

        var allClosedIssues = new List<JiraToBeMovedIssue>();

        foreach (var toBeSquashedVersion in toBeSquashedVersions)
          allClosedIssues.AddRange(_jiraIssueService.FindAllClosedIssues(toBeSquashedVersion.JiraProjectVersion.id));

        if (allClosedIssues.Count != 0)
          throw new JiraException(
              $"Version '{currentVersion.name}' cannot be released, as one  or multiple versions contain closed issues ({string.Join(", ", allClosedIssues.Select(aci => aci.Key))})");

        foreach (var toBeSquashedVersion in toBeSquashedVersions)
        {
          var toBeSquashedJiraProjectVersion = toBeSquashedVersion.JiraProjectVersion;

          if (toBeSquashedJiraProjectVersion.released == false)
          {
            var toBeSquashedVersionID = toBeSquashedJiraProjectVersion.id;

            var nonClosedIssues = _jiraIssueService.FindAllNonClosedIssues(toBeSquashedVersionID);
            _jiraIssueService.MoveIssuesToVersion(nonClosedIssues, toBeSquashedVersionID, nextVersionID);

            DeleteVersion(
                projectKey,
                toBeSquashedJiraProjectVersion.name ?? throw new InvalidOperationException("The version did not have a proper name assigned."));
          }
        }
      }

      ReleaseVersion(versionID);
    }
  }

  private bool IsReleased (JiraProjectVersionSemVerAdapter jiraVersion)
  {
    if (jiraVersion.JiraProjectVersion == null)
      throw new InvalidOperationException("The version did not have a proper jira project version assigned.");

    return jiraVersion.JiraProjectVersion.released;
  }

  public void ReleaseVersion (string versionID)
  {
    var resource = $"version/{versionID}";
    var request = _jiraRestClientProvider.GetJiraRestClient().CreateRestRequest(resource, Method.PUT);

    var adjustedReleaseDate = AdjustReleaseDateForJira(DateTime.Today);
    var projectVersion = new JiraProjectVersion { id = versionID, released = true, releaseDate = adjustedReleaseDate };
    request.AddBody(projectVersion);

    _jiraRestClientProvider.GetJiraRestClient().DoRequest(request, HttpStatusCode.OK);
  }

  public void DeleteVersion (string projectKey, string versionName)
  {
    var versions = _jiraProjectVersionFinder.GetVersions(projectKey);
    var versionToDelete = versions.SingleOrDefault(v => v.name == versionName);
    if (versionToDelete == null)
      throw new JiraException(string.Format("Error, version with name '{0}' does not exist in project '{1}'.", versionName, projectKey));

    var resource = $"version/{versionToDelete.id}";
    var request = _jiraRestClientProvider.GetJiraRestClient().CreateRestRequest(resource, Method.DELETE);
    _jiraRestClientProvider.GetJiraRestClient().DoRequest(request, HttpStatusCode.NoContent);
  }

  private static DateTime AdjustReleaseDateForJira (DateTime releaseDate)
  {
    var releaseDateAsUtcTime = releaseDate.ToUniversalTime();
    var difference = releaseDate - releaseDateAsUtcTime;
    var adjustedReleaseDate = releaseDate + difference;
    return adjustedReleaseDate;
  }
}