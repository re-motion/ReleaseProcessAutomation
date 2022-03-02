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
using ReleaseProcessScript.New.Extensions;
using ReleaseProcessScript.New.Git;
using ReleaseProcessScript.New.ReadInput;
using ReleaseProcessScript.New.SemanticVersioning;
using Serilog;

namespace ReleaseProcessScript.New.Steps.PipelineSteps;

/// <summary>
///   Should only be called by StartReleaseStep.
///   Finds next version by asking the user which version to release.
///   Determines the next step based on the next version.
/// </summary>
public interface IBranchFromReleaseStep
{
  void Execute (string? commitHash, bool pauseForCommit, bool noPush);
}

/// <inheritdoc cref="IBranchFromReleaseStep" />
public class BranchFromReleaseStep
    : IBranchFromReleaseStep
{
  private readonly IGitClient _gitClient;
  private readonly IInputReader _inputReader;
  private readonly IReleaseRCStep _releaseRcStep;
  private readonly IReleaseWithRcStep _releaseWithRcStep;
  private readonly ILogger _log = Log.ForContext<BranchFromReleaseStep>();

  public BranchFromReleaseStep (IGitClient gitClient, IInputReader inputReader, IReleaseRCStep releaseRcStep, IReleaseWithRcStep releaseWithRcStep)
  {
    _gitClient = gitClient;
    _inputReader = inputReader;
    _releaseRcStep = releaseRcStep;
    _releaseWithRcStep = releaseWithRcStep;
  }

  public void Execute (string? commitHash, bool pauseForCommit, bool noPush)
  {
    var currentVersion = new SemanticVersionParser().ParseVersionFromBranchName(_gitClient.GetCurrentBranchName()!);
    _log.Debug("The current found version is '{CurrentVersion}'", currentVersion);
    var rcVersion = FindNextRc(currentVersion);

    var choice = _inputReader.ReadVersionChoice("Which version do you wish to release?", new[] { currentVersion, rcVersion });

    if (choice.Equals(rcVersion))
    {
      _log.Debug("The choice was the rcVersion '{RCVersion}', calling release rc", rcVersion);
      _releaseRcStep.Execute(rcVersion, commitHash, pauseForCommit, noPush, "");
    }
    else
    {
      _log.Debug("The choice was the RTM version '{CurrentVersion}', calling release with rc", currentVersion);
      _releaseWithRcStep.Execute(pauseForCommit, noPush, "");
    }
  }

  private SemanticVersion FindNextRc (SemanticVersion currentVersion)
  {
    var nextVersion = currentVersion.GetNextRc();
    while (_gitClient.DoesTagExist($"v{nextVersion}"))
      nextVersion = nextVersion.GetNextRc();

    return nextVersion;
  }
}
