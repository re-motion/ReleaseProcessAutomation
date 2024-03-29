﻿// Copyright (c) rubicon IT GmbH, www.rubicon.eu
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
///   Should only be called when on release branch.
///   Determines and creates the appropriate tag, merges the changes made by previous steps and calls msBuild.
/// </summary>
public interface IContinueReleasePatchStep
{
  void Execute (SemanticVersion nextVersion, bool noPush, bool onMaster);
}

/// <inheritdoc cref="ContinueReleasePatchStep" />
public class ContinueReleasePatchStep
    : ContinueReleaseStepWithOptionalSupportBranchStepBase, IContinueReleasePatchStep
{
  private readonly IMSBuildCallAndCommit _msBuildCallAndCommit;
  private readonly IPushPatchReleaseStep _pushPatchReleaseStep;
  private readonly IGitBranchOperations _gitBranchOperations;
  private readonly ILogger _log = Log.ForContext<ContinueReleasePatchStep>();

  public ContinueReleasePatchStep (
      IGitClient gitClient,
      Config config,
      IInputReader inputReader,
      IMSBuildCallAndCommit msBuildCallAndCommit,
      IPushPatchReleaseStep pushPatchReleaseStep,
      IGitBranchOperations gitBranchOperations,
      IAnsiConsole console,
      IJiraVersionCreator jiraVersionCreator)
      : base(gitClient, config, inputReader, console, msBuildCallAndCommit, jiraVersionCreator)
  {
    _msBuildCallAndCommit = msBuildCallAndCommit;
    _pushPatchReleaseStep = pushPatchReleaseStep;
    _gitBranchOperations = gitBranchOperations;
  }

  public void Execute (SemanticVersion nextVersion, bool noPush, bool onMaster)
  {
    EnsureWorkingDirectoryClean();

    var mergeTargetBranchName = onMaster ? "master" : $"support/v{nextVersion.Major}.{nextVersion.Minor}";
    var toMergeBranchName = $"release/v{nextVersion}";

    _log.Debug("The branch '{ToMergeBranchName} 'will be merged into '{MergeTargetBranchName}'.", toMergeBranchName, mergeTargetBranchName);

    _gitBranchOperations.EnsureBranchUpToDate(mergeTargetBranchName);
    _gitBranchOperations.EnsureBranchUpToDate(toMergeBranchName);

    var tagName = $"v{nextVersion}";
    _log.Debug("Creating tag with name '{tagName}'.", tagName);

    if (GitClient.DoesTagExist(tagName))
    {
      var message = $"Cannot create tag '{tagName}' because it already exists.";
      throw new UserInteractionException(message);
    }

    GitClient.Checkout(mergeTargetBranchName);

    MergeBranch(mergeTargetBranchName, toMergeBranchName);

    GitClient.Checkout(mergeTargetBranchName);
    CreateTagWithMessage(tagName);

    var nextPatchVersion = nextVersion.GetNextPatchVersion();
    var hotfixBranch = $"hotfix/v{nextPatchVersion}";
    GitClient.CheckoutNewBranch(hotfixBranch);

    _msBuildCallAndCommit.CallMSBuildStepsAndCommit(MSBuildMode.DevelopmentForNextRelease, nextPatchVersion);

    GitClient.Checkout(mergeTargetBranchName);

    CreateSupportBranchWithHotfixForRelease(nextVersion.GetNextMinor(), noPush);

    GitClient.Checkout(mergeTargetBranchName);

    if (noPush)
      return;

    _pushPatchReleaseStep.Execute(mergeTargetBranchName, tagName, toMergeBranchName, hotfixBranch);
  }
}