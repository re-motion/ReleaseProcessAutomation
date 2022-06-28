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
using ReleaseProcessAutomation.Configuration.Data;
using ReleaseProcessAutomation.Git;
using ReleaseProcessAutomation.ReadInput;
using ReleaseProcessAutomation.Scripting;
using ReleaseProcessAutomation.SemanticVersioning;
using Serilog;
using Spectre.Console;

namespace ReleaseProcessAutomation.Steps.PipelineSteps;

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
      IGitBranchOperations gitBranchOperations)
      : base(gitClient, config, inputReader, console, msBuildCallAndCommit)
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
      throw new InvalidOperationException("Cannot call ContinueReleaseOnMaster when not on release branch.");
    }

    CreateTagAndMerge();

    if (noPush)
      return;

    _pushMasterReleaseStep.Execute(nextVersion);
  }

  private void CreateTagAndMerge ()
  {
    var currentBranchName = GitClient.GetCurrentBranchName();

    _log.Debug("The current branch name is '{CurrentBranchName}'.",currentBranchName);

    if (currentBranchName == null)
    {
      const string message = "Could not create tag and merge because there was no current branch found.";
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
      var message = $"There is already a commit tagged with '{tagName}'.";
      throw new Exception(message);
    }

    GitClient.Checkout("master");

    GitClient.MergeBranchToOnlyContainChangesFromMergedBranch(currentBranchName);

    CreateTagWithMessage(tagName);

    CreateSupportBranchWithHotfixForRelease(currentVersion);

    GitClient.Checkout("develop");
  }
}