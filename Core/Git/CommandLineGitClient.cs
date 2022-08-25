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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using Serilog;

namespace ReleaseProcessAutomation.Git;

public class CommandLineGitClient : IGitClient
{
  private class CommandLineResult
  {
    public bool Success { get; }
    public string Output { get; }

    public CommandLineResult (bool success, string output)
    {
      Success = success;
      Output = output;
    }
  }

  private const int c_gitProcessWaitTimeout = 30_000;

  private readonly ILogger _log = Log.ForContext<CommandLineGitClient>();

  public CommandLineGitClient ()
  {
    if (!IsMinGitVersion())
    {
      var currentVersion = ExecuteGitCommandWithOutput("version");
      var message = $"Could not find a recent enough git version. Version 2.7.0 or higher is required, version found is '{currentVersion}'";
      throw new InvalidOperationException(message);
    }
  }

  public bool DoesRemoteBranchExist (string remoteName, string branchName)
  {
    var output = ExecuteGitCommandWithOutput($"ls-remote --heads {remoteName} {branchName}");

    return output.Output.Length > 0 && output.Success;
  }

  public bool IsMinGitVersion ()
  {
    var minGitVersion = new[] { "2", "7", "0" };
    var currentVersion = ExecuteGitCommandWithOutput("version");
    if (!currentVersion.Success)
      return false;

    var regex = new Regex("\\d+(\\.\\d+)+");

    var match = regex.Match(currentVersion.Output);

    if (!match.Success)
      return false;
    var currentVersionArray = match.Value.Split('.');

    for (var i = 0; i < minGitVersion.Length; i++)
    {
      if (string.Compare(currentVersionArray[i], minGitVersion[i]) > 0 || currentVersionArray[i].Length > minGitVersion[i].Length)
        return true;

      if (string.Compare(currentVersionArray[i], minGitVersion[i]) < 0 || currentVersionArray[i].Length < minGitVersion[i].Length)
        return false;
    }

    //If its not bigger and not smaller, its equal!
    return true;
  }

  public string Push (string arguments)
  {
    var push = ExecuteGitCommandWithOutput($"push {arguments}");
    return push.Output;
  }

  public bool DoesBranchExist (string branchName)
  {
    var output = ExecuteGitCommandWithOutput($"show-ref --verify refs/heads/{branchName}");

    return output.Output.Length > 0 && output.Success;
  }

  public bool DoesTagExist (string tagName)
  {
    var output = ExecuteGitCommandWithOutput($"show-ref --verify refs/tags/{tagName}");

    return output.Output.Length > 0 && output.Success;
  }

  public string? GetRemoteOfBranch (string branchName)
  {
    var output = ExecuteGitCommandWithOutput($"config branch.{branchName}.remote");
    if (!output.Success)
      return null;

    return output.Output.Trim('#').Trim();
  }

  public string? GetCurrentBranchName ()
  {
    var name = ExecuteGitCommandWithOutput("symbolic-ref --short HEAD");
    if (!name.Success)
      return null;

    return name.Output.Trim('*', ' ').Replace("\n", string.Empty);
  }

  public IReadOnlyCollection<string> GetAncestors (params string[] expectedAncestor)
  {
    var branchName = GetCurrentBranchName();
    if (string.IsNullOrEmpty(branchName))
      return Array.Empty<string>();

    var allAncestorsString = ExecuteGitCommandWithOutput("show-branch");
    var allAncestors = allAncestorsString.Output.Split("\n", StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim());

    var selectAncestors = allAncestors
        .Where(ancestor => ancestor.Contains('!'))
        .Where(ancestor => !ancestor.Contains(branchName))
        .ToArray();

    for (var i = 0; i < selectAncestors.Length; i++)
    {
      selectAncestors[i] = selectAncestors[i][(selectAncestors[i].IndexOf('[') + 1)..];
      selectAncestors[i] = selectAncestors[i][..selectAncestors[i].IndexOf(']')];
    }

    var foundAncestors = new List<string>();

    foreach (var ancestor in selectAncestors)
    foreach (var expected in expectedAncestor)
      if (expected.Contains(ancestor) || ancestor.Contains(expected))
        foundAncestors.Add(ancestor);

    return foundAncestors;
  }

  public bool IsCommitHash (string? commitHash)
  {
    var hashValidation = ExecuteGitCommandWithOutput($"cat-file -t {commitHash}");

    return hashValidation.Success && string.Equals(hashValidation.Output, "commit\n");
  }

  public bool IsOnBranch (string branchName)
  {
    var git = ExecuteGitCommandWithOutput("symbolic-ref --short -q HEAD");

    if (!git.Success)
      return false;

    var output = git.Output.Replace("\n", string.Empty);

    return string.Equals(output, branchName) || branchName.EndsWith('/') && output.StartsWith(branchName);
  }

