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
///   Should only be called by startReleaseStep and when on a release branch.
///   Calls msBuild and Jira stuff.
///   Always continues with continueAlphaBetaStep.
/// </summary>
public interface IReleaseRCOnReleaseBranchStep
{
  void Execute (SemanticVersion nextVersion, string? commitHash, bool pauseForCommit, bool noPush, string ancestor);
}

/// <inheritdoc cref="IReleaseRCOnReleaseBranchStep" />
public class ReleaseRCOnReleaseBranchStep : ReleaseProcessStepBase, IReleaseRCOnReleaseBranchStep
{
  private readonly IAncestorFinder _ancestorFinder;
  private readonly IContinueAlphaBetaStep _continueAlphaBetaStep;
  private readonly IReleaseVersionAndMoveIssuesSubStep _releaseVersionAndMoveIssuesSubStep;
  private readonly IMSBuildCallAndCommit _msBuildCallAndCommit;
  private readonly ILogger _log = Log.ForContext<ReleaseRCOnReleaseBranchStep>();

  public ReleaseRCOnReleaseBranchStep (
      IGitClient gitClient,
      Config config,
      IInputReader inputReader,
      IAncestorFinder ancestorFinder,
      IMSBuildCallAndCommit msBuildCallAndCommit,
      IContinueAlphaBetaStep continueAlphaBetaStep,
      IAnsiConsole console,
      IReleaseVersionAndMoveIssuesSubStep releaseVersionAndMoveIssuesSubStep)
      : base(gitClient, config, inputReader, console)
  {
    _ancestorFinder = ancestorFinder;
    _msBuildCallAndCommit = msBuildCallAndCommit;
    _continueAlphaBetaStep = continueAlphaBetaStep;
    _releaseVersionAndMoveIssuesSubStep = releaseVersionAndMoveIssuesSubStep;
  }

  public void Execute (SemanticVersion nextVersion, string? commitHash, bool pauseForCommit, bool noPush, string ancestor = "")
  {
    EnsureWorkingDirectoryClean();

    if (!GitClient.IsOnBranch("release/"))
    {
      const string message = "Cannot call ReleaseRCStep when not on a release branch";
      throw new InvalidOperationException(message);
    }

    if (string.IsNullOrEmpty(ancestor))
      ancestor = _ancestorFinder.GetAncestor("develop", "hotfix/v");
    
    _log.Debug("Found/given ancestor is '{ancestor}'.", ancestor);
    
    var currentBranchName = GitClient.GetCurrentBranchName();
    if (string.IsNullOrEmpty(currentBranchName))
    {
      const string message = "Could not find current branch.";
      throw new InvalidOperationException(message);
    }

    IReadOnlyCollection<SemanticVersion> nextPossibleVersions;

    if (ancestor.Equals("develop") || ancestor.StartsWith("release/"))
    {
      _log.Debug("Getting next possible jira versions for develop from '{NextVersion}'.", nextVersion);
      nextPossibleVersions = nextVersion.GetNextPossibleVersionsForReleaseBranchFromDevelop();
    }
    else if (ancestor.StartsWith("hotfix/"))
    {      
      _log.Debug("Getting next possible jira versions for hotfix from '{NextVersion}'.", nextVersion);
      nextPossibleVersions = nextVersion.GetNextPossibleVersionsForReleaseBranchFromHotfix();
    }
    else
    {
      var message = $"Ancestor has to be either 'develop' or a 'hotfix/v*.*.*' branch but was '{ancestor}'.";
      throw new InvalidOperationException(message);
    }

    var nextJiraVersion = InputReader.ReadVersionChoice("Please choose next version (open JIRA issues get moved there): ", nextPossibleVersions);
    _releaseVersionAndMoveIssuesSubStep.Execute(nextVersion, nextJiraVersion);

    var preReleaseBranchName = $"prerelease/v{nextVersion}";
    _log.Debug("Will try to create pre release branch with name '{PrereleaseBranchName}'.", preReleaseBranchName);
    if (GitClient.DoesBranchExist(preReleaseBranchName))
    {
      var message = $"The branch {preReleaseBranchName} already exists while trying to create a branch with that name.";
      throw new InvalidOperationException(message);
    }

    _ = GitClient.CheckoutCommitWithNewBranch(commitHash, preReleaseBranchName);

    _msBuildCallAndCommit.CallMSBuildStepsAndCommit(MSBuildMode.PrepareNextVersion, nextVersion);

    if (pauseForCommit)
      return;

    _continueAlphaBetaStep.Execute(nextVersion, currentBranchName, currentBranchName, noPush);
  }
}