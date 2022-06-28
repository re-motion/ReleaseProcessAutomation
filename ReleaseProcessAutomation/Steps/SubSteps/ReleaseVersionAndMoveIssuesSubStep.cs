using System;
using System.Linq;
using ReleaseProcessAutomation.Jira;
using ReleaseProcessAutomation.Jira.ServiceFacadeImplementations;
using ReleaseProcessAutomation.ReadInput;
using ReleaseProcessAutomation.SemanticVersioning;
using Serilog;
using Spectre.Console;

namespace ReleaseProcessAutomation.Steps.SubSteps;

public interface IReleaseVersionAndMoveIssuesSubStep
{
  void Execute (SemanticVersion currentVersion, SemanticVersion nextVersion, bool squashUnreleased = false);
}

public class ReleaseVersionAndMoveIssuesSubStep
    : IReleaseVersionAndMoveIssuesSubStep
{
  private readonly IAnsiConsole _console;
  private readonly IInputReader _inputReader;
  private readonly IJiraIssueService _jiraIssueService;
  private readonly IJiraVersionCreator _jiraVersionCreator;
  private readonly IJiraVersionReleaser _jiraVersionReleaser;
  private readonly ILogger _log = Log.ForContext<ReleaseVersionAndMoveIssuesSubStep>();

  public ReleaseVersionAndMoveIssuesSubStep (
      IAnsiConsole console,
      IInputReader inputReader,
      IJiraIssueService jiraIssueService,
      IJiraVersionCreator jiraVersionCreator,
      IJiraVersionReleaser jiraVersionReleaser)
  {
    _console = console;
    _inputReader = inputReader;
    _jiraIssueService = jiraIssueService;
    _jiraVersionCreator = jiraVersionCreator;
    _jiraVersionReleaser = jiraVersionReleaser;
  }

  public void Execute (SemanticVersion currentVersion, SemanticVersion nextVersion, bool squashUnreleased = false)
  {
    var currentVersionID = CreateVersion(currentVersion);
    var nextVersionID = CreateVersion(nextVersion);

    var releaseMessage = $"Releasing version '{currentVersion}' on JIRA. ";
    _log.Information(releaseMessage);
    _console.WriteLine(releaseMessage);

    if (ShouldMoveIssuesToNextVersion(currentVersionID, nextVersionID, out var issuesToMove))
    {
      var moveMessage = $"Moving open issues to '{nextVersion}'.";
      _log.Information(moveMessage);
      _console.WriteLine(moveMessage);
      _jiraIssueService.MoveIssuesToVersion(issuesToMove, currentVersionID, nextVersionID);
    }

    if (squashUnreleased)
      _jiraVersionReleaser.ReleaseVersionAndSquashUnreleased(currentVersionID, nextVersionID);
    else
      _jiraVersionReleaser.ReleaseVersion(currentVersionID, false);
  }

  private string CreateVersion (SemanticVersion version)
  {
    return _jiraVersionCreator.CreateNewVersionWithVersionNumber(version.ToString());
  }

  private bool ShouldMoveIssuesToNextVersion (string versionID, string nextVersionID, out JiraToBeMovedIssue[] issuesToMove)
  {
    if (versionID == nextVersionID)
    {
      issuesToMove = Array.Empty<JiraToBeMovedIssue>();
      return false;
    }

    issuesToMove = _jiraIssueService.FindAllNonClosedIssues(versionID).ToArray();
    if (issuesToMove.Length == 0)
      return false;

    _console.WriteLine("These are some of the issues that will be moved by releasing the version on jira:");
    foreach (var issue in issuesToMove.Take(5))
      _console.WriteLine($"'{issue.Key} - {issue.Fields.Summary}'");
    _console.WriteLine("Do you want to move these issues to the new version and release the old one or just release the old version?");

    return _inputReader.ReadConfirmation();
  }
}