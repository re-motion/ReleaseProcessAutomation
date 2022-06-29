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
using ReleaseProcessAutomation.ReadInput;
using ReleaseProcessAutomation.Scripting;
using ReleaseProcessAutomation.SemanticVersioning;
using ReleaseProcessAutomation.Steps.SubSteps;
using Serilog;
using Spectre.Console;

namespace ReleaseProcessAutomation.Steps.PipelineSteps;

/// <summary>
///   Should only be called when on a develop or hotfix branch.
///   Calls MSBuild and updates Jira.
///   Always continues with continueAlphaBeta.
/// </summary>
public interface IReleaseAlphaBetaStep
{
  void Execute (SemanticVersion nextVersion, string? commitHash, bool pauseForCommit, bool noPush);
}

/// <inheritdoc cref="IReleaseAlphaBetaStep" />
public class ReleaseAlphaBetaStep
    : ReleaseProcessStepBase, IReleaseAlphaBetaStep
{
  private readonly IContinueAlphaBetaStep _continueAlphaBetaStep;
  private readonly IReleaseVersionAndMoveIssuesSubStep _releaseVersionAndMoveIssuesSubStep;
  private readonly IMSBuildCallAndCommit _msBuildCallAndCommit;
  private readonly ILogger _log = Log.ForContext<ReleaseAlphaBetaStep>();

  public ReleaseAlphaBetaStep (
      IGitClient gitClient,
      Config config,
      IInputReader inputReader,
      IMSBuildCallAndCommit msBuildCallAndCommit,
      IContinueAlphaBetaStep continueAlphaBetaStep,
      IAnsiConsole console,
      IReleaseVersionAndMoveIssuesSubStep releaseVersionAndMoveIssuesSubStep)
      : base(gitClient, config, inputReader, console)
  {
    _msBuildCallAndCommit = msBuildCallAndCommit;
    _continueAlphaBetaStep = continueAlphaBetaStep;
    _releaseVersionAndMoveIssuesSubStep = releaseVersionAndMoveIssuesSubStep;
  }

  public void Execute (SemanticVersion nextVersion, string? commitHash, bool pauseForCommit, bool noPush)
  {
    EnsureWorkingDirectoryClean();

    var currentBranchName = GitClient.GetCurrentBranchName();

    IReadOnlyCollection<SemanticVersion> nextPossibleJiraVersions;
    if (GitClient.IsOnBranch("develop"))
    {
      _log.Debug("On branch 'develop', getting next possible versions develop for jira.");
      nextPossibleJiraVersions = nextVersion.GetNextPossibleVersionsDevelop();
    }
    else if (GitClient.IsOnBranch("hotfix/"))
    {
      _log.Debug("On branch 'hotfix/', getting next possible versions hotfix for jira.");
      nextPossibleJiraVersions = nextVersion.GetNextPossibleVersionsHotfix();
    }
    else
    {
      var currentBranch = GitClient.GetCurrentBranchName();
      throw new InvalidOperationException("Cannot call ReleasePreReleaseFromDevelop from a non develop or hotfix/ branch.");
    }

    var branchName = $"prerelease/v{nextVersion}";
    if (GitClient.DoesBranchExist(branchName))
      throw new InvalidOperationException($"The branch '{branchName}' already exists.");

    GitClient.CheckoutCommitWithNewBranch(commitHash, branchName);

    var nextJiraVersion = InputReader.ReadVersionChoice("Please choose next version (open JIRA issues get moved there):", nextPossibleJiraVersions);

    _releaseVersionAndMoveIssuesSubStep.Execute(nextVersion, nextJiraVersion, false, true);

    _msBuildCallAndCommit.CallMSBuildStepsAndCommit(MSBuildMode.PrepareNextVersion, nextVersion);

    if (pauseForCommit)
      return;

    _continueAlphaBetaStep.Execute(nextVersion, currentBranchName, currentBranchName, noPush);
  }
}