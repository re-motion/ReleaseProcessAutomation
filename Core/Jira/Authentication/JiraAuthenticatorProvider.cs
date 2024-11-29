using Remotion.ReleaseProcessAutomation.Configuration.Data;

namespace Remotion.ReleaseProcessAutomation.Jira.Authentication
{
  public class JiraAuthenticatorProvider : IJiraAuthenticatorProvider
  {
    private readonly Config _config;

    public JiraAuthenticatorProvider(Config config)
    {
      _config = config;
    }

    public IJiraAuthenticator GetAuthenticator()
    {
      if (_config.Jira.UseBearer)
        return new JiraBearerAuthenticator();
      else
        return new JiraBasicAuthenticator();
    }
  }
}