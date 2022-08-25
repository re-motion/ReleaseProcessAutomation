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
using ReleaseProcessAutomation.Git;
using ReleaseProcessAutomation.SemanticVersioning;
using ReleaseProcessAutomation.Steps.PipelineSteps;
using Serilog;

namespace ReleaseProcessAutomation.Steps;

/// <summary>
///   Should be called when StartReleaseStep was ended early with the option pauseForCommit.
///   Determines the next steps based on the current branch.
/// </summary>
public interface IContinueRelease
{
  void Execute (string? ancestor, bool noPush);
}

/// <inheritdoc cref="IContinueRelease" />
public class ContinueReleaseStep
    : IContinueRelease
{
  private readonly IBranchFromPreReleaseForContinueVersionStep _branchFromPreReleaseForContinueVersionStep;
  private readonly IBranchFromReleaseForContinueVersionStep _branchFromReleaseForContinueVersionStep;
  private readonly IGitClient _gitClient;
  private readonly ILogger _log = Log.ForContext<ContinueReleaseStep>();

  public ContinueReleaseStep (
      IGitClient gitClient,
      IBranchFromPreReleaseForContinueVersionStep branchFromPreReleaseForContinueVersionStep,
      IBranchFromReleaseForContinueVersionStep branchFromReleaseForContinueVersionStep)
  {
    _gitClient = gitClient;
    _branchFromPreReleaseForContinueVersionStep = branchFromPreReleaseForContinueVersionStep;
    _branchFromReleaseForContinueVersionStep = branchFromReleaseForContinueVersionStep;
  }

  public void Execute (string? ancestor, bool noPush)
  {
    var currentBranchName = _gitClient.GetCurrentBranchName();
    if (string.IsNullOrEmpty(currentBranchName))
    {
      const string message = "Could not continue the release because there was no current branch found.";
      throw new InvalidOperationException(message);
    }

    //should already be on the releaseBranch, therefore this version is the next version
    var nextVersion = new SemanticVersionParser().ParseVersionFromBranchName(currentBranchName);
    _log.Debug("Next version to be released is '{NextVersion}'.", nextVersion);

    if (_gitClient.IsOnBranch("prerelease/"))
    {
      _log.Debug("On branch '{BranchName}', calling branch from pre release for continue version.", currentBranchName);
      _branchFromPreReleaseForContinueVersionStep.Execute(nextVersion, ancestor, noPush);
    }

    else if (_gitClient.IsOnBranch("release/"))
    {
      _log.Debug("On branch '{BranchName}', calling branch from release for continue version.", currentBranchName);
      _branchFromReleaseForContinueVersionStep.Execute(nextVersion, ancestor, noPush);
    }
    else
    {
      throw new InvalidOperationException("You have to be on a prerelease/* or release/* branch to continue a release.");
    }
  }
}