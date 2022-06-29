using System;
using System.Collections.Generic;
using System.Linq;
using ReleaseProcessAutomation.Extensions;
using ReleaseProcessAutomation.Jira;
using ReleaseProcessAutomation.Jira.ServiceFacadeImplementations;
using ReleaseProcessAutomation.ReadInput;
using ReleaseProcessAutomation.SemanticVersioning;
using Serilog;
using Spectre.Console;

namespace ReleaseProcessAutomation.Steps.SubSteps;

public interface IReleaseVersionAndMoveIssuesSubStep
{
  void Execute (SemanticVersion currentVersion, SemanticVersion nextVersion, bool squashUnreleased = false, bool movePreReleaseIssues = false);
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

  public void Execute (SemanticVersion currentVersion, SemanticVersion nextVersion, bool squashUnreleased = false, bool movePreReleaseIssues = false)
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

    if (movePreReleaseIssues)
      MoveClosedIssuesFromPreviousVersion(currentVersion);
  }

  private void MoveClosedIssuesFromPreviousVersion (SemanticVersion currentVersion)
  {
    var currentFullVersion = currentVersion.GetCurrentFullVersion().ToString();
    try
    {
      var currentVersions = _jiraVersionCreator.FindAllVersionsStartingWithVersionNumber(currentFullVersion);

      if (currentVersions.Count == 0)
        throw new InvalidOperationException($"Could not find version '{currentFullVersion}'.");

      var allVersionsWithFullVersion = _jiraVersionCreator.FindVersionWithVersionNumber(currentFullVersion)
                                       ?? throw new InvalidOperationException(
                                           $"Could not find any version with version number '{currentFullVersion}'.");

      var jiraClosedIssuesWithFixVersion =
          _jiraIssueService.FindIssuesWithOnlyExactFixVersion(currentVersions, allVersionsWithFullVersion);

      if (jiraClosedIssuesWithFixVersion.Count == 0)
        throw new InvalidOperationException($"Could not find any issues with exact fixVersion '{currentFullVersion}'.");

      foreach (var issue in jiraClosedIssuesWithFixVersion.Take(5))
        _console.WriteLine($"{issue.Key} - {issue.Fields.Summary}");
      _console.WriteLine(
          $"Do you want to move the closed versions with Jira fixVersion '{currentFullVersion}' to the newly released version '{currentVersion}'?");
      if (!_inputReader.ReadConfirmation())
        return;

      MoveClosedIssuesToNewVersion(currentFullVersion, currentVersion.ToString(), jiraClosedIssuesWithFixVersion);
    }
    catch (Exception e)
    {
      _console.WriteLine(e.Message);
      _console.WriteLine($"Could not move closed jira issues from version '{currentFullVersion}' to '{currentVersion}'. \nDo you wish to continue?");
      if (!_inputReader.ReadConfirmation())
        throw new UserDoesNotWantToContinueException("User does not want to continue due to inability to move jira issues.");
    }
  }

  private void MoveClosedIssuesToNewVersion (
      string currentVersion,
      string nextVersion,
      IEnumerable<JiraToBeMovedIssue> closedIssuesToMove)
  {
    var currentJiraVersion = _jiraVersionCreator.FindVersionWithVersionNumber(currentVersion);
    var nextJiraVersion = _jiraVersionCreator.FindVersionWithVersionNumber(nextVersion);
    if (currentJiraVersion == null || nextJiraVersion == null)
      throw new InvalidOperationException($"Could not find current jira version '{currentVersion}' or next jira version '{nextVersion}'");

    _jiraIssueService.MoveIssuesToVersion(closedIssuesToMove, currentJiraVersion.id, nextJiraVersion.id);
  }

  private string CreateVersion (SemanticVersion version)
  {
    return _jiraVersionCreator.CreateNewVersionWithVersionNumber(version.ToString());
  }

  private bool ShouldMoveIssuesToNextVersion (string versionID, string nextVersionID, out IReadOnlyList<JiraToBeMovedIssue> issuesToMove)
  {
    if (versionID == nextVersionID)
    {
      issuesToMove = Array.Empty<JiraToBeMovedIssue>();
      return false;
    }

    issuesToMove = _jiraIssueService.FindAllNonClosedIssues(versionID);
    if (issuesToMove.Count == 0)
      return false;

    _console.WriteLine("These are some of the issues that will be moved by releasing the version on jira:");
    foreach (var issue in issuesToMove.Take(5))
      _console.WriteLine($"'{issue.Key} - {issue.Fields.Summary}'");
    _console.WriteLine("Do you want to move these issues to the new version and release the old one or just release the old version?");

    return _inputReader.ReadConfirmation();
  }
}