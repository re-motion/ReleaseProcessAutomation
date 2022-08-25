using ReleaseProcessAutomation.Jira.ServiceFacadeImplementations;

namespace ReleaseProcessAutomation.Jira.Utility;

public interface IJiraRestClientProvider
{
  JiraRestClient GetJiraRestClient ();
}