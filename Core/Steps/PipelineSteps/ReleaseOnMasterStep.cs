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
  private readonly ILogger _log = Log.ForContext<ReleaseOnMasterStep>();

  public ReleaseOnMasterStep (
      IGitClient gitClient,
      IInputReader inputReader,
      IContinueReleaseOnMasterStep continueReleaseOnMasterStep,
      IPushNewReleaseBranchStep pushNewReleaseBranchStep,
      Config config,
      IMSBuildCallAndCommit msBuildCallAndCommit,
      IAnsiConsole console,
      IReleaseVersionAndMoveIssuesSubStep releaseVersionAndMoveIssuesSubStep)
      : base(gitClient, config, inputReader, console)
  {
    _continueReleaseOnMasterStep = continueReleaseOnMasterStep;
    _pushNewReleaseBranchStep = pushNewReleaseBranchStep;
    _msBuildCallAndCommit = msBuildCallAndCommit;
    _releaseVersionAndMoveIssuesSubStep = releaseVersionAndMoveIssuesSubStep;
  }

  public void Execute (SemanticVersion nextVersion, string? commitHash, bool startReleasePhase, bool pauseForCommit, bool noPush)
  {
    EnsureWorkingDirectoryClean();

    if (string.IsNullOrEmpty(GitClient.GetCurrentBranchName()))
    {
      var message = $"Could not find a branch in '{Environment.CurrentDirectory}'.";
      _log.Warning(message);
      Console.WriteLine(message);
    }

    if (!GitClient.IsOnBranch("develop"))
    {
      var currentBranch = GitClient.GetCurrentBranchName();
      var message = $"Cannot call ReleaseOnMasterStep when not on develop branch. Current branch: '{currentBranch}'.";
      throw new InvalidOperationException(message);
    }

    var releaseBranchName = $"release/v{nextVersion}";
    if (GitClient.DoesBranchExist(releaseBranchName))
    {
      var message = $"The branch '{releaseBranchName}' already exists.";
      throw new Exception(message);
    }

    _log.Debug("Getting next possible jira versions for develop from version '{NextVersion}'.", nextVersion);
    var nextPossibleVersions = nextVersion.GetNextPossibleVersionsDevelop();
    var nextJiraVersion = InputReader.ReadVersionChoice("Please choose next version(open JIRA issues get moved there):", nextPossibleVersions);

    _ = GitClient.CheckoutCommitWithNewBranch(commitHash, releaseBranchName);

    _ = GitClient.Checkout("develop");

    _msBuildCallAndCommit.CallMSBuildStepsAndCommit(MSBuildMode.PrepareNextVersion, nextJiraVersion);

    GitClient.Checkout(releaseBranchName);

    if (startReleasePhase)
    {
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