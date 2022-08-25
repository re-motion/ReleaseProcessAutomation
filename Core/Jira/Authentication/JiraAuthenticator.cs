using System;
using System.Collections.Generic;
using System.Net;
using ReleaseProcessAutomation.Jira.CredentialManagement;
using ReleaseProcessAutomation.Jira.ServiceFacadeImplementations;
using RestSharp;

namespace ReleaseProcessAutomation.Jira.Authentication;

public class JiraAuthenticator
    : IJiraAuthenticator
{
  public void CheckAuthentication (Credentials credentials, string projectKey, string jiraURL)
  {
    var jiraRestClient = JiraRestClient.CreateWithBasicAuthentication(jiraURL, credentials);
    var request = jiraRestClient.CreateAuthRequest("session", Method.GET);

    jiraRestClient.DoRequest<List<JiraProjectVersion>>(request, HttpStatusCode.OK);
  }
}