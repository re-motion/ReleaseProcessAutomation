using Remotion.ReleaseProcessAutomation.Configuration.Data;
using Remotion.ReleaseProcessAutomation.Jira.CredentialManagement;
using Remotion.ReleaseProcessAutomation.Jira.ServiceFacadeImplementations;

namespace Remotion.ReleaseProcessAutomation.Jira.Utility;

public class JiraRestClientProvider
    : IJiraRestClientProvider
{
  private readonly Config _config;
  private readonly IJiraCredentialManager _jiraCredentialManager;

  private JiraRestClient? _jiraRestClient;

  public JiraRestClientProvider (Config config, IJiraCredentialManager jiraCredentialManager)
  {
    _jiraCredentialManager = jiraCredentialManager;
    _config = config;
  }

  public JiraRestClient GetJiraRestClient ()
  {
    if (_jiraRestClient != null)
      return _jiraRestClient;

    if (_config.Jira.UseNTLM)
    {
      _jiraRestClient = JiraRestClient.CreateWithNtlmAuthentication(_config.Jira.JiraURL);
    }
    else
    {
      var credentials = _jiraCredentialManager.GetCredential(_config.Jira.JiraURL);
      _jiraRestClient = JiraRestClient.CreateWithBasicAuthentication(_config.Jira.JiraURL, credentials);
    }

    return _jiraRestClient;
  }
}