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
using System.IO;
using Moq;
using NUnit.Framework;
using ReleaseProcessScript.New.Configuration;
using ReleaseProcessScript.New.Git;
using ReleaseProcessScript.New.ReadInput;
using ReleaseProcessScript.New.Scripting;
using ReleaseProcessScript.New.SemanticVersioning;
using ReleaseProcessScript.New.Steps.PipelineSteps;
using Spectre.Console;

namespace ReleaseProcessScript.New.Tests.Steps.Releases
{
   [TestFixture]
  internal class ReleaseNonPreReleaseFromDevelopTests
  {
    [SetUp]
    public void setup()
    {
      var path = Path.Join(Environment.CurrentDirectory, c_configFileName);
      _config = new ConfigReader().LoadConfig(path);
      _msBuildInvokerMock = new Mock<IMSBuildCallAndCommit>();
      _continueReleaseOnMasterMock = new Mock<IContinueReleaseOnMasterStep>();
      _continueReleaseOnMasterMock.Setup(_ => _.Execute(It.IsAny<SemanticVersion>(), It.IsAny<bool>())).Verifiable();
      _consoleMock = new Mock<IAnsiConsole>();
    }

    private Mock<IAnsiConsole> _consoleMock;
    private Configuration.Data.Config _config;
    private Mock<IMSBuildCallAndCommit> _msBuildInvokerMock;
    private Mock<IContinueReleaseOnMasterStep> _continueReleaseOnMasterMock;
    private const string c_configFileName = "ReleaseProcessScript.Test.Config";

    [Test]
    public void Execute_WorkingDirectoryNotCleanWithoutConfirmation_ThrowsException()
    {
      var gitClientMock = new Mock<IGitClient>();
      gitClientMock.Setup(_ => _.IsWorkingDirectoryClean()).Returns(false);

      var readInputMock = new Mock<IInputReader>();
      readInputMock.Setup(
              _ => _.ReadConfirmation(true)).Returns(false);
      

      var step = new ReleaseOnMasterStep(
          gitClientMock.Object,
          readInputMock.Object,
          _continueReleaseOnMasterMock.Object,
          _config,
          _msBuildInvokerMock.Object,
          _consoleMock.Object);
      
      Assert.That(() => step.Execute(new SemanticVersion(), "", false, false, false),
          Throws.InstanceOf<Exception>()
              .With.Message.EqualTo("Release processes stopped"));
    }
    
    [Test]
    public void Execute_NoCurrentBranchName_ThrowsException()
    {
      var version = new SemanticVersion();
      var gitClientStub = new Mock<IGitClient>();
      gitClientStub.Setup(_ => _.IsWorkingDirectoryClean()).Returns(true);
      gitClientStub.Setup(_ => _.IsOnBranch("develop")).Returns(true);
      gitClientStub.Setup(_ => _.DoesBranchExist($"release/v{version}")).Returns(true);

      var readInputStub = new Mock<IInputReader>();

      var step = new ReleaseOnMasterStep(
          gitClientStub.Object,
          readInputStub.Object,
          _continueReleaseOnMasterMock.Object,
          _config,
          _msBuildInvokerMock.Object,
          _consoleMock.Object);

      Assert.That(() => step.Execute(version, "commitHash", false, false, false),
          Throws.InstanceOf<Exception>()
              .With.Message.EqualTo("The branch 'release/v0.0.0' already exists."));
    }

    [Test]
    public void Execute_NothingThrows_ShouldCallNextStep()
    {
      var version = new SemanticVersion
                        {
                            Major = 1,
                            Minor = 1,
                            Patch = 1,
                        };
      var gitClientStub = new Mock<IGitClient>();
      gitClientStub.Setup(_ => _.IsWorkingDirectoryClean()).Returns(true);
      gitClientStub.Setup(_ => _.IsOnBranch("develop")).Returns(true);
      gitClientStub.Setup(_ => _.DoesBranchExist($"release/v{version}")).Returns(false);

      var readInputStub = new Mock<IInputReader>();
      readInputStub.Setup(_ => _.ReadVersionChoice(It.IsAny<string>(), It.IsAny<IReadOnlyCollection<SemanticVersion>>())).Returns(version);

      var step = new ReleaseOnMasterStep(
          gitClientStub.Object,
          readInputStub.Object,
          _continueReleaseOnMasterMock.Object,
          _config,
          _msBuildInvokerMock.Object,
          _consoleMock.Object);


      step.Execute(version, "commitHash", false, false, false);


      _continueReleaseOnMasterMock.Verify();
    }

    [Test]
    public void Execute_NothingThrowsButStartReleasePhase_ShouldNotCallNextStep()
    {
      var version = new SemanticVersion
                    {
                        Major = 1,
                        Minor = 1,
                        Patch = 1,
                    };
      var gitClientStub = new Mock<IGitClient>();
      gitClientStub.Setup(_ => _.IsWorkingDirectoryClean()).Returns(true);
      gitClientStub.Setup(_ => _.IsOnBranch("develop")).Returns(true);
      gitClientStub.Setup(_ => _.DoesBranchExist($"release/v{version}")).Returns(false);

      var readInputStub = new Mock<IInputReader>();
      readInputStub.Setup(_ => _.ReadVersionChoice(It.IsAny<string>(), It.IsAny<IReadOnlyCollection<SemanticVersion>>())).Returns(version);

      var step = new ReleaseOnMasterStep(
          gitClientStub.Object,
          readInputStub.Object,
          _continueReleaseOnMasterMock.Object,
          _config,
          _msBuildInvokerMock.Object,
          _consoleMock.Object);


      step.Execute(version, "commitHash", true, false, false);
      

      _continueReleaseOnMasterMock.Verify(_=> _.Execute(It.IsAny<SemanticVersion>(), It.IsAny<bool>()),Times.Never);

    }

    [Test]
    public void Execute_NothingThrowsButPauseForCommit_ShouldNotCallNextStep()
    {
      var version = new SemanticVersion
                    {
                        Major = 1,
                        Minor = 1,
                        Patch = 1,
                    };
      var gitClientStub = new Mock<IGitClient>();
      gitClientStub.Setup(_ => _.IsWorkingDirectoryClean()).Returns(true);
      gitClientStub.Setup(_ => _.IsOnBranch("develop")).Returns(true);
      gitClientStub.Setup(_ => _.DoesBranchExist($"release/v{version}")).Returns(false);

      var readInputStub = new Mock<IInputReader>();
      readInputStub.Setup(_ => _.ReadVersionChoice(It.IsAny<string>(), It.IsAny<IReadOnlyCollection<SemanticVersion>>())).Returns(version);

      var step = new ReleaseOnMasterStep(
          gitClientStub.Object,
          readInputStub.Object,
          _continueReleaseOnMasterMock.Object,
          _config,
          _msBuildInvokerMock.Object,
          _consoleMock.Object);


      step.Execute(version, "commitHash", false, true, false);

      _continueReleaseOnMasterMock.Verify(_ => _.Execute(It.IsAny<SemanticVersion>(), It.IsAny<bool>()), Times.Never);
    }

    
  }
}
