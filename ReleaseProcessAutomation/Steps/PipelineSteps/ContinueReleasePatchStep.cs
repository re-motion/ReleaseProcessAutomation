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
using ReleaseProcessAutomation.Extensions;
using ReleaseProcessAutomation.Git;
using ReleaseProcessAutomation.ReadInput;
using ReleaseProcessAutomation.Scripting;
using ReleaseProcessAutomation.SemanticVersioning;
using Serilog;
using Spectre.Console;

namespace ReleaseProcessAutomation.Steps.PipelineSteps;

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
  private readonly ILogger _log = Log.ForContext<ContinueReleasePatchStep>();

  public ContinueReleasePatchStep (
      IGitClient gitClient,
      Config config,
      IInputReader inputReader,
      IMSBuildCallAndCommit msBuildCallAndCommit,
      IPushPatchReleaseStep pushPatchReleaseStep,
      IAnsiConsole console)
      : base(gitClient, config, inputReader, console, msBuildCallAndCommit)
  {
    _msBuildCallAndCommit = msBuildCallAndCommit;
    _pushPatchReleaseStep = pushPatchReleaseStep;
  }

  public void Execute (SemanticVersion nextVersion, bool noPush, bool onMaster)
  {
    EnsureWorkingDirectoryClean();

    var mergeTargetBranchName = onMaster ? "master" : $"support/v{nextVersion.Major}.{nextVersion.Minor}";
    var toMergeBranchName = $"release/v{nextVersion}";

    _log.Debug("The branch '{ToMergeBranchName} 'will be merged into '{MergeTargetBranchName}'",toMergeBranchName, mergeTargetBranchName);
    
    EnsureBranchUpToDate(mergeTargetBranchName);
    EnsureBranchUpToDate(toMergeBranchName);

    var tagName = $"v{nextVersion}";
    _log.Debug("Creating tag with name '{tagName}'", tagName);

    if (GitClient.DoesTagExist(tagName))
    {
      var message = $"Cannot create tag {tagName} because it already exists.";
      throw new InvalidOperationException(message);
    }

    GitClient.Checkout(mergeTargetBranchName);

    MergeBranchWithReset(mergeTargetBranchName, toMergeBranchName, IgnoreListType.TagStableMergeIgnoreList);

    GitClient.Checkout(mergeTargetBranchName);
    CreateTagWithMessage(tagName);

    var nextPatchVersion = nextVersion.GetNextPatchVersion();
    GitClient.CheckoutNewBranch($"hotfix/v{nextPatchVersion}");

    _msBuildCallAndCommit.CallMSBuildStepsAndCommit(MSBuildMode.DevelopmentForNextRelease, nextPatchVersion);

    GitClient.Checkout(mergeTargetBranchName);

    CreateSupportBranchWithHotfixForRelease(nextVersion);

    GitClient.Checkout(mergeTargetBranchName);
    
    if (noPush)
      return;

    _pushPatchReleaseStep.Execute(mergeTargetBranchName, tagName, toMergeBranchName);
  }
  
}
