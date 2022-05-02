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
using Serilog;
using Spectre.Console;

namespace ReleaseProcessAutomation.Steps.PipelineSteps;

/// <summary>
///   Should only be called by startReleaseStep and when on a release branch.
///   Calls msBuild and Jira stuff.
///   Always continues with continueAlphaBetaStep.
/// </summary>
public interface IReleaseRCStep
{
  void Execute (SemanticVersion nextVersion, string? commitHash, bool pauseForCommit, bool noPush, string ancestor);
}

/// <inheritdoc cref="IReleaseRCStep" />
public class ReleaseRCStep : ReleaseProcessStepBase, IReleaseRCStep
{
  private readonly IAncestorFinder _ancestorFinder;
  private readonly IContinueAlphaBetaStep _continueAlphaBetaStep;
  private readonly IMSBuildCallAndCommit _msBuildCallAndCommit;
  private readonly ILogger _log = Log.ForContext<ReleaseRCStep>();
  
  public ReleaseRCStep (
      IGitClient gitClient,
      Config config,
      IInputReader inputReader,
      IAncestorFinder ancestorFinder,
      IMSBuildCallAndCommit msBuildCallAndCommit,
      IContinueAlphaBetaStep continueAlphaBetaStep,
      IAnsiConsole console)
      : base(gitClient, config, inputReader, console)
  {
    _ancestorFinder = ancestorFinder;
    _msBuildCallAndCommit = msBuildCallAndCommit;
    _continueAlphaBetaStep = continueAlphaBetaStep;
  }

  public void Execute (SemanticVersion nextVersion, string? commitHash, bool pauseForCommit, bool noPush, string ancestor = "")
  {
    EnsureWorkingDirectoryClean();

    if (!GitClient.IsOnBranch("release/"))
    {
      const string message = "Cannot call ReleaseRcStep when not on a release branch";
      throw new InvalidOperationException(message);
    }

    if (string.IsNullOrEmpty(ancestor))
      ancestor = _ancestorFinder.GetAncestor("develop", "hotfix/v");
    
    _log.Debug("Found/given ancestor is '{ancestor}'", ancestor);
    
    var currentBranchName = GitClient.GetCurrentBranchName();
    if (string.IsNullOrEmpty(currentBranchName))
    {
      const string message = "Could not find current branch.";
      throw new InvalidOperationException(message);
    }

    IReadOnlyCollection<SemanticVersion> nextPossibleVersions;

    if (ancestor.Equals("develop") || ancestor.StartsWith("release/"))
    {
      _log.Debug("Getting next possible jira versions for develop from '{NextVersion}'", nextVersion);
      nextPossibleVersions = nextVersion.GetNextPossibleVersionsDevelop();
    }
    else if (ancestor.StartsWith("hotfix/"))
    {      
      _log.Debug("Getting next possible jira versions for hotfix from '{NextVersion}'", nextVersion);
      nextPossibleVersions = nextVersion.GetNextPossibleVersionsHotfix();
    }
    else
    {
      var message = $"Ancestor has to be either 'develop' or a 'hotfix/v*.*.*' branch but was '{ancestor}'";
      throw new InvalidOperationException(message);
    }

    var nextJiraVersion = InputReader.ReadVersionChoice("Please choose next version (open JIRA issues get moved there): ", nextPossibleVersions);

    //Create and release jira versions

    var preReleaseBranchName = $"prerelease/v{nextVersion}";
    _log.Debug("Will try to create pre release branch with name '{PrereleaseBranchName}'", preReleaseBranchName);
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
