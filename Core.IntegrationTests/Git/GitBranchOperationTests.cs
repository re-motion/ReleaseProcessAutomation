using System;
using System.IO;
using Moq;
using NUnit.Framework;
using Remotion.ReleaseProcessAutomation.Configuration;
using Remotion.ReleaseProcessAutomation.Git;
using Spectre.Console;
using Spectre.Console.Testing;

namespace Remotion.ReleaseProcessAutomation.IntegrationTests.Git;

[TestFixture]
public class GitBranchOperationTests : GitBackedTestBase
{
  private const string c_configFileName = "ReleaseProcessScript.Test.Config";

  private Configuration.Data.Config _config;
  private IAnsiConsole _console;

  [SetUp]
  public void Setup ()
  {
    var path = Path.Join(PreviousWorkingDirectory, c_configFileName);
    _config = new ConfigReader().LoadConfig(path);
    _console = new TestConsole();
  }

  [Test]
  public void EnsureBranchUpToDate_WithoutRemotesInConfig_ThrowsException ()
  {
    _config.RemoteRepositories.RemoteNames = new string[] { };

    var gitClientStub = new Mock<IGitClient>();

    var gitBranchOperation = new GitBranchOperations(gitClientStub.Object, _config);

    Assert.That(
        () => gitBranchOperation.EnsureBranchUpToDate(""),
        Throws.InstanceOf<UserInteractionException>()
            .With.Message.EqualTo("There are no remote repositories specified in the config. Stopping execution."));
  }

  [Test]
  public void EnsureBranchUpToDate_WithOneRemoteUpToDate_DoesNotThrow ()
  {
    _config.RemoteRepositories.RemoteNames = new[]
                                             {
                                                 "origin"
                                             };

    var gitClientStub = new Mock<IGitClient>();
    gitClientStub.Setup(_ => _.GetHash(It.IsAny<string>(), It.IsAny<string>())).Returns("hash");
    gitClientStub.Setup(_ => _.GetMostRecentCommonAncestorWithRemote("branch", "branch", "origin")).Returns("hash");

    var gitBranchOperation = new GitBranchOperations(gitClientStub.Object, _config);

    Assert.That(() => gitBranchOperation.EnsureBranchUpToDate("branch"), Throws.Nothing);
  }

  [Test]
  public void EnsureBranchUpToDate_WithRemoteBehind_DoesNotThrow ()
  {
    _config.RemoteRepositories.RemoteNames = new[]
                                             {
                                                 "origin"
                                             };

    var gitClientStub = new Mock<IGitClient>();
    gitClientStub.Setup(_ => _.GetHash("branch", "")).Returns("laterHash");
    gitClientStub.Setup(_ => _.GetHash("branch", "origin")).Returns("hash");
    gitClientStub.Setup(_ => _.GetMostRecentCommonAncestorWithRemote("branch", "branch", "origin")).Returns("hash");

    var gitBranchOperation = new GitBranchOperations(gitClientStub.Object, _config);

    Assert.That(() => gitBranchOperation.EnsureBranchUpToDate("branch"), Throws.Nothing);
  }

  [Test]
  public void EnsureBranchUpToDate_WithCurrentBehind_ThrowsException ()
  {
    _config.RemoteRepositories.RemoteNames = new[]
                                             {
                                                 "origin"
                                             };

    var gitClientStub = new Mock<IGitClient>();
    gitClientStub.Setup(_ => _.GetHash("branch", "")).Returns("hash");
    gitClientStub.Setup(_ => _.GetHash("branch", "origin")).Returns("laterHash");
    gitClientStub.Setup(_ => _.GetMostRecentCommonAncestorWithRemote("branch", "branch", "origin")).Returns("hash");

    var gitBranchOperation = new GitBranchOperations(gitClientStub.Object, _config);

    Assert.That(
        () => gitBranchOperation.EnsureBranchUpToDate("branch"),
        Throws.InstanceOf<UserInteractionException>()
            .With.Message.EqualTo("Need to pull, local 'branch' branch is behind on repository 'origin'."));
  }

  [Test]
  public void EnsureBranchUpToDate_WithDivergingHashes_DoesThrow ()
  {
    _config.RemoteRepositories.RemoteNames = new[]
                                             {
                                                 "origin"
                                             };

    var gitClientStub = new Mock<IGitClient>();
    gitClientStub.Setup(_ => _.GetHash("branch", "")).Returns("laterHash");
    gitClientStub.Setup(_ => _.GetHash("branch", "origin")).Returns("latestHash");
    gitClientStub.Setup(_ => _.GetMostRecentCommonAncestorWithRemote("branch", "branch", "origin")).Returns("hash");

    var gitBranchOperation = new GitBranchOperations(gitClientStub.Object, _config);

    Assert.That(
        () => gitBranchOperation.EnsureBranchUpToDate("branch"),
        Throws.InstanceOf<UserInteractionException>()
            .With.Message.EqualTo("'branch' diverged, need to rebase at repository 'origin'."));
  }
}