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
using ReleaseProcessAutomation.Configuration.Data;
using ReleaseProcessAutomation.Extensions;
using ReleaseProcessAutomation.Git;
using ReleaseProcessAutomation.ReadInput;
using ReleaseProcessAutomation.SemanticVersioning;
using Serilog;
using Spectre.Console;

namespace ReleaseProcessAutomation.Steps;

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

  protected void MergeBranchAndSkipIgnoredFiles (string currentBranchName, string mergeBranchName, IgnoreListType ignoreListType)
  {
    GitClient.Checkout(currentBranchName);
    GitClient.MergeBranchWithoutCommit(mergeBranchName);
    ResetItemsOfIgnoreList(ignoreListType);
    GitClient.CommitAll($" Merge branch '{mergeBranchName}' into {currentBranchName}");
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

    throw new Exception("Working directory not clean, user does not want to continue. Release process stopped.");
  }

  protected void ResetItemsOfIgnoreList (IgnoreListType ignoreListType)
  {
    var ignoredFiles = Config.GetIgnoredFiles(ignoreListType);

    foreach (var ignoredFile in ignoredFiles)
    {
      _log.Debug("Resetting '{IgnoredFile}'.", ignoredFile);

      GitClient.Reset(ignoredFile);
      GitClient.CheckoutDiscard(ignoredFile);
    }
  }

  protected void CreateTagWithMessage (string tagName)
  {
    GitClient.Tag(tagName, $"Create tag with version {tagName}");
  }

  protected SemanticVersion? CreateNewSupportBranch (SemanticVersion nextVersion)
  {
    Console.WriteLine("Do you wish to create a new support branch?");
    if (!InputReader.ReadConfirmation())
      return null;

    var splitHotfixVersion = nextVersion.GetNextMinor();
    GitClient.CheckoutNewBranch($"support/v{splitHotfixVersion.Major}.{splitHotfixVersion.Minor}");
    GitClient.CheckoutNewBranch($"hotfix/v{splitHotfixVersion}");
    return splitHotfixVersion;
  }
}