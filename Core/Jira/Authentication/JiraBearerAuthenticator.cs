using System.Collections.Generic;
using System.Net;
using Remotion.ReleaseProcessAutomation.Jira.CredentialManagement;
using Remotion.ReleaseProcessAutomation.Jira.ServiceFacadeImplementations;
using RestSharp;

namespace Remotion.ReleaseProcessAutomation.Jira.Authentication
{
  public class JiraBearerAuthenticator : IJiraAuthenticator
  {
    public void CheckAuthentication (Credentials credential, string projectKey, string jiraURL)
    {
      var jiraRestClient = JiraRestClient.CreateWithBearerTokenAuthentication (jiraURL, credential);
      var request = jiraRestClient.CreateAuthRequest ("session", Method.GET);

      jiraRestClient.DoRequest<List<JiraProjectVersion>> (request, HttpStatusCode.OK);
    }
  }
}