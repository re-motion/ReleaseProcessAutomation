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
using ReleaseProcessAutomation.Extensions;
using ReleaseProcessAutomation.Git;
using ReleaseProcessAutomation.ReadInput;
using ReleaseProcessAutomation.Scripting;
using ReleaseProcessAutomation.SemanticVersioning;
using ReleaseProcessAutomation.Steps.PipelineSteps;
using Spectre.Console;

namespace ReleaseProcessAutomation.Tests.Steps.Releases
{
  [TestFixture]
  internal class ReleasePatchStepTests
  {
    [SetUp]
    public void Setup()
    {
      _gitClientStub = new Mock<IGitClient>();
      _inputReaderStub = new Mock<IInputReader>();
      _msBuildInvokerMock = new Mock<IMSBuildCallAndCommit>();
      _contineReleasePatchMock = new Mock<IContinueReleasePatchStep>();
      _consoleStub = new Mock<IAnsiConsole>();


      var path = Path.Join(Environment.CurrentDirectory, c_configFileName);
      _config = new ConfigReader().LoadConfig(path);

    }

    private Mock<IAnsiConsole> _consoleStub;
    private Mock<IGitClient> _gitClientStub;
    private Mock<IInputReader> _inputReaderStub;
    private Configuration.Data.Config _config;
    private Mock<IMSBuildCallAndCommit> _msBuildInvokerMock;
    private Mock<IContinueReleasePatchStep> _contineReleasePatchMock;
    private const string c_configFileName = "ReleaseProcessScript.Test.Config";

    [Test]
    public void Execute_OnMasterWithoutErrors_CallsNextStep()
    {
      _gitClientStub.Setup(_ => _.IsWorkingDirectoryClean()).Returns(true);
      _gitClientStub.Setup(_ => _.IsCommitHash("")).Returns(true);
      _gitClientStub.Setup(_ => _.IsOnBranch("master")).Returns(true);
      _gitClientStub.Setup(_ => _.GetCurrentBranchName()).Returns("release/v0.0.0");
      var nextPossibleVersions = new SemanticVersion().GetNextPossibleVersionsHotfix();
      _inputReaderStub.Setup(_ => _.ReadVersionChoice(It.IsAny<string>(), nextPossibleVersions)).Returns(new SemanticVersion());
      _contineReleasePatchMock.Setup(_ => _.Execute(new SemanticVersion(), false, true)).Verifiable();

      var patchStep = new ReleasePatchStep(
          _gitClientStub.Object,
          _config,
          _inputReaderStub.Object,
          _msBuildInvokerMock.Object,
          _contineReleasePatchMock.Object,
          _consoleStub.Object);



      Assert.That(
          () => patchStep.Execute(new SemanticVersion(), "" , false, false, false, true),
          Throws.Nothing);
      _contineReleasePatchMock.Verify();
    }

    [Test]
    public void Execute_OnHotfixWithoutErrors_CallsNextStep()
    {
      _gitClientStub.Setup(_ => _.IsWorkingDirectoryClean()).Returns(true);
      _gitClientStub.Setup(_ => _.IsCommitHash("")).Returns(true);
      _gitClientStub.Setup(_ => _.IsOnBranch("hotfix/")).Returns(true);
      _gitClientStub.Setup(_ => _.GetCurrentBranchName()).Returns("release/v0.0.0");
      var nextPossibleVersions = new SemanticVersion().GetNextPossibleVersionsHotfix();
      _inputReaderStub.Setup(_ => _.ReadVersionChoice(It.IsAny<string>(), nextPossibleVersions)).Returns(new SemanticVersion());
      _contineReleasePatchMock.Setup(_ => _.Execute(new SemanticVersion(), false, false)).Verifiable();

      var patchStep = new ReleasePatchStep(
          _gitClientStub.Object,
          _config,
          _inputReaderStub.Object,
          _msBuildInvokerMock.Object,
          _contineReleasePatchMock.Object,
          _consoleStub.Object);



      Assert.That(
          () => patchStep.Execute(new SemanticVersion(), "", false, false, false, false),
          Throws.Nothing);
      _contineReleasePatchMock.Verify();
    }

    [Test]
    public void Execute_OnHotfixWithoutErrorsWithStartReleasePhase_DoesNotCallNextStepAndInvokeAndCommit()
    {
      _gitClientStub.Setup(_ => _.IsWorkingDirectoryClean()).Returns(true);
      _gitClientStub.Setup(_ => _.IsCommitHash("")).Returns(true);
      _gitClientStub.Setup(_ => _.IsOnBranch("hotfix/")).Returns(true);
      _gitClientStub.Setup(_ => _.GetCurrentBranchName()).Returns("release/v0.0.0");
      var nextPossibleVersions = new SemanticVersion().GetNextPossibleVersionsHotfix();
      _inputReaderStub.Setup(_ => _.ReadVersionChoice(It.IsAny<string>(), nextPossibleVersions)).Returns(new SemanticVersion());
      

      var patchStep = new ReleasePatchStep(
          _gitClientStub.Object,
          _config,
          _inputReaderStub.Object,
          _msBuildInvokerMock.Object,
          _contineReleasePatchMock.Object,
          _consoleStub.Object);



      Assert.That(
          () => patchStep.Execute(new SemanticVersion(), "", true, false, false, false),
          Throws.Nothing);
      _msBuildInvokerMock.Verify(_ => _.CallMSBuildStepsAndCommit(It.IsAny<MSBuildMode>(), It.IsAny<SemanticVersion>()), Times.Never);
      _contineReleasePatchMock.Verify(_=>_.Execute(It.IsAny<SemanticVersion>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
    }
    [Test]
    public void Execute_OnHotfixWithoutErrorsWithPauseForCommit_DoesNotCallNextStepButDoesCallInvokeBuildAndCommit()
    {
      _gitClientStub.Setup(_ => _.IsWorkingDirectoryClean()).Returns(true);
      _gitClientStub.Setup(_ => _.IsCommitHash("")).Returns(true);
      _gitClientStub.Setup(_ => _.IsOnBranch("hotfix/")).Returns(true);
      _gitClientStub.Setup(_ => _.GetCurrentBranchName()).Returns("release/v0.0.0");
      var nextPossibleVersions = new SemanticVersion().GetNextPossibleVersionsHotfix();
      _inputReaderStub.Setup(_ => _.ReadVersionChoice(It.IsAny<string>(), nextPossibleVersions)).Returns(new SemanticVersion());
      _msBuildInvokerMock.Setup(_ => _.CallMSBuildStepsAndCommit(It.IsAny<MSBuildMode>(), It.IsAny<SemanticVersion>())).Verifiable();

      var patchStep = new ReleasePatchStep(
          _gitClientStub.Object,
          _config,
          _inputReaderStub.Object,
          _msBuildInvokerMock.Object,
          _contineReleasePatchMock.Object,
          _consoleStub.Object);



      Assert.That(
          () => patchStep.Execute(new SemanticVersion(), "", false, true, false, false),
          Throws.Nothing);
      _msBuildInvokerMock.Verify();
      _contineReleasePatchMock.Verify(_ => _.Execute(It.IsAny<SemanticVersion>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
    }
  }
}
