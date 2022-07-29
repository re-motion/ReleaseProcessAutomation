namespace ReleaseProcessAutomation.Jira;

public interface IJiraVersionReleaser
{
  void ReleaseVersion (string versionID, string nextVersionID, bool sortReleasedVersion);
  void ReleaseVersionAndSquashUnreleased (string versionID, string nextVersionID);
}