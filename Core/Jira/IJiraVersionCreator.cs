using System.Collections.Generic;
using Remotion.ReleaseProcessAutomation.Jira.ServiceFacadeImplementations;

namespace Remotion.ReleaseProcessAutomation.Jira;

public interface IJiraVersionCreator
{
  string CreateNewVersionWithVersionNumber (string versionNumber);
  JiraProjectVersion? FindVersionWithVersionNumber (string versionNumber);
  IReadOnlyList<JiraProjectVersion> FindAllVersionsStartingWithVersionNumber (string versionNumber);
}