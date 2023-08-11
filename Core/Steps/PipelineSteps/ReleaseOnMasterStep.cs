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
using Remotion.ReleaseProcessAutomation.Configuration.Data;
using Remotion.ReleaseProcessAutomation.Extensions;
using Remotion.ReleaseProcessAutomation.Git;
using Remotion.ReleaseProcessAutomation.ReadInput;
using Remotion.ReleaseProcessAutomation.Scripting;
using Remotion.ReleaseProcessAutomation.SemanticVersioning;
using Remotion.ReleaseProcessAutomation.Steps.SubSteps;
using Serilog;
using Spectre.Console;

namespace Remotion.ReleaseProcessAutomation.Steps.PipelineSteps;

/// <summary>
///   Should only be called when on develop branch.
///   Calls msBuild and Jira stuff.
///   Always continues with ContinueReleaseOnMaster.
/// </summary>
public interface IReleaseOnMasterStep
{
  void Execute (SemanticVersion nextVersion, string? commitHash, bool startReleasePhase, bool pauseForCommit, bool noPush);
}

/// <inheritdoc cref="IReleaseOnMasterStep" />
public class ReleaseOnMasterStep
    : ReleaseProcessStepBase, IReleaseOnMasterStep
{
  private readonly IContinueReleaseOnMasterStep _continueReleaseOnMasterStep;
  private readonly IPushNewReleaseBranchStep _pushNewReleaseBranchStep;
  private readonly IMSBuildCallAndCommit _msBuildCallAndCommit;
  private readonly IReleaseVersionAndMoveIssuesSubStep _releaseVersionAndMoveIssuesSubStep;
  private readonly IAddFixVersionsForNewReleaseBranchSubStep _addFixVersionsForNewReleaseBranchSubStep;
  private readonly ILogger _log = Log.ForContext<ReleaseOnMasterStep>();

  public ReleaseOnMasterStep (
      IGitClient gitClient,
      IInputReader inputReader,
      IContinueReleaseOnMasterStep continueReleaseOnMasterStep,
      IPushNewReleaseBranchStep pushNewReleaseBranchStep,
      Config config,
      IMSBuildCallAndCommit msBuildCallAndCommit,
      IAnsiConsole console,
      IReleaseVersionAndMoveIssuesSubStep releaseVersionAndMoveIssuesSubStep,
      IAddFixVersionsForNewReleaseBranchSubStep addFixVersionsForNewReleaseBranchSubStep)
      : base(gitClient, config, inputReader, console)
  {
    _continueReleaseOnMasterStep = continueReleaseOnMasterStep;
    _pushNewReleaseBranchStep = pushNewReleaseBranchStep;
    _msBuildCallAndCommit = msBuildCallAndCommit;
    _releaseVersionAndMoveIssuesSubStep = releaseVersionAndMoveIssuesSubStep;
    _addFixVersionsForNewReleaseBranchSubStep = addFixVersionsForNewReleaseBranchSubStep;
  }

  public void Execute (SemanticVersion nextVersion, string? commitHash, bool startReleasePhase, bool pauseForCommit, bool noPush)
  {
    EnsureWorkingDirectoryClean();

    var currentBranch = GitClient.GetCurrentBranchName();
    if (string.IsNullOrEmpty(currentBranch))
    {
      var message = "Could not identify the currently checked-out branch in the repository's working directory.";
      throw new InvalidOperationException(message);
    }

    if (!GitClient.IsOnBranch("develop"))
    {
      var message = $"Cannot release a release candidate when not on the 'develop' branch. Current branch: '{currentBranch}'.";
      throw new UserInteractionException(message);
    }

    var releaseBranchName = $"release/v{nextVersion}";
    if (GitClient.DoesBranchExist(releaseBranchName))
    {
      var message = $"The branch '{releaseBranchName}' already exists.";
      throw new UserInteractionException(message);
    }

    _log.Debug("Getting next possible jira versions for develop from version '{NextVersion}'.", nextVersion);
    var nextPossibleVersions = nextVersion.GetNextPossibleVersionsDevelop();
    var nextJiraVersion = InputReader.ReadVersionChoiceForFollowingRelease(nextPossibleVersions);

    _ = GitClient.CheckoutCommitWithNewBranch(commitHash, releaseBranchName);

    _ = GitClient.Checkout("develop");

    _msBuildCallAndCommit.CallMSBuildStepsAndCommit(MSBuildMode.PrepareNextVersion, nextJiraVersion);

    GitClient.Checkout(releaseBranchName);

    if (startReleasePhase)
    {
      _addFixVersionsForNewReleaseBranchSubStep.Execute(nextVersion, nextJiraVersion);
      _pushNewReleaseBranchStep.Execute(releaseBranchName, "develop");
      return;
    }
    _releaseVersionAndMoveIssuesSubStep.Execute(nextVersion, nextJiraVersion);

    _msBuildCallAndCommit.CallMSBuildStepsAndCommit(MSBuildMode.PrepareNextVersion, nextVersion);

    if (pauseForCommit)
      return;

    _continueReleaseOnMasterStep.Execute(nextVersion, noPush);
  }
}