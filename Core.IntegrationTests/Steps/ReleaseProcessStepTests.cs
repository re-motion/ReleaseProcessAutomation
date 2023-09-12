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
using Remotion.ReleaseProcessAutomation.Configuration;
using Remotion.ReleaseProcessAutomation.Configuration.Data;
using Remotion.ReleaseProcessAutomation.Git;
using Remotion.ReleaseProcessAutomation.ReadInput;
using Remotion.ReleaseProcessAutomation.Steps;
using Spectre.Console;
using Spectre.Console.Testing;

namespace Remotion.ReleaseProcessAutomation.IntegrationTests.Steps;

[TestFixture]
internal class ReleaseProcessStepTests : GitBackedTestBase
{
  private Config _config;
  private const string c_configFileName = "ReleaseProcessScript.Test.Config";

  private IAnsiConsole _console;

  private class NestedReleaseProcessStepBase : ReleaseProcessStepBase
  {
    public NestedReleaseProcessStepBase (
        IGitClient gitClient,
        Config config,
        IInputReader inputReader,
        IAnsiConsole console)
        : base(gitClient, config, inputReader, console)
    {
    }

    public new void EnsureWorkingDirectoryClean ()
    {
      base.EnsureWorkingDirectoryClean();
    }

    public new void ResetItemsForMerge (string intoBranchName, string mergeBranchName, IgnoreListType ignoreListType)
    {
      base.ResetItemsForMerge(intoBranchName, mergeBranchName, ignoreListType);
    }
  }

  [SetUp]
  public void Setup ()
  {
    var path = Path.Join(PreviousWorkingDirectory, c_configFileName);
    _config = new ConfigReader().LoadConfig(path);
    _console = new TestConsole();
  }


  [Test]
  public void EnsureWorkingDirectoryClean_CleanDir_DoesNotThrow ()
  {
    var gitClientStub = new Mock<IGitClient>();
    gitClientStub.Setup(_ => _.IsWorkingDirectoryClean()).Returns(true);

    var readerMock = new Mock<IInputReader>();
    var rps = new NestedReleaseProcessStepBase(gitClientStub.Object, _config, readerMock.Object, _console);

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
  public void EnsureWorkingDirectoryClean_UncleanDirectoryNotIgnored_DoesThrow ()
  {
    var gitClientStub = new Mock<IGitClient>();
    gitClientStub.Setup(_ => _.IsWorkingDirectoryClean()).Returns(false);
    var readInputStub = new Mock<IInputReader>();
    readInputStub.Setup(_ => _.ReadConfirmation(true)).Returns(false);

    var rps = new NestedReleaseProcessStepBase(gitClientStub.Object, _config, readInputStub.Object, _console);

    Assert.That(
        () => rps.EnsureWorkingDirectoryClean(),
        Throws.InstanceOf<Exception>()
            .With.Message.EqualTo("Working directory not clean, user does not want to continue. Release process stopped."));
  }

  [Test]
  public void ResetItemsForMerge_DoesRevertChanges ()
  {
    var fileName = "File.txt";
    _config.DevelopStableMergeIgnoreList.FileName = new[] { fileName };

    var combinePath = Path.Combine(RepositoryPath, fileName);
    using var fs = File.Create(combinePath);
    fs.Close();

    ExecuteGitCommand("checkout -b develop");
    ExecuteGitCommand($"add {fileName}");
    ExecuteGitCommand("commit -m Commit");
    ExecuteGitCommand("checkout -b prerelease/v1.0.0-alpha.1");

    File.WriteAllText(combinePath, "Temporary Text");
    var gitClient = new CommandLineGitClient();
    var inputReaderMock = new Mock<IInputReader>();

    var rps = new NestedReleaseProcessStepBase(gitClient, _config, inputReaderMock.Object, _console);
    rps.ResetItemsForMerge("develop", "prerelease/v1.0.0-alpha.1", IgnoreListType.DevelopStableMergeIgnoreList);

    Assert.That(File.ReadAllText(combinePath), Is.Empty);
  }

  [Test]
  public void ResetItemsForMerge_WithFilesNotInIgnoreList_DoesOnlyRevertIgnoreListChanges ()
  {
    var fileName = "File.txt";
    var otherFileName = "OtherFile.txt";
    _config.DevelopStableMergeIgnoreList.FileName = new[] { fileName };

    var combinePath = Path.Combine(RepositoryPath, fileName);
    using var fs = File.Create(combinePath);
    fs.Close();

    var otherCombinePath = Path.Combine(RepositoryPath, otherFileName);
    using var ofs = File.Create(otherCombinePath);
    ofs.Close();

    File.WriteAllText(otherCombinePath, "Permanent Text");
    ExecuteGitCommand("checkout -b develop");
    ExecuteGitCommand($"add {fileName}");
    ExecuteGitCommand($"add {otherFileName}");
    ExecuteGitCommand("commit -m Commit");
    ExecuteGitCommand("checkout -b prerelease/v1.0.0-alpha.1");

    File.WriteAllText(combinePath, "Temporary Text");
    var gitClient = new CommandLineGitClient();
    var inputReaderMock = new Mock<IInputReader>();

    var rps = new NestedReleaseProcessStepBase(gitClient, _config, inputReaderMock.Object, _console);
    rps.ResetItemsForMerge("develop", "prerelease/v1.0.0-alpha.1", IgnoreListType.DevelopStableMergeIgnoreList);

    Assert.That(File.ReadAllText(combinePath), Is.Empty);
    Assert.That(File.ReadAllText(otherCombinePath), Is.EqualTo("Permanent Text"));
  }

  [Test]
  public void ResetItemsForMerge_WithWrongIgnoreList_DoesNotRevertAnything ()
  {
    var fileName = "File.txt";
    var otherFileName = "OtherFile.txt";

    _config.DevelopStableMergeIgnoreList.FileName = new[] { fileName };
    _config.TagStableMergeIgnoreList.FileName = new[] { "" };

    var combinePath = Path.Combine(RepositoryPath, fileName);
    using var fs = File.Create(combinePath);
    fs.Close();

    var otherCombinePath = Path.Combine(RepositoryPath, otherFileName);
    using var ofs = File.Create(otherCombinePath);
    ofs.Close();

    File.WriteAllText(otherCombinePath, "Permanent Text");
    ExecuteGitCommand("checkout -b develop");
    ExecuteGitCommand($"add {fileName}");
    ExecuteGitCommand($"add {otherFileName}");
    ExecuteGitCommand("commit -m Commit");
    ExecuteGitCommand("checkout -b prerelease/v1.0.0-alpha.1");

    File.WriteAllText(combinePath, "Temporary Text");
    var gitClient = new CommandLineGitClient();
    var inputReaderMock = new Mock<IInputReader>();

    var rps = new NestedReleaseProcessStepBase(gitClient, _config, inputReaderMock.Object, _console);
    rps.ResetItemsForMerge("develop", "prerelease/v1.0.0-alpha.1", IgnoreListType.TagStableMergeIgnoreList);

    Assert.That(File.ReadAllText(combinePath), Is.EqualTo("Temporary Text"));
    Assert.That(File.ReadAllText(otherCombinePath), Is.EqualTo("Permanent Text"));
  }
}