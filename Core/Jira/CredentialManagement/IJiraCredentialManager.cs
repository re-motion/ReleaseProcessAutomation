namespace Remotion.ReleaseProcessAutomation.Jira.CredentialManagement;

public interface IJiraCredentialManager
{
  Credentials GetCredential (string target);
}