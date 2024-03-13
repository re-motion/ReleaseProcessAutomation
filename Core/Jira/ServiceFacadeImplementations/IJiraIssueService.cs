using System.Collections.Generic;

namespace Remotion.ReleaseProcessAutomation.Jira.ServiceFacadeImplementations;

public interface IJiraIssueService
{
  void MoveIssuesToVersion (IEnumerable<JiraToBeMovedIssue> issues, string oldVersionId, string newVersionId);
  IReadOnlyList<JiraToBeMovedIssue> FindAllNonClosedIssues (string versionId, string projectKey);
  IReadOnlyList<JiraToBeMovedIssue> FindAllClosedIssues (string versionId, string projectKey);

  IReadOnlyList<JiraToBeMovedIssue> FindIssuesWithOnlyExactFixVersion (
      IEnumerable<JiraProjectVersion> allVersions,
      JiraProjectVersion exactFixVersion,
      string projectKey);

  void AddFixVersionToIssues(IEnumerable<JiraToBeMovedIssue> issues, string versionId);
}