  public IReadOnlyCollection<string> GetTags (string from = "HEAD", string to = "")
  {
    CommandLineResult allVersionsOutput;
    if (string.IsNullOrEmpty(to))
      allVersionsOutput = ExecuteGitCommandWithOutput($"tag --merged={from}");
    else
      allVersionsOutput = ExecuteGitCommandWithOutput($"tag --merged={from} --contains={to}");

    if (!allVersionsOutput.Success)
    {
      var currentBranchName = GetCurrentBranchName();
      var currentCommit = ExecuteGitCommandWithOutput("rev-parse HEAD");
      var message =
          $"Could not find tags from '{from}' to '{to}' while on branch '{currentBranchName}' from commit '{currentCommit}'. \nGit error: \n{allVersionsOutput.Output}";
      throw new InvalidOperationException(message);
    }

    var allVersionsSplit = allVersionsOutput.Output.Split("\n").Select(s => s.Trim());

    return allVersionsSplit.ToArray();
  }

  public bool IsWorkingDirectoryClean ()
  {
    var status = ExecuteGitCommandWithOutput("status --porcelain");

    if (!status.Success || string.IsNullOrEmpty(status.Output))
      return true;

    return false;
  }

  public string CheckoutCommitWithNewBranch (string? commitHash, string branchName)
  {
    var checkout = ExecuteGitCommandWithOutput($"checkout {commitHash} -b {branchName}");

    if (!checkout.Success)
    {
      var message = $"Could not checkout commit '{commitHash}'.\nGit error:\n{checkout.Output}";
      throw new InvalidOperationException(message);
    }

    return checkout.Output;
  }

  public string Checkout (string toCheckout)
  {
    var checkout = ExecuteGitCommandWithOutput($"checkout {toCheckout}");

    if (!checkout.Success)
    {
      var currentBranch = GetCurrentBranchName();
      var message = $"Could not checkout '{toCheckout}' from branch {currentBranch}.\nGit error:\n{checkout.Output}";
      throw new InvalidOperationException(message);
    }

    return checkout.Output;
  }

  public string CheckoutNewBranch (string branchName)
  {
    var checkout = ExecuteGitCommandWithOutput($"checkout -b {branchName}");

    if (!checkout.Success)
    {
      var message = $"Could not checkout new branch '{branchName}'.\nGit error:\n{checkout.Output}";
      throw new Exception(message);
    }

    return checkout.Output;
  }

  public void MergeBranchWithoutCommit (string branchName)
  {
    var merge = ExecuteGitCommandWithOutput($"merge {branchName} --no-ff --no-commit");
    if (!merge.Success)
    {
      var currentBranch = GetCurrentBranchName();
      var message = $"Could not merge branch {branchName} into {currentBranch}.\nGit error: \n{merge.Output}";
      throw new Exception(message);
    }
  }

  public void MergeBranchToOnlyContainChangesFromMergedBranch (string branchName)
  {
    var currentBranchName = GetCurrentBranchName()!;
    var intoMessage = currentBranchName.Equals("master") ? "" : $" into {currentBranchName}";
    var hash = ExecuteGitCommandWithOutput(
        $"commit-tree -m \"Merge branch '{branchName}'{intoMessage}\" -p HEAD -p {branchName} {branchName}:");
    ExecuteGitCommandWithOutput($"update-ref HEAD {hash.Output.Trim()}");
    ExecuteGitCommand("reset --hard");

    var diffOutput = ExecuteGitCommandWithOutput($"diff {branchName}");
    if (!diffOutput.Success || !diffOutput.Output.Equals(string.Empty))
    {
      var message = $"Could not merge branch {branchName} into {currentBranchName}.\nGit error: \n{diffOutput.Output}";
      throw new Exception(message);
    }
  }

  public void CommitAll (string message)
  {
    var commit = ExecuteGitCommandWithOutput($"commit -am \"{message}\" --allow-empty");
    if (!commit.Success)
    {
      var status = ExecuteGitCommandWithOutput("status");
      var errorMessage = $"Could not commit on branch.\nGit status:\n{status.Output}\n\nGit error:\n{commit.Output}";
      throw new Exception(errorMessage);
    }
  }

  public void AddAll ()
  {
    var add = ExecuteGitCommandWithOutput("add --all");
    if (!add.Success)
    {
      var status = ExecuteGitCommandWithOutput("status");
      var message = $"Could not add all to next commit. Git status:\n{status.Output}\n\nGit error:\n{add.Output}";
      throw new Exception(message);
    }
  }

  public void ResolveMergeConflicts ()
  {
    var mergeConflicts = ExecuteGitCommandWithOutput("diff --name-only --diff-filter=U");
    if (!string.IsNullOrEmpty(mergeConflicts.Output))
    {
      ExecuteGitCommand($"mergetool {mergeConflicts.Output}");
      ExecuteGitCommand("commit --file .git/MERGE_MSG");
    }
  }

  public void Reset (string fileName)
  {
    ExecuteGitCommand($"reset HEAD {fileName}");
  }

  public void CheckoutDiscard (string fileName)
  {
    ExecuteGitCommand($"checkout -- {fileName}");
  }

