namespace ReleaseProcessAutomation.Git;

public interface IGitBranchOperations
{
  void EnsureBranchUpToDate (string branchName);
  
}