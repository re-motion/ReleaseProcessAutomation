using System.Collections.Generic;

namespace ReleaseProcessAutomation.Jira.ServiceFacadeImplementations;

public interface IJiraIssueService
{
  void MoveIssuesToVersion (IEnumerable<JiraToBeMovedIssue> issues, string oldVersionId, string newVersionId);
  IReadOnlyList<JiraToBeMovedIssue> FindAllNonClosedIssues (string versionId);
  IReadOnlyList<JiraToBeMovedIssue> FindAllClosedIssues (string versionId);

  IReadOnlyList<JiraToBeMovedIssue> FindIssuesWithOnlyExactFixVersion (
      IEnumerable<JiraProjectVersion> allVersions,
      JiraProjectVersion exactFixVersion);
}