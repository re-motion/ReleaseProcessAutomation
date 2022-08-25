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
using ReleaseProcessAutomation.Extensions;
using ReleaseProcessAutomation.Git;
using ReleaseProcessAutomation.ReadInput;
using ReleaseProcessAutomation.SemanticVersioning;
using Serilog;

namespace ReleaseProcessAutomation.Steps.PipelineSteps;

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
  private readonly IReleaseRCStep _releaseRCStep;
  private readonly IReleaseWithRCStep _releaseWithRCStep;
  private readonly ILogger _log = Log.ForContext<BranchFromReleaseStep>();

  public BranchFromReleaseStep (IGitClient gitClient, IInputReader inputReader, IReleaseRCStep releaseRCStep, IReleaseWithRCStep releaseWithRCStep)
  {
    _gitClient = gitClient;
    _inputReader = inputReader;
    _releaseRCStep = releaseRCStep;
    _releaseWithRCStep = releaseWithRCStep;
  }

  public void Execute (string? commitHash, bool pauseForCommit, bool noPush)
  {
    var currentVersion = new SemanticVersionParser().ParseVersionFromBranchName(_gitClient.GetCurrentBranchName()!);
    _log.Debug("The current found version is '{CurrentVersion}'.", currentVersion);
    var rcVersion = FindNextRC(currentVersion);

    var choice = _inputReader.ReadVersionChoice("Which version do you wish to release?", new[] { currentVersion, rcVersion });

    if (choice.Equals(rcVersion))
    {
      _log.Debug("The choice was the rc version '{RCVersion}', calling release rc.", rcVersion);
      _releaseRCStep.Execute(rcVersion, commitHash, pauseForCommit, noPush, "");
    }
    else
    {
      _log.Debug("The choice was the release-to-master version '{CurrentVersion}', calling release with rc.", currentVersion);
      _releaseWithRCStep.Execute(pauseForCommit, noPush, "");
    }
  }

  private SemanticVersion FindNextRC (SemanticVersion currentVersion)
  {
    var nextVersion = currentVersion.GetNextRC();
    while (_gitClient.DoesTagExist($"v{nextVersion}"))
      nextVersion = nextVersion.GetNextRC();

    return nextVersion;
  }
}