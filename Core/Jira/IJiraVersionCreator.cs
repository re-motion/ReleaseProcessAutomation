using System.Collections;
using System.Collections.Generic;
using ReleaseProcessAutomation.Jira.ServiceFacadeImplementations;

namespace ReleaseProcessAutomation.Jira;

public interface IJiraVersionCreator
{
  string CreateNewVersionWithVersionNumber (string versionNumber);
  JiraProjectVersion? FindVersionWithVersionNumber (string versionNumber);
  IReadOnlyList<JiraProjectVersion> FindAllVersionsStartingWithVersionNumber (string versionNumber);
}