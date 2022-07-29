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
///   Should only be called by startReleaseStep and when on a hotfix branch.
///   Determines the next step based on the current hotfix version.
/// </summary>
public interface IBranchFromHotfixStep
{
  void Execute (string? commitHash, bool startReleasePhase, bool pauseForCommit, bool noPush);
}

/// <inheritdoc cref="IBranchFromHotfixStep" />
public class BranchFromHotfixStep
    : IBranchFromHotfixStep
{
  private readonly IGitClient _gitClient;
  private readonly IInputReader _inputReader;
  private readonly IReleaseAlphaBetaStep _releaseAlphaBetaStep;
  private readonly IReleasePatchStep _releasePatchStep;
  private readonly ISemanticVersionedGitRepository _semanticVersionedGitRepository;
  private readonly ILogger _log = Log.ForContext<BranchFromHotfixStep>();

  public BranchFromHotfixStep (
      IGitClient gitClient,
      IInputReader inputReader,
      ISemanticVersionedGitRepository semanticVersionedGitRepository,
      IReleasePatchStep releasePatchStep,
      IReleaseAlphaBetaStep releaseAlphaBetaStep)
  {
    _gitClient = gitClient;
    _inputReader = inputReader;
    _semanticVersionedGitRepository = semanticVersionedGitRepository;
    _releasePatchStep = releasePatchStep;
    _releaseAlphaBetaStep = releaseAlphaBetaStep;
  }

  public void Execute (string? commitHash, bool startReleasePhase, bool pauseForCommit, bool noPush)
  {
    var nextVersion = GetCurrentHotfixVersion(startReleasePhase);
    _log.Debug("The next version to be released is '{NextVersion}'", nextVersion);

    if (nextVersion.Pre == null)
    {
      _log.Debug("Prerelease version was null, calling release branch  step");
      _releasePatchStep.Execute(nextVersion, commitHash, startReleasePhase, pauseForCommit, noPush, false);
    }
    else if (nextVersion.Pre is PreReleaseStage.alpha or PreReleaseStage.beta)
    {
      _log.Debug("Prerelease version was '{PreVersion}', calling release alpha beta step", nextVersion.Pre);
      _releaseAlphaBetaStep.Execute(nextVersion, commitHash, pauseForCommit, noPush);
    }
  }

  private SemanticVersion GetCurrentHotfixVersion (bool startReleasePhase = false)
  {
    if (startReleasePhase)
    {
      var currentBranchName = _gitClient.GetCurrentBranchName();
      if (string.IsNullOrEmpty(currentBranchName))
      {
        const string message = "Could not find the current branch while trying to get next hotfix version.";
        throw new InvalidOperationException(message);
      }

      return new SemanticVersionParser().ParseVersionFromBranchName(currentBranchName);
    }

    var mostRecent = _semanticVersionedGitRepository.GetMostRecentHotfixVersion();

    var possibleVersions = mostRecent.GetCurrentPossibleVersionsHotfix();
    return _inputReader.ReadVersionChoice("Please choose next Version:", possibleVersions);
  }
}