  public void Tag (string tagName, string message)
  {
    var argumentedMessage = string.IsNullOrEmpty(message) ? "" : $"-m \"{message}\"";
    var arguments = $"tag {tagName} {argumentedMessage}";
    ExecuteGitCommand(arguments);
  }

  public string? Fetch (string arguments)
  {
    var fetch = ExecuteGitCommandWithOutput($"fetch {arguments}");
    if (!fetch.Success)
      return null;
    return fetch.Output;
  }

  public string? GetHash (string branch, string? remoteName = "")
  {
    var arguments = branch;
    if (!string.IsNullOrEmpty(remoteName))
      arguments = $"{remoteName}/{branch}";

    var hash = ExecuteGitCommandWithOutput($"rev-parse {arguments}");
    if (!hash.Success)
      return null;

    return hash.Output;
  }

  public string? GetMostRecentCommonAncestorWithRemote (string branch, string branchOnRemote, string remote)
  {
    var commonAncestor = ExecuteGitCommandWithOutput($"merge-base {branch} {remote}/{branchOnRemote}");

    if (!commonAncestor.Success)
      return null;

    return commonAncestor.Output;
  }

  public void PushToRepos (IReadOnlyCollection<string> remoteNames, string branchName, string? tagName = null)
  {
    var beforeBranchName = GetCurrentBranchName();
    if (string.IsNullOrEmpty(beforeBranchName))
    {
      var status = ExecuteGitCommandWithOutput("status");
      var message = $"Could not determine current branch while trying to push to repos. Git status:\n{status}";
      throw new InvalidOperationException(message);
    }

    Checkout(branchName);

    if (!string.IsNullOrEmpty(tagName) && !DoesTagExist(tagName))
    {
      var message = $"Tag with name '{tagName}' does not exist, must not have been created before calling pushToRepos, check previous steps.";
      throw new InvalidOperationException(message);
    }

    foreach (var remoteName in remoteNames)
      if (!string.IsNullOrEmpty(remoteName))
      {
        _log.Debug("Pushing branch '{BranchName}' and tag '{TagName}' to remote '{RemoteName}'.", branchName, tagName, remoteName);

        var upstream = "";

        var remoteNameOfBranch = GetRemoteOfBranch(branchName);

        if (string.IsNullOrEmpty(remoteNameOfBranch))
        {
          var ancestor = GetFirstAncestor();
          var remoteNameOfAncestor = "";

          if (!string.IsNullOrEmpty(ancestor))
            remoteNameOfAncestor = GetRemoteOfBranch(ancestor);

          if (string.IsNullOrEmpty(remoteNameOfAncestor))
            if (remoteName.Equals(remoteNames.First()))
              upstream = "-u";
        }
        else if (remoteNameOfBranch.Equals(remoteName))
        {
          upstream = "-u";
        }

        Push($"{upstream} {remoteName} {branchName} {tagName}");
      }

    Checkout(beforeBranchName);
  }

  private string GetFirstAncestor ()
  {
    var branchName = GetCurrentBranchName();
    if (string.IsNullOrEmpty(branchName))
    {
      var currentCommit = ExecuteGitCommandWithOutput("rev-parse HEAD");
      var message = $"Did not find current branch when trying to find first ancestor. Current commit hash: '{currentCommit}'.";
      throw new InvalidOperationException(message);
    }

    var allAncestorsString = ExecuteGitCommandWithOutput("show-branch");

    if (!allAncestorsString.Success)
    {
      var message = $"Did not find first ancestor on branch '{branchName}'.\nGit error:\n{allAncestorsString.Output}";
      throw new InvalidOperationException(message);
    }

    var allAncestors = allAncestorsString.Output.Split("\n", StringSplitOptions.RemoveEmptyEntries);

    var selectAncestors = allAncestors
        .Where(ancestor => ancestor.Contains('*'))
        .Where(ancestor => !ancestor.Contains(branchName))
        .ToArray();

    if (selectAncestors.Length == 0)
      return string.Empty;

    var selectAncestor = selectAncestors[0];

    return selectAncestor.Substring(0, selectAncestor.IndexOf(']')).Substring(selectAncestor.IndexOf('[') + 1);
  }

  private CommandLineResult ExecuteGitCommandWithOutput (string arguments)
  {
    _log.Debug("Executing git command with output: git '{Arguments}'.", arguments);

    var psi = new ProcessStartInfo("git", arguments)
              {
                  UseShellExecute = false,
                  RedirectStandardOutput = true,
                  RedirectStandardError = true
              };

    using var command = Process.Start(psi);
    command!.WaitForExit(c_gitProcessWaitTimeout);

    var success = command.ExitCode == 0;
    if (!success)
      return new CommandLineResult(success, command.StandardError.ReadToEnd());

    var output = command.StandardOutput.ReadToEnd();

    return new CommandLineResult(success, output);
  }

  private void ExecuteGitCommand (string arguments)
  {
    _log.Debug("Executing git command: git '{Arguments}'.", arguments);

    using var command = Process.Start("git", arguments);
    command.WaitForExit(c_gitProcessWaitTimeout);
  }
}