// Copyright (c) rubicon IT GmbH, www.rubicon.eu
//
// See the NOTICE file distributed with this work for additional information
// regarding copyright ownership.  rubicon licenses this file to you under
// the Apache License, Version 2.0 (the "License"); you may not use this
// file except in compliance with the License.  You may obtain a copy of the
// License at
//
//   http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
// WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.  See the
// License for the specific language governing permissions and limitations
// under the License.
//

using System;
using System.Collections.Generic;
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
    : AddMoveCreateFixVersionsSubStepBase, IReleaseVersionAndMoveIssuesSubStep
{
  private readonly IJiraVersionReleaser _jiraVersionReleaser;
  private readonly ILogger _log = Log.ForContext<ReleaseVersionAndMoveIssuesSubStep>();

  public ReleaseVersionAndMoveIssuesSubStep (
      IAnsiConsole console,
      IInputReader inputReader,
      IJiraIssueService jiraIssueService,
      IJiraVersionCreator jiraVersionCreator,
      IJiraVersionReleaser jiraVersionReleaser)
      : base(console, inputReader, jiraIssueService, jiraVersionCreator)
  {
    _jiraVersionReleaser = jiraVersionReleaser;
  }

  public void Execute (SemanticVersion currentVersion, SemanticVersion nextVersion, bool squashUnreleased = false, bool movePreReleaseIssues = false)
  {
    var currentVersionID = CreateVersion(currentVersion);
    var nextVersionID = CreateVersion(nextVersion);

    var releaseMessage = $"Releasing version '{currentVersion}' on JIRA. ";
    _log.Information(releaseMessage);
    Console.WriteLine(releaseMessage);

    if (ShouldMoveIssuesToNextVersion(currentVersionID, nextVersionID, currentVersion, nextVersion, out var issuesToMove))
    {
      var moveMessage = $"Moving open issues to '{nextVersion}'.";
      _log.Information(moveMessage);
      Console.WriteLine(moveMessage);
      JiraIssueService.MoveIssuesToVersion(issuesToMove, currentVersionID, nextVersionID);
    }

    if (squashUnreleased)
      _jiraVersionReleaser.ReleaseVersionAndSquashUnreleased(currentVersionID, nextVersionID);
    else
      _jiraVersionReleaser.ReleaseVersion(currentVersionID, false);

    if (movePreReleaseIssues)
      AddNewlyReleasedVersionToClosedIssuesOnlyAssociatedWithFullVersion(currentVersion);
  }

  private bool ShouldMoveIssuesToNextVersion (string versionID, string nextVersionID, SemanticVersion currentVersion,SemanticVersion nextVersion, out IReadOnlyList<JiraToBeMovedIssue> issuesToMove)
  {
    if (versionID == nextVersionID)
    {
      issuesToMove = Array.Empty<JiraToBeMovedIssue>();
      return false;
    }

    issuesToMove = JiraIssueService.FindAllNonClosedIssues(versionID);
    if (issuesToMove.Count == 0)
      return false;

    Console.WriteLine($"These are some of the issues that will be moved by releasing version '{currentVersion}' on JIRA:");
    PrintIssueListing(issuesToMove);
    Console.WriteLine($"Do you want to move these issues to version '{nextVersion}' on JIRA?");

    return InputReader.ReadConfirmation();
  }

  private void AddNewlyReleasedVersionToClosedIssuesOnlyAssociatedWithFullVersion (SemanticVersion currentVersion)
  {
    var currentFullVersion = currentVersion.GetCurrentFullVersion().ToString();
    try
    {
      var allJiraVersionsStartingWithFullVersion = JiraVersionCreator.FindAllVersionsStartingWithVersionNumber(currentFullVersion);

      if (allJiraVersionsStartingWithFullVersion.Count == 0)
        throw new UserInteractionException($"Could not find versions starting with '{currentFullVersion}' in JIRA.");

      var currentFullJiraVersion =
          JiraVersionCreator.FindVersionWithVersionNumber(currentFullVersion)
          ?? throw new UserInteractionException($"Version '{currentFullVersion}' does not exist in JIRA.");

      var closedIssuesOnlyAssociatedWithFullVersion =
          JiraIssueService.FindIssuesWithOnlyExactFixVersion(allJiraVersionsStartingWithFullVersion, currentFullJiraVersion);

      if (closedIssuesOnlyAssociatedWithFullVersion.Count == 0)
      {
        var message = $"Could not find any issues only associated with fixVersion '{currentFullVersion}', no moving of these issues is necessary.";
        Console.WriteLine(message);
        _log.Debug(message);
        return;
      }

      Console.WriteLine($"The following JIRA tickets are closed and only associated with version '{currentFullVersion}':");

      PrintIssueListing(closedIssuesOnlyAssociatedWithFullVersion);

      Console.WriteLine($"Add the newly released version '{currentVersion}' to the fix-version of these JIRA tickets?");
      if (!InputReader.ReadConfirmation())
        return;

      AddFixVersionToIssues(currentVersion.ToString(), closedIssuesOnlyAssociatedWithFullVersion);
    }
    catch (Exception e)
    {
      Console.WriteLine(e.Message);
      Console.WriteLine($"Could not move closed JIRA issues from version '{currentFullVersion}' to '{currentVersion}'. \nDo you wish to continue?");
      if (!InputReader.ReadConfirmation())
        throw new UserInteractionException("Release canceled");
    }
  }

}