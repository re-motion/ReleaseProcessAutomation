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

  protected void EnsureBranchUpToDate (string branchName)
  {
    _log.Debug("Ensuring branch '{BranchName}' is up to date.", branchName);

    GitClient.Checkout(branchName);

    var remoteNames = Config.RemoteRepositories.RemoteNames.Where(n => n is { Length: > 0 }).ToArray();

    if (remoteNames.Length == 0)
    {
      const string message = "There were no remotes specified in the config. Stopping execution.";
      throw new InvalidOperationException(message);
    }

    foreach (var remoteName in remoteNames)
    {
      if (!string.IsNullOrEmpty(GitClient.GetRemoteOfBranch(remoteName)))
        continue;

      var fetch = GitClient.Fetch($"{remoteName} {branchName}");
      if (fetch != null)
        Console.WriteLine(fetch);

      var local = GitClient.GetHash(branchName) ?? "";
      var remote = GitClient.GetHash(branchName, remoteName) ?? "";
      var basis = GitClient.GetMostRecentCommonAncestorWithRemote(branchName, branchName, remoteName) ?? "";

      if (local.Equals(remote))
      {
        _log.Debug("'{BranchName}' and remote '{RemoteName}' are up to date.", branchName, remoteName);
        //Up-To-Date. OK
      }
      else if (local.Equals(basis))
      {
        var message = $"Need to pull, local '{branchName}' branch is behind on repository '{remoteName}'.";
        throw new InvalidOperationException(message);
      }
      else if (remote.Equals(basis))
      {
        _log.Debug("Remote branch on '{RemoteName}' is behind of '{BranchName}'.", remoteName, branchName);
        //Need to push, remote branch is behind. Ok
      }
      else
      {
        var message = $"'{branchName}' diverged, need to rebase at repository '{remoteName}'.";
        throw new InvalidOperationException(message);
      }
    }
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