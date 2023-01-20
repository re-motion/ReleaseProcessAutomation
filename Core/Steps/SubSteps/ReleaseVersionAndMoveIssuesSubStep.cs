using System;
using System.Collections.Generic;
using System.Linq;
using Remotion.ReleaseProcessAutomation.Extensions;
using Remotion.ReleaseProcessAutomation.Jira;
using Remotion.ReleaseProcessAutomation.Jira.ServiceFacadeImplementations;
using Remotion.ReleaseProcessAutomation.ReadInput;
using Remotion.ReleaseProcessAutomation.SemanticVersioning;
using Serilog;
using Spectre.Console;

namespace Remotion.ReleaseProcessAutomation.Steps.SubSteps;

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
      AddNewlyReleasedVersionToClosedIssuesOnlyAssociatedWithFullVersion(currentVersion);
  }

  private void AddNewlyReleasedVersionToClosedIssuesOnlyAssociatedWithFullVersion (SemanticVersion currentVersion)
  {
    var currentFullVersion = currentVersion.GetCurrentFullVersion().ToString();
    try
    {
      var allJiraVersionsStartingWithFullVersion = _jiraVersionCreator.FindAllVersionsStartingWithVersionNumber(currentFullVersion);

      if (allJiraVersionsStartingWithFullVersion.Count == 0)
        throw new UserInteractionException($"Could not find versions starting with '{currentFullVersion}' in JIRA.");

      var currentFullJiraVersion =
          _jiraVersionCreator.FindVersionWithVersionNumber(currentFullVersion)
          ?? throw new UserInteractionException($"Version '{currentFullVersion}' does not exist in JIRA.");

      var closedIssuesOnlyAssociatedWithFullVersion =
          _jiraIssueService.FindIssuesWithOnlyExactFixVersion(allJiraVersionsStartingWithFullVersion, currentFullJiraVersion);

      if (closedIssuesOnlyAssociatedWithFullVersion.Count == 0)
      {
        var message = $"Could not find any issues only associated with fixVersion '{currentFullVersion}', no moving of these issues is necessary.";
        _console.WriteLine(message);
        _log.Debug(message);
        return;
      }

      _console.WriteLine($"The following Jira tickets are closed and only associated with version '{currentFullVersion}':");

      PrintIssueListing(closedIssuesOnlyAssociatedWithFullVersion);

      _console.WriteLine($"Add the newly released version '{currentVersion}' to the fix-version of these Jira tickets?");
      if (!_inputReader.ReadConfirmation())
        return;

      AddFixVersionToIssues(currentVersion.ToString(), closedIssuesOnlyAssociatedWithFullVersion);
    }
    catch (Exception e)
    {
      _console.WriteLine(e.Message);
      _console.WriteLine($"Could not move closed jira issues from version '{currentFullVersion}' to '{currentVersion}'. \nDo you wish to continue?");
      if (!_inputReader.ReadConfirmation())
        throw new UserInteractionException("Release canceled");
    }
  }

  private void AddFixVersionToIssues (
      string currentVersion,
      IEnumerable<JiraToBeMovedIssue> issues)
  {
    var currentJiraVersion = _jiraVersionCreator.FindVersionWithVersionNumber(currentVersion);
    if (currentJiraVersion == null)
      throw new UserInteractionException($"Cannot apply fix version '{currentVersion}' because the version does not exist in JIRA.");

    _jiraIssueService.AddFixVersionToIssues(issues, currentJiraVersion.id);
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
    PrintIssueListing(issuesToMove);
    _console.WriteLine("Do you want to move these issues to the new version and release the old one or just release the old version?");

    return _inputReader.ReadConfirmation();
  }

  private void PrintIssueListing (IReadOnlyList<JiraToBeMovedIssue> issues, int maxLines = 5)
  {
    const string elipses = "...";
    var maxLength = _console.Profile.Width;
    foreach (var issue in issues.Take(maxLines))
    {
      var message = $"* {issue.Key} - {issue.Fields.Summary}";

      if (message.Length > maxLength)
        message = message.Substring(0, maxLength - elipses.Length).TrimEnd() + elipses;

      _console.WriteLine(message);
    }
    if (issues.Count > maxLines)
      _console.WriteLine(elipses);
  }
}