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
using ReleaseProcessAutomation.Jira.ServiceFacadeInterfaces;
using ReleaseProcessAutomation.Jira.Utility;
using ReleaseProcessAutomation.SemanticVersioning;

namespace ReleaseProcessAutomation.Jira.ServiceFacadeImplementations;

public class JiraProjectVersionRepairer
    : IJiraProjectVersionRepairer
{
  private readonly IJiraProjectVersionService _jiraProjectVersionService;
  private readonly IJiraProjectVersionFinder _jiraProjectVersionFinder;

  public JiraProjectVersionRepairer (IJiraProjectVersionService jiraProjectVersionService, IJiraProjectVersionFinder jiraProjectVersionFinder)
  {
    _jiraProjectVersionService = jiraProjectVersionService;
    _jiraProjectVersionFinder = jiraProjectVersionFinder;
  }

  public void RepairVersionPosition (string versionId)
  {
    var jiraProjectVersion = _jiraProjectVersionFinder.GetVersionById(versionId);

    if (string.IsNullOrEmpty(jiraProjectVersion.projectId))
      throw new InvalidOperationException("Project id was null while trying to repair version");
    if (string.IsNullOrEmpty(jiraProjectVersion.name))
      throw new InvalidOperationException("Name was null while trying to repair version");
    var versions = _jiraProjectVersionFinder
        .FindVersions(jiraProjectVersion.projectId, "(?s).*")
        .ToList();

    if (IsSemanticVersion(jiraProjectVersion.name, ParseSemanticVersionOrNull))
      RepairVersion(jiraProjectVersion, versions, ParseSemanticVersionOrNull);
    else if (IsDotNetVersion(jiraProjectVersion.name, ParseDotNetVersionOrNull))
      RepairVersion(jiraProjectVersion, versions, ParseDotNetVersionOrNull);
    else
      throw new ArgumentException("Version has to be either a System.Version (1.0.0.0) or Semantic Version (1.0.0)");
  }

  private Version? ParseDotNetVersionOrNull (string input)
  {
    Version? outVersion;
    Version.TryParse(input, out outVersion);
    return outVersion;
  }

  private SemanticVersion? ParseSemanticVersionOrNull (string input)
  {
    return new SemanticVersionParser().TryParseVersion(input, out var output) ? output : null;
  }

  private bool IsDotNetVersion (string version, Func<string, Version?> netVersionParserFunc)
  {
    return netVersionParserFunc(version) != null;
  }

  private bool IsSemanticVersion (string version, Func<string, SemanticVersion?> semanticVersionParserFunc)
  {
    return semanticVersionParserFunc(version) != null;
  }

  private void RepairVersion<T> (
      JiraProjectVersion toBeRepairedVersion,
      IEnumerable<JiraProjectVersion> versions,
      Func<string, T?> parseVersion)
      where T : IComparable<T>
  {
    var parsedVersion = parseVersion(toBeRepairedVersion.name);

    var versionList = versions.Select(
        x => new JiraProjectVersionComparableAdapter<T>()
             {
                 JiraProjectVersion = x,
                 ComparableVersion = parseVersion(x.name ?? throw new InvalidOperationException("Name was null while trying to repair version"))
             }).ToList();

    var repairedVersionAsAdapter =
        new JiraProjectVersionComparableAdapter<T>()
        {
            JiraProjectVersion = toBeRepairedVersion,
            ComparableVersion = parsedVersion
        };

    var jiraMovePositioner = new JiraVersionPositionFinder<T>(versionList, repairedVersionAsAdapter);
    RepairVersionPosition(jiraMovePositioner);
  }

  private void RepairVersionPosition<T> (IJiraVersionMovePositioner<T> jiraVersionPositionFinder)
      where T : IComparable<T>
  {
    var createdVersion = jiraVersionPositionFinder.GetCreatedVersion();

    if (!jiraVersionPositionFinder.HasToBeMoved())
      return;

    var versionBeforeCreatedVersion = jiraVersionPositionFinder.GetVersionBeforeCreatedVersionOrderedList();

    if (createdVersion.JiraProjectVersion == null)
      throw new InvalidOperationException("Created version did not have a jira project version.");
    if (createdVersion.JiraProjectVersion.id == null)
      throw new InvalidOperationException("Created version did not have a jira project version with an id .");

    if (versionBeforeCreatedVersion == null)
    {
      _jiraProjectVersionService.MoveVersionByPosition(createdVersion.JiraProjectVersion.id, "First");
    }
    else if (versionBeforeCreatedVersion.ComparableVersion == null
             || !versionBeforeCreatedVersion.ComparableVersion.Equals(createdVersion.ComparableVersion))
    {
      if (versionBeforeCreatedVersion.JiraProjectVersion == null)
        throw new InvalidOperationException("The version before the created version did not have a jira project version.");
      if (versionBeforeCreatedVersion.JiraProjectVersion.self == null)
        throw new InvalidOperationException("The version before the created version did not have a 'self' in the jira project version.");
      _jiraProjectVersionService.MoveVersion(createdVersion.JiraProjectVersion.id, versionBeforeCreatedVersion.JiraProjectVersion.self);
    }
  }
}