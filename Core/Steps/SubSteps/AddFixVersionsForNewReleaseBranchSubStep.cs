using Remotion.ReleaseProcessAutomation.Extensions;
using Remotion.ReleaseProcessAutomation.Jira;
using Remotion.ReleaseProcessAutomation.Jira.ServiceFacadeImplementations;
using Remotion.ReleaseProcessAutomation.ReadInput;
using Remotion.ReleaseProcessAutomation.SemanticVersioning;
using Spectre.Console;

namespace Remotion.ReleaseProcessAutomation.Steps.SubSteps;

public interface IAddFixVersionsForNewReleaseBranchSubStep
{
  void Execute (SemanticVersion currentVersion, SemanticVersion nextJiraVersion);
}

public class AddFixVersionsForNewReleaseBranchSubStep : AddMoveCreateFixVersionsSubStepBase, IAddFixVersionsForNewReleaseBranchSubStep
{
  public AddFixVersionsForNewReleaseBranchSubStep (
      IAnsiConsole console,
      IInputReader inputReader,
      IJiraIssueService jiraIssueService,
      IJiraVersionCreator jiraVersionCreator)
      : base(console, inputReader, jiraIssueService, jiraVersionCreator)
  {
  }

  public void Execute (SemanticVersion currentVersion, SemanticVersion nextJiraVersion)
  {
    var issues = JiraIssueService.FindAllNonClosedIssues(currentVersion.ToString());
    Console.WriteLine($"These are some of the issues that are open for the current version '{currentVersion}':");
    PrintIssueListing(issues);

    var rcVersion = currentVersion.GetNextRC();
    Console.WriteLine($"Do you want to create version '{rcVersion}' in JIRA and add it as a fix version for the issues?");
    if (InputReader.ReadConfirmation())
    {
      AddFixVersionToIssues(rcVersion.ToString(), issues);
    }

    Console.WriteLine($"Do you want to create version '{nextJiraVersion}' in JIRA and add it as a fix version for the issues?");
    if (InputReader.ReadConfirmation())
    {
      AddFixVersionToIssues(nextJiraVersion.ToString(), issues);
    }
  }
}