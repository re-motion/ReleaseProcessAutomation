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
using Remotion.ReleaseProcessAutomation.Jira;
using Remotion.ReleaseProcessAutomation.ReadInput;
using Remotion.ReleaseProcessAutomation.Scripting;
using Remotion.ReleaseProcessAutomation.SemanticVersioning;
using Serilog;
using Spectre.Console;

namespace Remotion.ReleaseProcessAutomation.Steps.PipelineSteps;

/// <summary>
///   Should only be called when on the release branch.
///   Determines and creates the appropriate tag and merges the changes made by the previous steps.
///   Always continues with PushMasterRelease.
/// </summary>
public interface IContinueReleaseOnMasterStep
{
  void Execute (SemanticVersion nextVersion, bool noPush);
}

/// <inheritdoc cref="IContinueReleaseOnMasterStep" />
public class ContinueReleaseOnMasterStep
    : ContinueReleaseStepWithOptionalSupportBranchStepBase, IContinueReleaseOnMasterStep
{
  private readonly IPushMasterReleaseStep _pushMasterReleaseStep;
  private readonly IGitBranchOperations _gitBranchOperations;
  private readonly ILogger _log = Log.ForContext<ContinueReleaseOnMasterStep>();

  public ContinueReleaseOnMasterStep (
      IGitClient gitClient,
      Config config,
      IInputReader inputReader,
      IPushMasterReleaseStep pushMasterReleaseStep,
      IAnsiConsole console,
      IMSBuildCallAndCommit msBuildCallAndCommit,
      IGitBranchOperations gitBranchOperations,
      IJiraVersionCreator jiraVersionCreator)
      : base(gitClient, config, inputReader, console, msBuildCallAndCommit, jiraVersionCreator)
  {
    _pushMasterReleaseStep = pushMasterReleaseStep;
    _gitBranchOperations = gitBranchOperations;
  }

  public void Execute (SemanticVersion nextVersion, bool noPush)
  {
    EnsureWorkingDirectoryClean();

    if (!GitClient.IsOnBranch("release/"))
    {
      var currentBranch = GitClient.GetCurrentBranchName();
      throw new UserInteractionException($"Cannot complete the release when not on a 'master' branch. Current branch: '{currentBranch}'.");
    }

    CreateTagAndMerge(noPush);

    if (noPush)
      return;

    _pushMasterReleaseStep.Execute(nextVersion);
  }

  private void CreateTagAndMerge (bool noPush)
  {
    var currentBranchName = GitClient.GetCurrentBranchName();

    _log.Debug("The current branch name is '{CurrentBranchName}'.",currentBranchName);

    if (currentBranchName == null)
    {
      const string message = "Could not identify the currently checked-out branch in the repository's working directory.";
      throw new InvalidOperationException(message);
    }

    var currentVersion = new SemanticVersionParser().ParseVersionFromBranchName(currentBranchName);
    _log.Debug("The current version is '{CurrentVersion}'.",currentVersion);

    _gitBranchOperations.EnsureBranchUpToDate(currentBranchName);
    _gitBranchOperations.EnsureBranchUpToDate("master");
    _gitBranchOperations.EnsureBranchUpToDate("develop");

    var tagName = $"v{currentVersion}";
    _log.Debug("Will try to create tag with name '{TagName}'.", tagName);
    if (GitClient.DoesTagExist(tagName))
    {
      var message = $"Cannot create tag '{tagName}' because it already exists.";
      throw new UserInteractionException(message);
    }

    GitClient.Checkout("master");

    GitClient.MergeBranchToOnlyContainChangesFromMergedBranch(currentBranchName);

    CreateTagWithMessage(tagName);

    CreateSupportBranchWithHotfixForRelease(currentVersion.GetNextPatchVersion(), noPush);

    GitClient.Checkout("develop");
  }
}