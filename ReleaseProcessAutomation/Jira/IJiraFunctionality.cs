using System;
using ReleaseProcessAutomation.SemanticVersioning;

namespace ReleaseProcessAutomation.Jira;

public interface IJiraFunctionality
{
  void CreateAndReleaseJiraVersion (SemanticVersion currentVersion, SemanticVersion nextVersion, bool squashUnreleased = false);
}