using System;
using ReleaseProcessAutomation.Configuration.Data;
using ReleaseProcessAutomation.Extensions;
using ReleaseProcessAutomation.Git;
using ReleaseProcessAutomation.ReadInput;
using ReleaseProcessAutomation.Scripting;
using ReleaseProcessAutomation.SemanticVersioning;
using Spectre.Console;

namespace ReleaseProcessAutomation.Steps;

public class ContinueReleaseStepWithOptionalSupportBranchStepBase
    : ReleaseProcessStepBase
{
  private readonly IMSBuildCallAndCommit _msBuildCallAndCommit;

  protected ContinueReleaseStepWithOptionalSupportBranchStepBase (IGitClient gitClient, Config config, IInputReader inputReader, IAnsiConsole console, IMSBuildCallAndCommit msBuildCallAndCommit)
      : base(gitClient, config, inputReader, console)
  {
    _msBuildCallAndCommit = msBuildCallAndCommit;
  }

  protected void CreateSupportBranchWithHotfixForRelease (SemanticVersion currentVersion)
  {
    Console.WriteLine("Do you wish to create a new support branch?");
    if (!InputReader.ReadConfirmation())
      return;

    var splitHotfixVersion = currentVersion.GetNextMinor();
    GitClient.CheckoutNewBranch($"support/v{splitHotfixVersion.Major}.{splitHotfixVersion.Minor}");
    GitClient.CheckoutNewBranch($"hotfix/v{splitHotfixVersion}");
    _msBuildCallAndCommit.CallMSBuildStepsAndCommit(MSBuildMode.DevelopmentForNextRelease, splitHotfixVersion);
  }
}