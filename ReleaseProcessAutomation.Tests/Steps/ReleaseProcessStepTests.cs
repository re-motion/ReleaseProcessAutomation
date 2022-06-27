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
using System.IO;
using Moq;
using NUnit.Framework;
using ReleaseProcessAutomation.Configuration;
using ReleaseProcessAutomation.Configuration.Data;
using ReleaseProcessAutomation.Git;
using ReleaseProcessAutomation.ReadInput;
using ReleaseProcessAutomation.Steps;
using Spectre.Console;
using Spectre.Console.Testing;

namespace ReleaseProcessAutomation.Tests.Steps;

[TestFixture]
internal class ReleaseProcessStepTests : GitBackedTests
{
  [SetUp]
  public void Setup ()
  {
    var path = Path.Join(PreviousWorkingDirectory, c_configFileName);
    _config = new ConfigReader().LoadConfig(path);
    _console = new TestConsole();
  }

  private class NestedReleaseProcessStepBase : ReleaseProcessStepBase
  {
    public NestedReleaseProcessStepBase (
        IGitClient gitClient, 
        Configuration.Data.Config config, 
        IInputReader inputReader,
        IAnsiConsole console)
        : base(gitClient, config, inputReader, console)
    {
    }

    public new void EnsureBranchUpToDate (string branchName)
    {
      base.EnsureBranchUpToDate(branchName);
    }

    public new void EnsureWorkingDirectoryClean ()
    {
      base.EnsureWorkingDirectoryClean();
    }

    public new void ResetItemsOfIgnoreList (IgnoreListType ignoreListType)
    {
      base.ResetItemsOfIgnoreList(ignoreListType);
    }
  }

  private Configuration.Data.Config _config;
  private const string c_configFileName = "ReleaseProcessScript.Test.Config";

  private IAnsiConsole _console;

  [Test]
  public void EnsureBranchUpToDate_WithoutProperConfig_ThrowsException ()
  {
    _config.RemoteRepositories.RemoteNames = new string[] { };

    var gitClientStub = new Mock<IGitClient>();
    var readerMock = new Mock<IInputReader>();
    var rps = new NestedReleaseProcessStepBase(gitClientStub.Object, _config, readerMock.Object, _console);

    Assert.That(
        () => rps.EnsureBranchUpToDate(""),
        Throws.InstanceOf<InvalidOperationException>()
            .With.Message.EqualTo("There were no remotes specified in the config. Stopping execution"));
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

    var readerMock = new Mock<IInputReader>();
    var rps = new NestedReleaseProcessStepBase(gitClientStub.Object, _config, readerMock.Object, _console);

    Assert.That(() => rps.EnsureBranchUpToDate("branch"), Throws.Nothing);
  }

  [Test]
  public void EnsureBranchUpToDate_WithRemoteBehindUpToDate_DoesNotThrow ()
  {
    _config.RemoteRepositories.RemoteNames = new[]
                                             {
                                                 "origin"
                                             };

    var gitClientStub = new Mock<IGitClient>();
    gitClientStub.Setup(_ => _.GetHash("branch", "")).Returns("laterHash");
    gitClientStub.Setup(_ => _.GetHash("branch", "origin")).Returns("hash");
    gitClientStub.Setup(_ => _.GetMostRecentCommonAncestorWithRemote("branch", "branch", "origin")).Returns("hash");

    var readerMock = new Mock<IInputReader>();
    var rps = new NestedReleaseProcessStepBase(gitClientStub.Object, _config, readerMock.Object, _console);

    Assert.That(() => rps.EnsureBranchUpToDate("branch"), Throws.Nothing);
  }

  [Test]
  public void EnsureBranchUpToDate_WithCurrentBehindUpToDate_DoesThrow ()
  {
    _config.RemoteRepositories.RemoteNames = new[]
                                             {
                                                 "origin"
                                             };

    var gitClientStub = new Mock<IGitClient>();
    gitClientStub.Setup(_ => _.GetHash("branch", "")).Returns("hash");
    gitClientStub.Setup(_ => _.GetHash("branch", "origin")).Returns("laterHash");
    gitClientStub.Setup(_ => _.GetMostRecentCommonAncestorWithRemote("branch", "branch", "origin")).Returns("hash");

    var readerMock = new Mock<IInputReader>();
    var rps = new NestedReleaseProcessStepBase(gitClientStub.Object, _config, readerMock.Object, _console);

    Assert.That(
        () => rps.EnsureBranchUpToDate("branch"),
        Throws.InstanceOf<InvalidOperationException>()
            .With.Message.EqualTo("Need to pull, local 'branch' branch is behind on repository 'origin'"));
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

    var readerMock = new Mock<IInputReader>();
    var rps = new NestedReleaseProcessStepBase(gitClientStub.Object, _config, readerMock.Object, _console);

    Assert.That(
        () => rps.EnsureBranchUpToDate("branch"),
        Throws.InstanceOf<InvalidOperationException>()
            .With.Message.EqualTo("'branch' diverged, need to rebase at repository 'origin'"));
  }

  [Test]
  public void EnsureWorkingDirectoryClean_CleanDir_DoesNotThrow ()
  {
    var gitClientStub = new Mock<IGitClient>();
    gitClientStub.Setup(_ => _.IsWorkingDirectoryClean()).Returns(true);

    var readerMock = new Mock<IInputReader>();
    var rps = new NestedReleaseProcessStepBase(gitClientStub.Object, _config, readerMock.Object,_console);

    Assert.That(() => rps.EnsureWorkingDirectoryClean(), Throws.Nothing);
  }

