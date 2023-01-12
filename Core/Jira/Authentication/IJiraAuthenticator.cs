using Remotion.ReleaseProcessAutomation.Jira.CredentialManagement;

namespace Remotion.ReleaseProcessAutomation.Jira.Authentication;

public interface IJiraAuthenticator
{
  void CheckAuthentication (Credentials credential, string projectKey, string jiraURL);
}