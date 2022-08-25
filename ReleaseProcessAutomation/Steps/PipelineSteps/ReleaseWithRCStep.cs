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
using ReleaseProcessAutomation.Configuration.Data;
using ReleaseProcessAutomation.Extensions;
using ReleaseProcessAutomation.Git;
using ReleaseProcessAutomation.Jira;
using ReleaseProcessAutomation.ReadInput;
using ReleaseProcessAutomation.Scripting;
using ReleaseProcessAutomation.SemanticVersioning;
using ReleaseProcessAutomation.Steps.SubSteps;
using Serilog;
using Spectre.Console;

namespace ReleaseProcessAutomation.Steps.PipelineSteps;

/// <summary>
///   Should only be called by startReleaseStep and when on release branch.
///   Determines next Jira version based on the ancestor of the branch.
///   Calls msBuild and Jira stuff.
///   Determines the next step based on the ancestor of the branch.
///   Can optionally call release on master, but is unreachable.
/// </summary>
public interface IReleaseWithRCStep
{
  void Execute (bool pauseForCommit, bool noPush, string ancestor);
}

/// <inheritdoc cref="IReleaseWithRCStep" />
public class ReleaseWithRCStep : ReleaseProcessStepBase, IReleaseWithRCStep
{
  private readonly IAncestorFinder _ancestorFinder;
  private readonly IContinueReleaseOnMasterStep _continueReleaseOnMasterStep;
  private readonly IContinueReleasePatchStep _continueReleasePatchStep;
  private readonly IReleaseVersionAndMoveIssuesSubStep _releaseVersionAndMoveIssuesSubStep;
  private readonly IMSBuildCallAndCommit _msBuildCallAndCommit;
  private readonly ILogger _log = Log.ForContext<ReleaseWithRCStep>();

  public ReleaseWithRCStep (
      IGitClient gitClient,
      Config config,
      IInputReader inputReader,
      IAncestorFinder ancestorFinder,
      IMSBuildCallAndCommit msBuildCallAndCommit,
      IContinueReleaseOnMasterStep continueReleaseOnMasterStep,
      IContinueReleasePatchStep continueReleasePatchStep,
      IAnsiConsole console,
      IReleaseVersionAndMoveIssuesSubStep releaseVersionAndMoveIssuesSubStep)
      : base(gitClient, config, inputReader, console)
  {
    _ancestorFinder = ancestorFinder;
    _msBuildCallAndCommit = msBuildCallAndCommit;
    _continueReleaseOnMasterStep = continueReleaseOnMasterStep;
    _continueReleasePatchStep = continueReleasePatchStep;
    _releaseVersionAndMoveIssuesSubStep = releaseVersionAndMoveIssuesSubStep;
  }

  public void Execute (bool pauseForCommit, bool noPush, string ancestor)
  {
    EnsureWorkingDirectoryClean();

    if (!GitClient.IsOnBranch("release/"))
    {
      var currentBranch = GitClient.GetCurrentBranchName();
      var message = $"Cannot call ReleaseWithRC while not on a releaseBranch. Current branch: '{currentBranch}'.";
      throw new InvalidOperationException(message);
    }

    if (string.IsNullOrEmpty(ancestor))
      ancestor = _ancestorFinder.GetAncestor("develop", "hotfix/v");

    var currentBranchName = GitClient.GetCurrentBranchName();
    var nextVersion = new SemanticVersionParser().ParseVersionFromBranchName(currentBranchName!);

    var tagName = $"v{nextVersion}";
    _log.Debug("Will try to create tag with name '{TagName}'", tagName);
    if (GitClient.DoesTagExist(tagName))
    {
      var message = $"There is already a commit tagged with {tagName} while trying to create a tag.";
      throw new InvalidOperationException(message);
    }

    var releaseInformationMessage = $"You are releasing version '{nextVersion}'.";
    _log.Information(releaseInformationMessage);
    Console.WriteLine(releaseInformationMessage);

    IReadOnlyCollection<SemanticVersion> nextPossibleVersions;

    if (ancestor.Equals("develop"))
    {
      _log.Debug("Getting next possible jira versions for develop from '{nextVersion}'.", nextVersion);
      nextPossibleVersions = nextVersion.GetNextPossibleVersionsForReleaseBranchFromDevelop();
    }
    else if (ancestor.StartsWith("hotfix/"))
    {
      _log.Debug("Getting next possible jira versions for hotfix from '{nextVersion}'.", nextVersion);
      nextPossibleVersions = nextVersion.GetNextPossibleVersionsForReleaseBranchFromHotfix();
    }
    else
    {
      var message = $"Could not get next possible Jira Versions from the next version '{nextVersion}', current branch is '{currentBranchName}' and ancestor is '{ancestor}'.";
      throw new InvalidOperationException(message);
    }

    var nextJiraVersion = InputReader.ReadVersionChoice("Choose next version (open JIRA issues get moved there):", nextPossibleVersions);

    _releaseVersionAndMoveIssuesSubStep.Execute(nextVersion, nextJiraVersion);

    _msBuildCallAndCommit.CallMSBuildStepsAndCommit(MSBuildMode.PrepareNextVersion, nextVersion);

    if (pauseForCommit)
      return;

    if (ancestor.Equals("develop"))
    {
      _log.Debug("Ancestor is 'develop', calling continue release on master.");
      _continueReleaseOnMasterStep.Execute(nextVersion, noPush);
    }

    else if (ancestor.StartsWith("hotfix/"))
    {
      _log.Debug("Ancestor is 'hotfix/', calling continue release patch step not on master.");
      _continueReleasePatchStep.Execute(nextVersion, noPush, false);
    }
    //RPS in psm1 has master branching here, but has no version of how to get to it.
    //There are no tests for it since it cant happen anyways
    else if (ancestor.StartsWith("master"))
    {
      _log.Debug("Ancestor is 'master', calling continue release patch step on master.");
      _continueReleasePatchStep.Execute(nextVersion, noPush, true);
    }
  }
}