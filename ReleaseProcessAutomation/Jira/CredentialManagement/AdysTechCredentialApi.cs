using System.Net;
using AdysTech.CredentialManager;

namespace ReleaseProcessAutomation.Jira.CredentialManagement;

public class AdysTechCredentialApi : IJiraCredentialAPI
{
  public Credentials? GetCredential (string target)
  {
    var cred = CredentialManager.GetCredentials(target);

    if (cred == null)
    {
      return null;
    }
    return new Credentials(cred.UserName, cred.Password);
  }
  
  public void SaveCredentials (Credentials tmpCredentials, string target)
  {
    var cred = new NetworkCredential(tmpCredentials.Username, tmpCredentials.Password);
    try
    {
      CredentialManager.RemoveCredentials(target);
    }
    catch
    {
      //catch the error that occurs when the credentials dont get deleted as this should only happen if there were no credentials to begin with
    }

    CredentialManager.SaveCredentials(target, cred);
  }
}