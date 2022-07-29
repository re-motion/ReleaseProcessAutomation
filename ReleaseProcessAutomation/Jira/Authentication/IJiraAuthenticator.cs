using System;
using ReleaseProcessAutomation.Jira.CredentialManagement;

namespace ReleaseProcessAutomation.Jira.Authentication;

public interface IJiraAuthenticator
{
  void CheckAuthentication (Credentials credential, string projectKey, string jiraURL);
}