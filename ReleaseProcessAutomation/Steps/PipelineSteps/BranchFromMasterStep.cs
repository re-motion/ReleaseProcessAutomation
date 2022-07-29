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
using ReleaseProcessAutomation.SemanticVersioning;
using Serilog;

namespace ReleaseProcessAutomation.Steps.PipelineSteps;

/// <summary>
///   Should be reworked since having swapped to support branches.
///   Finds the version that is to be released.
///   Always continues with releasePatchStep.
/// </summary>
public interface IBranchFromMasterStep
{
  void Execute (string? commitHash, bool startReleasePhase, bool pauseForCommit, bool noPush);
}

/// <inheritdoc cref="IBranchFromMasterStep" />
public class BranchFromMasterStep
    : IBranchFromMasterStep
{
  private readonly IGitClient _gitClient;
  private readonly IReleasePatchStep _releasePatchStep;
  private readonly ISemanticVersionedGitRepository _semanticVersionedGitRepository;
  private readonly ILogger _log = Log.ForContext<BranchFromMasterStep>();

  public BranchFromMasterStep (
      IGitClient gitClient,
      ISemanticVersionedGitRepository semanticVersionedGitRepository,
      IReleasePatchStep releasePatchStep)
  {
    _gitClient = gitClient;
    _semanticVersionedGitRepository = semanticVersionedGitRepository;
    _releasePatchStep = releasePatchStep;
  }

  private SemanticVersion FindNextPatch (SemanticVersion version)
  {
    var nextVersion = version.GetNextPatchVersion();
    while (_gitClient.DoesTagExist($"v{nextVersion}"))
      nextVersion = nextVersion.GetNextPatchVersion();

    return nextVersion;
  }

  public void Execute (string? commitHash, bool startReleasePhase, bool pauseForCommit, bool noPush)
  {
    _semanticVersionedGitRepository.TryGetCurrentVersion(out var currentVersion, "master");
    _log.Debug("The current found version is '{CurrentVersion}'", currentVersion);

    var nextVersion = FindNextPatch(currentVersion ?? throw new InvalidOperationException("No current version exists."));
    _log.Debug("The next version to be released is '{NextVersion}'", nextVersion);

    _releasePatchStep.Execute(nextVersion, commitHash, startReleasePhase, pauseForCommit, noPush, true);
  }
}