  [Test]
  public void EnsureWorkingDirectoryClean_UncleanDirIgnored_DoesNotThrow ()
  {
    var gitClientStub = new Mock<IGitClient>();
    gitClientStub.Setup(_ => _.IsWorkingDirectoryClean()).Returns(false);
    var readInputStub = new Mock<IInputReader>();
    readInputStub.Setup(_ => _.ReadConfirmation(true)).Returns(true);

    var rps = new NestedReleaseProcessStepBase(gitClientStub.Object, _config, readInputStub.Object, _console);

    Assert.That(() => rps.EnsureWorkingDirectoryClean(), Throws.Nothing);
  }

  [Test]
  public void EnsureWorkingDirectoryClean_UncleanDir_DoesThrow ()
  {
    var gitClientStub = new Mock<IGitClient>();
    gitClientStub.Setup(_ => _.IsWorkingDirectoryClean()).Returns(false);
    var readInputStub = new Mock<IInputReader>();
    readInputStub.Setup(_ => _.ReadConfirmation(true)).Returns(false);

    var rps = new NestedReleaseProcessStepBase(gitClientStub.Object, _config, readInputStub.Object,_console);

    
    
    Assert.That(
        () => rps.EnsureWorkingDirectoryClean(),
        Throws.InstanceOf<Exception>()
            .With.Message.EqualTo("Working directory not clean, user does not want to continue. Release process stopped"));
  }

  [Test]
  public void ResetItemsOfIgnoreList_DoesResetItemsOfIgnoreList_ShouldRevertChanges ()
  {
    var fileName = "File.txt";
    for (var index = 0; index < _config.DevelopStableMergeIgnoreList.FileName.Length; index++)
    {
      _config.DevelopStableMergeIgnoreList.FileName[index] = fileName;
    }
    var combinePath = Path.Combine(RepositoryPath, fileName);
    using var fs = File.Create(combinePath);
    fs.Close();
    ExecuteGitCommand($"add {fileName}");
    ExecuteGitCommand("commit -m Commit");

    File.WriteAllText(combinePath, "Temporary Text");
    var gitClient = new CommandLineGitClient();
    var inputReaderMock = new Mock<IInputReader>();

    var rps = new NestedReleaseProcessStepBase(gitClient, _config, inputReaderMock.Object, _console);
    rps.ResetItemsOfIgnoreList(IgnoreListType.DevelopStableMergeIgnoreList);

    Assert.That(File.ReadAllText(combinePath), Is.Empty);
  }

  [Test]
  public void ResetItemsOfIgnoreList_DoesResetItemsOfIgnoreList_ShouldOnlyRevertIgnoreListChanges()
  {
    var fileName = "File.txt";
    var otherFileName = "OtherFile.txt";
    for (var index = 0; index < _config.DevelopStableMergeIgnoreList.FileName.Length; index++)
    {
      _config.DevelopStableMergeIgnoreList.FileName[index] = fileName;
    }
    var combinePath = Path.Combine(RepositoryPath, fileName);
    using var fs = File.Create(combinePath);
    fs.Close();
    var otherCombinePath = Path.Combine(RepositoryPath, otherFileName);
    using var ofs = File.Create(otherCombinePath);
    ofs.Close();
    File.WriteAllText(otherCombinePath, "Permanent Text");
    ExecuteGitCommand($"add {fileName}");
    ExecuteGitCommand($"add {otherFileName}");
    ExecuteGitCommand("commit -m Commit");
    File.WriteAllText(combinePath, "Temporary Text");
    var gitClient = new CommandLineGitClient();
    var inputReaderMock = new Mock<IInputReader>();

    var rps = new NestedReleaseProcessStepBase(gitClient, _config, inputReaderMock.Object, _console);
    rps.ResetItemsOfIgnoreList(IgnoreListType.DevelopStableMergeIgnoreList);

    Assert.That(File.ReadAllText(combinePath), Is.Empty);
    Assert.That(File.ReadAllText(otherCombinePath), Is.EqualTo("Permanent Text"));
  }

  [Test]
  public void ResetItemsOfIgnoreList_WithWrongIgnoreList_DoesNotRevertAnythingHere()
  {
    var fileName = "File.txt";
    var otherFileName = "OtherFile.txt";
    for (var index = 0; index < _config.DevelopStableMergeIgnoreList.FileName.Length; index++)
    {
      _config.DevelopStableMergeIgnoreList.FileName[index] = fileName;
    }

    for (var index = 0; index < _config.TagStableMergeIgnoreList.FileName.Length; index++)
    {
      _config.DevelopStableMergeIgnoreList.FileName[index] = "";
    }
    var combinePath = Path.Combine(RepositoryPath, fileName);
    using var fs = File.Create(combinePath);
    fs.Close();
    var otherCombinePath = Path.Combine(RepositoryPath, otherFileName);
    using var ofs = File.Create(otherCombinePath);
    ofs.Close();
    File.WriteAllText(otherCombinePath, "Permanent Text");
    ExecuteGitCommand($"add {fileName}");
    ExecuteGitCommand($"add {otherFileName}");
    ExecuteGitCommand("commit -m Commit");

    File.WriteAllText(combinePath, "Temporary Text");
    var gitClient = new CommandLineGitClient();
    var inputReaderMock = new Mock<IInputReader>();

    var rps = new NestedReleaseProcessStepBase(gitClient, _config, inputReaderMock.Object, _console);
    rps.ResetItemsOfIgnoreList(IgnoreListType.TagStableMergeIgnoreList);

    Assert.That(File.ReadAllText(combinePath), Is.EqualTo("Temporary Text"));
    Assert.That(File.ReadAllText(otherCombinePath), Is.EqualTo("Permanent Text"));
  }
}