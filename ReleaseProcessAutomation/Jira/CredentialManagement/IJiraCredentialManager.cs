using System;

namespace ReleaseProcessAutomation.Jira.CredentialManagement;

public interface IJiraCredentialManager
{
  Credentials GetCredential (string target);
}