using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using ReleaseProcessAutomation.Jira.CredentialManagement;
using ReleaseProcessAutomation.Jira.ServiceFacadeImplementations;
using RestSharp;

namespace ReleaseProcessAutomation.IntegrationTests.Jira;

public static class JiraTestUtility
{
  private const string c_usernameEnvironmentVariableName = "JiraUsername";
  private const string c_passwordEnvironmentVariableName = "JiraPassword";

  public static int RunProgramWithoutWindowsCredentials (string[] args)
  {
    var services = new ApplicationServiceCollectionFactory().CreateServiceCollection();

    var jiraCredentialAPIStub = new Mock<IJiraCredentialAPI>();
    var jiraCredentialAPIDescriptor = new ServiceDescriptor(typeof(IJiraCredentialAPI), x => jiraCredentialAPIStub.Object, ServiceLifetime.Singleton);

    services.Replace(jiraCredentialAPIDescriptor);

    var app = new ApplicationCommandAppFactory().CreateConfiguredCommandApp(services);

    return app.Run(args);
  }

  public static Credentials GetLocallySavedCredentials ()
  {
    var jiraUsername = Environment.GetEnvironmentVariable(c_usernameEnvironmentVariableName);
    var jiraPassword = Environment.GetEnvironmentVariable(c_passwordEnvironmentVariableName);

    if (string.IsNullOrEmpty(jiraUsername))
      throw new InvalidOperationException($"Could not load credentials from environment variable '{c_usernameEnvironmentVariableName}'");

    if (string.IsNullOrEmpty(jiraPassword))
      throw new InvalidOperationException($"Could not load credentials from environment variable '{c_passwordEnvironmentVariableName}'");

    return new Credentials(jiraUsername, jiraPassword);
  }

  public static string CreateVersion (JiraRestClient restClient, string versionName, string projectKey)
  {
    var request = restClient.CreateRestRequest("version", Method.POST);

    var projectVersion = new JiraProjectVersion { name = versionName, project = projectKey, releaseDate = null };
    request.AddBody(projectVersion);

    var newVersion = restClient.DoRequest<JiraProjectVersion>(request, HttpStatusCode.Created);

    return newVersion.Data.id;
  }

  public static void DeleteVersionIfExistent (string projectName, string versionName, JiraRestClient restClient)
  {
    var versions = GetAllJiraVersions(projectName, restClient);
    var versionToDelete = versions.SingleOrDefault(v => v.name == versionName);
    if (versionToDelete == null)
      return;

    var resource = $"version/{versionToDelete.id}";

    var request = restClient.CreateRestRequest(resource, Method.DELETE);
    restClient.DoRequest(request, HttpStatusCode.NoContent);
  }

  public static void DeleteVersionsIfExistent (string projectName, JiraRestClient jiraRestClient, params string[] versionNames)
  {
    foreach (var versionName in versionNames)
      DeleteVersionIfExistent(projectName, versionName, jiraRestClient);
  }

  public static JiraIssue AddTestIssueToVersion (
      string summaryOfIssue,
      bool closed,
      string jiraProjectKey,
      JiraRestClient restClient,
      params JiraProjectVersion[] toRelease)
  {
    return AddTestIssueToVersion(summaryOfIssue, closed, jiraProjectKey, restClient, toRelease.Select(v => v.id).ToArray());
  }

  public static JiraIssue AddTestIssueToVersion (
      string summaryOfIssue,
      bool closed,
      string jiraProjectKey,
      JiraRestClient restClient,
      params string[] toReleaseID)
  {
    // Create new issue
    var resource = "issue";
    var request = restClient.CreateRestRequest(resource, Method.POST);

    var body = new
               {
                   fields = new
                            {
                                project = new { key = jiraProjectKey },
                                issuetype = new { name = "Task" },
                                summary = summaryOfIssue,
                                description = "testDescription",
                                fixVersions = toReleaseID.Select(s => new JiraProjectVersion { id = s }),
                                components = new[] { new { name = "APMTestComponent" } }
                            }
               };
    request.AddBody(body);

    var response = restClient.DoRequest<JiraIssue>(request, HttpStatusCode.Created);

    // Close issue if necessary
    if (closed)
    {
      var issue = response.Data;
      CloseIssue(issue.ID, restClient);
    }

    return response.Data;
  }

  public static JiraIssue GetIssue (string issueID, JiraRestClient restClient)
  {
    var request = restClient.CreateRestRequest($"/issue/{issueID}", Method.GET);
    var response = restClient.DoRequest<JiraIssue>(request, HttpStatusCode.OK);
    return response.Data;
  }

  public static void DeleteIssue (JiraRestClient restClient, string issueID)
  {
    var request = restClient.CreateRestRequest($"/issue/{issueID}", Method.DELETE);
    restClient.DoRequest(request, HttpStatusCode.NoContent);
  }

  public static void DeleteIssues (JiraRestClient jiraRestClient, params string[] issueIds)
  {
    foreach (var issueId in issueIds)
      DeleteIssue(jiraRestClient, issueId);
  }

  public static bool IsPartOfJiraVersions (string projectKey, string versionName, JiraRestClient restClient, out JiraProjectVersion foundVersion)
  {
    var allVersions = GetAllJiraVersions(projectKey, restClient);
    try
    {
      foundVersion = allVersions.First(v => v.name.Equals(versionName));
    }
    catch
    {
      foundVersion = null;
      return false;
    }

    return true;
  }

  private static void CloseIssue (string issueID, JiraRestClient restClient)
  {
    var resource = "issue/" + issueID + "/transitions";
    var request = restClient.CreateRestRequest(resource, Method.POST);

    var body = new { transition = new { id = 2 } };
    request.AddBody(body);

    restClient.DoRequest(request, HttpStatusCode.NoContent);
  }

  private static IEnumerable<JiraProjectVersion> GetAllJiraVersions (string projectKey, JiraRestClient restClient)
  {
    var request = restClient.CreateRestRequest($"project/{projectKey}/versions", Method.GET);
    var response = restClient.DoRequest<List<JiraProjectVersion>>(request, HttpStatusCode.OK);
    return response.Data;
  }
}