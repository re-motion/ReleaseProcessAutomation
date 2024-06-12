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
using Remotion.ReleaseProcessAutomation.Configuration.Data;
using Remotion.ReleaseProcessAutomation.Extensions;
using Remotion.ReleaseProcessAutomation.Git;
using Remotion.ReleaseProcessAutomation.ReadInput;
using Remotion.ReleaseProcessAutomation.SemanticVersioning;
using Serilog;
using Spectre.Console;

namespace Remotion.ReleaseProcessAutomation.Steps;

/// <summary>
///   Abstract class to hold some much used methods for the other steps.
/// </summary>
public abstract class ReleaseProcessStepBase
{
  protected readonly Config Config;
  protected readonly IAnsiConsole Console;
  protected readonly IGitClient GitClient;
  protected readonly IInputReader InputReader;

  private readonly ILogger _log = Log.ForContext<ReleaseProcessStepBase>();

  protected ReleaseProcessStepBase (IGitClient gitClient, Config config, IInputReader inputReader, IAnsiConsole console)
  {
    GitClient = gitClient;
    Config = config;
    InputReader = inputReader;
    Console = console;
  }

  protected void ResetItemsForMerge (string intoBranchName, string mergeBranchName, IgnoreListType ignoreListType)
  {
    GitClient.Checkout(mergeBranchName);

    var ignoredFiles = Config.GetIgnoredFiles(ignoreListType);

    foreach (var ignoredFile in ignoredFiles)
    {
      _log.Debug("Resetting '{IgnoredFile}'.", ignoredFile);

      GitClient.Reset(ignoredFile, intoBranchName);

      GitClient.CheckoutDiscard(ignoredFile);
    }
    GitClient.CommitAll("Reset metadata for merge.");
  }

  protected void MergeBranch (string intoBranchName, string mergeBranchName)
  {
    GitClient.Checkout(intoBranchName);
    GitClient.MergeBranchWithoutCommit(mergeBranchName);
    GitClient.CommitAll($"Merge branch '{mergeBranchName}' into {intoBranchName}");
    GitClient.ResolveMergeConflicts();
  }

  protected void EnsureWorkingDirectoryClean ()
  {
    if (GitClient.IsWorkingDirectoryClean())
      return;

    _log.Warning("Working directory not clean, asking for user input if the execution should continue.");
    Console.WriteLine("Your Working directory is not clean, do you still wish to continue?");
    var shouldContinue = InputReader.ReadConfirmation();

    if (shouldContinue)
    {
      _log.Debug("User wants to continue.");
      return;
    }

    throw new UserInteractionException("Working directory not clean, user does not want to continue. Release process stopped.");
  }

  protected void CreateTagWithMessage (string tagName)
  {
    GitClient.Tag(tagName, $"Create tag with version {tagName}");
  }
}