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
using System.Linq;
using ReleaseProcessAutomation.Extensions;
using ReleaseProcessAutomation.Git;
using ReleaseProcessAutomation.ReadInput;
using ReleaseProcessAutomation.SemanticVersioning;
using Serilog;

namespace ReleaseProcessAutomation.Steps.PipelineSteps;

/// <summary>
///   Should only be called when on develop branch.
///   Determines the version that should be released based on either the previous release and/or user input.
///   Calls the next steps based on the determined version.
/// </summary>
public interface IBranchFromDevelopStep
{
  void Execute (string? commitHash, bool pauseForCommit, bool noPush, bool startReleasePhase);
}

/// <inheritdoc cref="IBranchFromDevelopStep" />
public class BranchFromDevelopStep
    : IBranchFromDevelopStep
{
  private readonly IInputReader _inputReader;
  private readonly IReleaseAlphaBetaStep _releaseAlphaBetaStep;
  private readonly IReleaseOnMasterStep _releaseOnMasterStep;
  private readonly ISemanticVersionedGitRepository _semanticVersionedGitRepository;

  private readonly ILogger _log = Log.ForContext<BranchFromDevelopStep>();

  public BranchFromDevelopStep (
      IInputReader inputReader,
      IReleaseOnMasterStep releaseOnMasterStep,
      IReleaseAlphaBetaStep releaseAlphaBetaStep,
      ISemanticVersionedGitRepository semanticVersionedGitRepository)
  {
    _releaseAlphaBetaStep = releaseAlphaBetaStep;
    _semanticVersionedGitRepository = semanticVersionedGitRepository;
    _inputReader = inputReader;
    _releaseOnMasterStep = releaseOnMasterStep;
  }

  public void Execute (string? commitHash, bool pauseForCommit, bool noPush, bool startReleasePhase)
  {
    var nextVersion = GetNextVersion(startReleasePhase);
    _log.Debug("The next version to be released is '{NextVersion}'", nextVersion);

    var preVersion = nextVersion.Pre;

    if (preVersion == null)
    {
      _log.Debug("Prerelease version was null, calling release on master step");
      _releaseOnMasterStep.Execute(nextVersion, commitHash, startReleasePhase, pauseForCommit, noPush);
    }

    else if (preVersion == PreReleaseStage.alpha || preVersion == PreReleaseStage.beta)
    {
      _log.Debug("Prerelease version was '{PreVersion}', calling release alpha beta step",preVersion);
      _releaseAlphaBetaStep.Execute(nextVersion, commitHash, pauseForCommit, noPush);
    }
  }

  private SemanticVersion GetNextVersion (bool isPreRelease)
  {
    if (!_semanticVersionedGitRepository.TryGetCurrentVersion(out _))
      return _inputReader.ReadSemanticVersion("Enter the next version of the branch");

    _semanticVersionedGitRepository.TryGetCurrentVersion(out var developVersion);
    if (_semanticVersionedGitRepository.TryGetCurrentVersion(out var masterVersion, "master"))
    {
      var mostRecentVersion = new[] { developVersion, masterVersion }.Max()!;
      var possibleNextVersions = mostRecentVersion.GetNextPossibleVersionsDevelop(isPreRelease);

      return _inputReader.ReadVersionChoice("Please choose the next version", possibleNextVersions);
    }

    return _inputReader.ReadVersionChoice("Please choose the next version", developVersion!.GetNextPossibleVersionsDevelop(isPreRelease));
  }
}
