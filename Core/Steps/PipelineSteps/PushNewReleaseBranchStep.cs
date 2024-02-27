using Remotion.ReleaseProcessAutomation.Configuration.Data;
using Remotion.ReleaseProcessAutomation.Git;

namespace Remotion.ReleaseProcessAutomation.Steps.PipelineSteps;

public interface IPushNewReleaseBranchStep
{
  void Execute (string releaseBranchName, string firstAncestorBranchName);
}

public class PushNewReleaseBranchStep
    : IPushNewReleaseBranchStep
{
  private readonly IGitClient _gitClient;
  private readonly Config _config;

  public PushNewReleaseBranchStep (IGitClient gitClient, Config config)
  {
    _gitClient = gitClient;
    _config = config;
  }
  
  public void Execute (string releaseBranchName, string firstAncestorBranchName)
  {
    var remoteNames = _config.RemoteRepositories.RemoteNames;
    _gitClient.PushToRepos(remoteNames, releaseBranchName);
    _gitClient.PushToRepos(remoteNames, firstAncestorBranchName);
  }
}