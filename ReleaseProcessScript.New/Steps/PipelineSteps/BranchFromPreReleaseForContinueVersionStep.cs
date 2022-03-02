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
using ReleaseProcessScript.New.Configuration.Data;
using ReleaseProcessScript.New.Git;
using ReleaseProcessScript.New.ReadInput;
using ReleaseProcessScript.New.SemanticVersioning;
using Serilog;
using Spectre.Console;

namespace ReleaseProcessScript.New.Steps.PipelineSteps;

/// <summary>
///   Should only be called when on the prerelease branch when StartReleaseStep was ended early with the option
///   PauseForCommit.
///   Always calls continueReleaseAlphaBeta.
/// </summary>
public interface IBranchFromPreReleaseForContinueVersionStep
{
  void Execute (SemanticVersion nextVersion, string? ancestor, bool noPush);
}

/// <inheritdoc cref="IBranchFromPreReleaseForContinueVersionStep" />
public class BranchFromPreReleaseForContinueVersionStep
    : ReleaseProcessStepBase, IBranchFromPreReleaseForContinueVersionStep
{
  private readonly IContinueAlphaBetaStep _continueReleaseAlphaBetaStep;

  public BranchFromPreReleaseForContinueVersionStep (
      IGitClient gitClient,
      Config config,
      IInputReader inputReader,
      IContinueAlphaBetaStep continueReleaseAlphaBetaStep,
      IAnsiConsole console)
      : base(gitClient, config, inputReader, console)
  {
    _continueReleaseAlphaBetaStep = continueReleaseAlphaBetaStep;
  }

  public void Execute (SemanticVersion nextVersion, string? ancestor, bool noPush)
  {
    _continueReleaseAlphaBetaStep.Execute(nextVersion, ancestor, "", noPush);
  }
}
