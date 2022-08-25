namespace ReleaseProcessAutomation.Jira.CredentialManagement;

public interface IJiraCredentialAPI
{
  public Credentials? GetCredential (string target);

  public void SaveCredentials (Credentials tmpCredentials, string target);
}