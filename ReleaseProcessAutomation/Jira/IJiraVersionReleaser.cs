namespace ReleaseProcessAutomation.Jira;

public interface IJiraVersionReleaser
{
  void ReleaseVersion (string versionID, bool sortReleasedVersion);
  void ReleaseVersionAndSquashUnreleased (string versionID, string nextVersionID);
}