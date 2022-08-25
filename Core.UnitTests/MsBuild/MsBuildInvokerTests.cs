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
using ReleaseProcessAutomation.MSBuild;
using ReleaseProcessAutomation.Scripting;
using ReleaseProcessAutomation.SemanticVersioning;
using Spectre.Console;
using Spectre.Console.Testing;

namespace ReleaseProcessAutomation.UnitTests.MsBuild;

[TestFixture]
internal class MSBuildInvokerTests
{
  private const string c_configFileName = "ReleaseProcessScript.Test.Config";

  private Mock<IAnsiConsole> _consoleStub;
  private Configuration.Data.Config _config;
  private TestConsole testConsole;

  [SetUp]
  public void Setup ()
  {
    var path = Path.Join(TestContext.CurrentContext.TestDirectory, c_configFileName);
    _config = new ConfigReader().LoadConfig(path);
    _consoleStub = new Mock<IAnsiConsole>();
    testConsole = new TestConsole();
    testConsole.Width(int.MaxValue);
  }

  [Test]
  public void InvokeMSBuildAndCommit_NoMSBuildPathSpecified_EndsEarlyWithErrors ()
  {
    var version = new SemanticVersion
                  {
                      Major = 1,
                      Minor = 1,
                      Patch = 1
                  };

    _config.MSBuildSettings.MSBuildPath = "";
    var gitClientStub = new Mock<IGitClient>();
    var msBuildMock = new Mock<IMSBuild>();
    msBuildMock.Setup(_ => _.CallMSBuild("", It.IsAny<string>())).Verifiable();
    
    var msBuildInvoker = new MSBuildCallAndCommit(
        gitClientStub.Object,
        _config,
        msBuildMock.Object,
        testConsole);

    var act = msBuildInvoker.CallMSBuildStepsAndCommit(MSBuildMode.PrepareNextVersion, version);

    Assert.That(act, Is.EqualTo(-1));
    Assert.That(testConsole.Output, Does.Contain("There was no MSBuild path specified in the config, will continue without invoking MSBuild."));
    msBuildMock.Verify(n => n.CallMSBuild("", It.IsAny<string>()), Times.Never);
  }

  [Test]
  public void InvokeMSBuildAndCommit_CorrectMSBuildPathSpecified_FinishesWithoutErrors ()
  {
    var version = new SemanticVersion
                  {
                      Major = 1,
                      Minor = 1,
                      Patch = 1,
                  };

    _config.MSBuildSettings.MSBuildPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "msbuild.exe");
    File.Delete(_config.MSBuildSettings.MSBuildPath);
    var writer = File.Create(_config.MSBuildSettings.MSBuildPath);
    writer.Close();
    var gitClientStub = new Mock<IGitClient>();
    gitClientStub.Setup(_ => _.IsWorkingDirectoryClean()).Returns(true);
    var msBuildMock = new Mock<IMSBuild>();

    var testConsole = new TestConsole();

    var msBuildInvoker = new MSBuildCallAndCommit(
        gitClientStub.Object,
        _config,
        msBuildMock.Object,
        testConsole);

    var act = msBuildInvoker.CallMSBuildStepsAndCommit(MSBuildMode.PrepareNextVersion, version);

    Assert.That(act, Is.EqualTo(0));

    msBuildMock.Verify(n => n.CallMSBuild(It.IsAny<string>(), It.IsAny<string>()));
    File.Delete(_config.MSBuildSettings.MSBuildPath);
  }

  [Test]
  public void InvokeMSBuildAndCommit_WrongMSBuildPathSpecified_EndsEarlyWithSpecificMessage ()
  {
    var version = new SemanticVersion
                  {
                      Major = 1,
                      Minor = 1,
                      Patch = 1,
                  };

    _config.MSBuildSettings.MSBuildPath = "D:\\DefinitelyNotAFolder\\DefinitelyNotAnMSBuildPath.definitelyNotAnExe";
    var gitClientStub = new Mock<IGitClient>();
    var msBuildMock = new Mock<IMSBuild>();
    msBuildMock.Setup(_ => _.CallMSBuild("", It.IsAny<string>())).Verifiable();

    var msBuildInvoker = new MSBuildCallAndCommit(
        gitClientStub.Object,
        _config,
        msBuildMock.Object,
        testConsole);

    var act = msBuildInvoker.CallMSBuildStepsAndCommit(MSBuildMode.PrepareNextVersion, version);

    Assert.That(act, Is.EqualTo(-1));
    Assert.That(testConsole.Output, Does.Contain($"The configured MSBuild path '{_config.MSBuildSettings.MSBuildPath}' does not exist.\nPlease configure a proper MSBuild path in the config.\nWill continue without invoking MSBuild.\n"));
    msBuildMock.Verify(n => n.CallMSBuild("", It.IsAny<string>()), Times.Never);
  }

  [Test]
  public void InvokeMSBuildAndCommit_WorkingDirectoryNotCleanAfterCallToMSBuildWithOutCommitMessage_ThrowsException ()
  {
    _config.PrepareNextVersionMSBuildSteps.Steps[0].CommitMessage = string.Empty;
    var version = new SemanticVersion
                  {
                      Major = 1,
                      Minor = 1,
                      Patch = 1
                  };

    var gitClientStub = new Mock<IGitClient>();
    gitClientStub.Setup(_ => _.IsWorkingDirectoryClean()).Returns(false);
    var msBuildStub = new Mock<IMSBuild>();

    var msBuildInvoker = new MSBuildCallAndCommit(
        gitClientStub.Object,
        _config,
        msBuildStub.Object,
        _consoleStub.Object);

    Assert.That(() => msBuildInvoker.CallMSBuildStepsAndCommit(MSBuildMode.PrepareNextVersion, version),
            Throws.InstanceOf<InvalidOperationException>()
                    .With.Message.EqualTo("Working directory not clean after call to MSBuild.exe without commit message. Check your targets in the config and make sure they do not create new files."));
  }

  [Test]
  public void InvokeMSBuildAndCommit_WorkingDirectoryNotCleanBeforeCallToMSBuildWithCommitMessage_ThrowsException ()
  {
    _config.PrepareNextVersionMSBuildSteps.Steps[0].CommitMessage = "CommitAll";
    var version = new SemanticVersion
                  {
                      Major = 1,
                      Minor = 1,
                      Patch = 1
                  };
    var gitClientStub = new Mock<IGitClient>();
    gitClientStub.Setup(_ => _.IsWorkingDirectoryClean()).Returns(false);
    var msBuildStub = new Mock<IMSBuild>();

    var msBuildInvoker = new MSBuildCallAndCommit(
        gitClientStub.Object,
        _config,
        msBuildStub.Object,
        _consoleStub.Object);

    Assert.That(
        () => msBuildInvoker.CallMSBuildStepsAndCommit(MSBuildMode.PrepareNextVersion, version),
        Throws.InstanceOf<InvalidOperationException>()
            .With.Message.EqualTo("Working directory not clean before a call to MSBuild.exe with a commit message defined in config."));
  }
}