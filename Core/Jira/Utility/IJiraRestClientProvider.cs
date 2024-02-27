using Remotion.ReleaseProcessAutomation.Jira.ServiceFacadeImplementations;

namespace Remotion.ReleaseProcessAutomation.Jira.Utility;

public interface IJiraRestClientProvider
{
  JiraRestClient GetJiraRestClient ();
}