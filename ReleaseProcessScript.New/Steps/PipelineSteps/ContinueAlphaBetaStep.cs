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
///   Should only be called when on a prerelease branch.
///   Determines and creates appropriate tag and merges the changes made by the previous steps.
///   Continues with pushAlphaBeta.
/// </summary
public interface IContinueAlphaBetaStep
{
  public void Execute (SemanticVersion nextVersion, string? ancestor, string? currentBranchName, bool noPush);
}

/// <inheritdoc cref="IContinueAlphaBetaStep" />
public class ContinueAlphaBetaStep : ReleaseProcessStepBase, IContinueAlphaBetaStep
{
  private readonly IAncestorFinder _ancestorFinder;
  private readonly IPushPreReleaseStep _pushPreReleaseStep;
  private readonly ILogger _log = Log.ForContext<ContinueAlphaBetaStep>();

  public ContinueAlphaBetaStep (
      IGitClient gitClient,
      Config config,
      IInputReader inputReader,
      IAncestorFinder ancestorFinder,
      IPushPreReleaseStep pushPreReleaseStep,
      IAnsiConsole console)
      : base(gitClient, config, inputReader, console)
  {
    _ancestorFinder = ancestorFinder;
    _pushPreReleaseStep = pushPreReleaseStep;
  }

  public void Execute (SemanticVersion nextVersion, string? ancestor, string? currentBranchName, bool noPush)
  {
    EnsureWorkingDirectoryClean();

    var preReleaseBranchName = GitClient.GetCurrentBranchName();
    _log.Debug("The current found branch name is '{PrereleaseBranchName}'", preReleaseBranchName);
    
    if (preReleaseBranchName == null)
    {
      _log.Error("Could not find current branch while at continueAlphaBetaStep before creating tag and merging");
      throw new InvalidOperationException("Could not find current branch.");
    }

    if (!preReleaseBranchName.StartsWith("prerelease/"))
    {
      const string message = "Cannot call ContinuePreReleaseFromDevelop when not on prerelease branch";
      _log.Error(message);
      throw new InvalidOperationException(message);
    }

    var baseBranchName = ancestor switch
    {
        { Length: > 0 } => ancestor,
        _ => _ancestorFinder.GetAncestor("release/v", "develop", "hotfix/v")
    };

    EnsureBranchUpToDate(baseBranchName);
    EnsureBranchUpToDate(preReleaseBranchName);

    GitClient.Checkout(preReleaseBranchName);

    var tagName = $"v{nextVersion}";
    _log.Debug("Will try to create tag with name '{TagName}'", tagName);
    if (GitClient.DoesTagExist(tagName))
    {
      var message = $"Could not create tag {tagName} because it already exists";
      _log.Error(message);
      throw new InvalidOperationException(message);
    }

    GitClient.Tag($"-a {tagName} -m {tagName}");

    MergeBranchWithReset(baseBranchName, preReleaseBranchName, IgnoreListType.PreReleaseMergeIgnoreList);

    if (noPush)
      return;

    _pushPreReleaseStep.Execute(preReleaseBranchName, currentBranchName!, tagName);
  }
}
