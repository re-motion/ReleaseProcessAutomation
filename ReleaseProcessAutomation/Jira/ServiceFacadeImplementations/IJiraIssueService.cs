using System.Collections.Generic;

namespace ReleaseProcessAutomation.Jira.ServiceFacadeImplementations;

public interface IJiraIssueService
{
  void MoveIssuesToVersion (IEnumerable<JiraToBeMovedIssue> issues, string oldVersionId, string newVersionId);
  IEnumerable<JiraToBeMovedIssue> FindAllNonClosedIssues (string versionId);
  IEnumerable<JiraToBeMovedIssue> FindAllClosedIssues (string versionId);
}