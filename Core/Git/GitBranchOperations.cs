using System;
using System.Linq;
using ReleaseProcessAutomation.Configuration.Data;
using Serilog;

namespace ReleaseProcessAutomation.Git;

public class GitBranchOperations : IGitBranchOperations
{
  private readonly IGitClient _gitClient;
  private readonly Config _config;
  private readonly ILogger _log = Log.ForContext<GitBranchOperations>();

  public GitBranchOperations (IGitClient gitClient, Config config)
  {
    _gitClient = gitClient;
    _config = config;
  }
  
  public void EnsureBranchUpToDate (string branchName)
  
  {
    var beforeBranchName = _gitClient.GetCurrentBranchName();
    var remoteNames = _config.RemoteRepositories.RemoteNames.Where(n => n is { Length: >0 }).ToArray();

    if (remoteNames.Length == 0)
    {
      const string message = "There were no remotes specified in the config. Stopping execution";
      throw new InvalidOperationException(message);
    }

    _log.Debug("Ensuring branch '{BranchName}' is up to date", branchName);

    _gitClient.Checkout(branchName);

    foreach (var remoteName in remoteNames)
    {
      if (!string.IsNullOrEmpty(_gitClient.GetRemoteOfBranch(remoteName)))
        continue;

      var fetch = _gitClient.Fetch($"{remoteName} {branchName}");
      if (fetch != null)
        _log.Debug(fetch);

      var local = _gitClient.GetHash(branchName) ?? "";
      var remote = _gitClient.GetHash(branchName, remoteName) ?? "";
      var basis = _gitClient.GetMostRecentCommonAncestorWithRemote(branchName, branchName, remoteName) ?? "";

      if (local.Equals(remote))
      {
        _log.Debug("'{BranchName}' and remote '{RemoteName}' are up to date", branchName, remoteName);
        //Up-To-Date. OK
      }
      else if (local.Equals(basis))
      {
        _gitClient.Checkout(beforeBranchName!);
        var message = $"Need to pull, local '{branchName}' branch is behind on repository '{remoteName}'";
        throw new InvalidOperationException(message);
      }
      else if (remote.Equals(basis))
      {
        _gitClient.Checkout(beforeBranchName!);
        _log.Debug("Remote branch on '{RemoteName}' is behind of '{BranchName}'", remoteName, branchName);
        //Need to push, remote branch is behind. Ok
      }
      else
      {
        var message = $"'{branchName}' diverged, need to rebase at repository '{remoteName}'";
        throw new InvalidOperationException(message);
      }
    }
    _gitClient.Checkout(beforeBranchName!);
  }
}