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
///   Should only be called when on hotfix or master branch and when the commit hash has already been validated.
///   Calls MSBuild and JIRA.
///   Continues to ContinueReleasePatchStep.
/// </summary>
public interface IReleasePatchStep
{
  void Execute (SemanticVersion nextVersion, string? commitHash, bool startReleasePhase, bool pauseForCommit, bool noPush, bool onMaster);
}

/// <inheritdoc cref="IReleasePatchStep" />
public class ReleasePatchStep : ReleaseProcessStepBase, IReleasePatchStep
{
  private readonly IContinueReleasePatchStep _continueReleasePatchStep;
  private readonly IReleaseVersionAndMoveIssuesSubStep _releaseVersionAndMoveIssuesSubStep;
  private readonly IPushNewReleaseBranchStep _pushNewReleaseBranchStep;
  private readonly IMSBuildCallAndCommit _msBuildCallAndCommit;
  private readonly ILogger _log = Log.ForContext<ReleasePatchStep>();

  public ReleasePatchStep (
      IGitClient gitClient,
      Config config,
      IInputReader inputReader,
      IMSBuildCallAndCommit msBuildCallAndCommit,
      IContinueReleasePatchStep continueReleasePatchStep,
      IPushNewReleaseBranchStep pushNewReleaseBranchStep,
      IAnsiConsole console,
      IReleaseVersionAndMoveIssuesSubStep releaseVersionAndMoveIssuesSubStep)
      : base(gitClient, config, inputReader, console)
  {
    _msBuildCallAndCommit = msBuildCallAndCommit;
    _continueReleasePatchStep = continueReleasePatchStep;
    _releaseVersionAndMoveIssuesSubStep = releaseVersionAndMoveIssuesSubStep;
    _pushNewReleaseBranchStep = pushNewReleaseBranchStep;

  }

  public void Execute (SemanticVersion nextVersion, string? commitHash, bool startReleasePhase, bool pauseForCommit, bool noPush, bool onMaster)
  {
    EnsureWorkingDirectoryClean();

    var currentBranchName = GitClient.GetCurrentBranchName();
    if (currentBranchName == null)
    {
      const string message = "Could not get the name of the current branch.";
      throw new InvalidOperationException(message);
    }

    if (onMaster)
    {
      if (currentBranchName != "master")
      {
        const string message = "Cannot release a patch for master when not on master branch.";
        throw new InvalidOperationException(message);
      }
    }
    else
    {
      if (!currentBranchName.StartsWith("hotfix/"))
      {
        const string message = "Cannot release a patch for hotfix when not on hotfix branch";
        throw new InvalidOperationException(message);
      }
    }

    var versionToBeReleasedMessage = $"The version to be released: '{nextVersion}'";
    _log.Debug(versionToBeReleasedMessage);
    Console.WriteLine(versionToBeReleasedMessage);

    _log.Debug("Getting next possible jira versions for hotfix from version '{NextVersion}'.", nextVersion);
    var nextPossibleJiraVersions = nextVersion.GetNextPossibleVersionsHotfix();
    var nextJiraVersion = InputReader.ReadVersionChoice("Please choose next version (open JIRA issues get moved there): ", nextPossibleJiraVersions);

    var releaseBranchName = $"release/v{nextVersion}";
    _log.Debug("Will try to create release branch name '{ReleaseBranchName}'.", releaseBranchName);
    if (GitClient.DoesBranchExist(releaseBranchName))
    {
      var message = $"Cannot create release branch '{releaseBranchName}' because it already exists.";
      throw new InvalidOperationException(message);
    }

    var tagName = $"v{nextVersion}";
    _log.Debug("Will try to create tag with name '{TagName}'.", tagName);
    if (GitClient.DoesTagExist(tagName))
    {
      var message = $"Cannot create tag'{tagName}' because that tag already exists.";
      throw new InvalidOperationException(message);
    }

    GitClient.CheckoutCommitWithNewBranch(commitHash, releaseBranchName);

    if (startReleasePhase)
    {
      _pushNewReleaseBranchStep.Execute(releaseBranchName, currentBranchName);
      return;
    }

    _releaseVersionAndMoveIssuesSubStep.Execute(nextVersion, nextJiraVersion);

    _msBuildCallAndCommit.CallMSBuildStepsAndCommit(MSBuildMode.PrepareNextVersion, nextVersion);

    if (pauseForCommit)
      return;

    _continueReleasePatchStep.Execute(nextVersion, noPush, onMaster);
  }
}