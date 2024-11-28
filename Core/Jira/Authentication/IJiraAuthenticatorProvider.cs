namespace Remotion.ReleaseProcessAutomation.Jira.Authentication
{
  public interface IJiraAuthenticatorProvider
  {
    IJiraAuthenticator GetAuthenticator();
  }
}