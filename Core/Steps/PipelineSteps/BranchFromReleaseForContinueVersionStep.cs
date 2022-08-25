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
using ReleaseProcessAutomation.SemanticVersioning;
using Serilog;
using Spectre.Console;

namespace ReleaseProcessAutomation.Steps.PipelineSteps;

/// <summary>
///   Should only be called by ContinueReleaseStep when on the release branch.
///   Determines which further steps to take based on the ancestor of the current branch.
/// </summary>
public interface IBranchFromReleaseForContinueVersionStep
{
  void Execute (SemanticVersion nextVersion, string? ancestor, bool noPush);
}

/// <inheritdoc cref="IBranchFromReleaseForContinueVersionStep" />
public class BranchFromReleaseForContinueVersionStep
    : ReleaseProcessStepBase, IBranchFromReleaseForContinueVersionStep
{
  private readonly IAncestorFinder _ancestorFinder;
  private readonly IContinueReleaseOnMasterStep _continueReleaseOnMasterStep;
  private readonly IContinueReleasePatchStep _continueReleasePatchStep;
  private readonly ILogger _log = Log.ForContext<BranchFromReleaseForContinueVersionStep>();

  public BranchFromReleaseForContinueVersionStep (
      IGitClient gitClient,
      Config config,
      IInputReader inputReader,
      IAncestorFinder ancestorFinder,
      IContinueReleaseOnMasterStep continueReleaseOnMasterStep,
      IContinueReleasePatchStep continueReleasePatchStep,
      IAnsiConsole console)
      : base(gitClient, config, inputReader, console)
  {
    _ancestorFinder = ancestorFinder;
    _continueReleaseOnMasterStep = continueReleaseOnMasterStep;
    _continueReleasePatchStep = continueReleasePatchStep;
  }

  public void Execute (SemanticVersion nextVersion, string? ancestor, bool noPush)
  {
    if (string.IsNullOrEmpty(ancestor))
      ancestor = _ancestorFinder.GetAncestor("develop", "hotfix/v");

    _log.Debug("The given/found ancestor is '{Ancestor}'.", ancestor);

    if (ancestor.Equals("develop"))
    {
      _log.Debug("The ancestor is develop, therefore calling continue release on master.");
      _continueReleaseOnMasterStep.Execute(nextVersion, noPush);
    }
    else if (ancestor.StartsWith("hotfix/"))
    {
      _log.Debug("The ancestor is hotfix/, therefore calling continue release patch.");
      _continueReleasePatchStep.Execute(nextVersion, noPush, false);
    }
    else
    {
      var message = $"Ancestor has to be either 'develop' or a 'hotfix/v*.*.*' branch but was '{ancestor}'.";
      throw new InvalidOperationException(message);
    }
  }
}