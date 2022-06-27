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
using Spectre.Console.Testing;

namespace ReleaseProcessAutomation.Tests.Steps.Releases
{
  [TestFixture]
  internal class ReleaseRcStepTests
  {
    [SetUp]
    public void Setup ()
    {
      _gitClientStub = new Mock<IGitClient>();
      _inputReaderMock = new Mock<IInputReader>();
      _ancestorStub = new Mock<IAncestorFinder>();
      _msBuildInvokerMock = new Mock<IMSBuildCallAndCommit>();
      _continueAlphaBetaMock = new Mock<IContinueAlphaBetaStep>();

      _consoleStub = new Mock<IAnsiConsole>();

      var path = Path.Join(TestContext.CurrentContext.TestDirectory, c_configFileName);
      _config = new ConfigReader().LoadConfig(path);

    }

    private Mock<IAnsiConsole> _consoleStub;
    private Mock<IGitClient> _gitClientStub;
    private Mock<IInputReader> _inputReaderMock;
    private Configuration.Data.Config _config;
    private Mock<IAncestorFinder> _ancestorStub;
    private Mock<IMSBuildCallAndCommit> _msBuildInvokerMock;
    private Mock<IContinueAlphaBetaStep> _continueAlphaBetaMock;
    private const string c_configFileName = "ReleaseProcessScript.Test.Config";

    [Test]
    public void Execute_callsNextVersionFromDevelop_WithProperVersionCollection ()
    {
      _gitClientStub.Setup(_ => _.IsWorkingDirectoryClean()).Returns(true);
      _gitClientStub.Setup(_ => _.IsCommitHash("")).Returns(true);
      _gitClientStub.Setup(_ => _.IsOnBranch("release/")).Returns(true);
      _gitClientStub.Setup(_ => _.GetCurrentBranchName()).Returns("branchName");
      _gitClientStub.Setup(_ => _.DoesBranchExist("prerelease/v0.0.0")).Returns(false);
      var nextPossibleVersions = new SemanticVersion().GetNextPossibleVersionsDevelop();

      _inputReaderMock.Setup(_ => _.ReadVersionChoice(It.IsAny<string>(), nextPossibleVersions)).Returns(new SemanticVersion()).Verifiable();

      var rcStep = new ReleaseRCStep(
          _gitClientStub.Object,
          _config,
          _inputReaderMock.Object,
          _ancestorStub.Object,
          _msBuildInvokerMock.Object,
          _continueAlphaBetaMock.Object,
          _consoleStub.Object);

      Assert.That(
          () => rcStep.Execute(new SemanticVersion(), "", false, false, "develop"),
          Throws.Nothing);
      _inputReaderMock.Verify();

    }

    [Test]
    public void Execute_callsNextVersionFromRelease_WithProperVersionCollection ()
    {
      _gitClientStub.Setup(_ => _.IsWorkingDirectoryClean()).Returns(true);
      _gitClientStub.Setup(_ => _.IsCommitHash("")).Returns(true);
      _gitClientStub.Setup(_ => _.IsOnBranch("release/")).Returns(true);
      _gitClientStub.Setup(_ => _.GetCurrentBranchName()).Returns("branchName");
      _gitClientStub.Setup(_ => _.DoesBranchExist("prerelease/v0.0.0")).Returns(false);
      var nextPossibleVersions = new SemanticVersion().GetNextPossibleVersionsDevelop();

      _inputReaderMock.Setup(_ => _.ReadVersionChoice(It.IsAny<string>(), nextPossibleVersions)).Returns(new SemanticVersion()).Verifiable();

      var rcStep = new ReleaseRCStep(
          _gitClientStub.Object,
          _config,
          _inputReaderMock.Object,
          _ancestorStub.Object,
          _msBuildInvokerMock.Object,
          _continueAlphaBetaMock.Object,
          _consoleStub.Object);

      Assert.That(
          () => rcStep.Execute(new SemanticVersion(), "", false, false, "release/v1.3.5"),
          Throws.Nothing);
      _inputReaderMock.Verify();

    }

    [Test]
    public void Execute_callsNextVersioFromHotfix_WithProperVersionCollection ()
    {
      _gitClientStub.Setup(_ => _.IsWorkingDirectoryClean()).Returns(true);
      _gitClientStub.Setup(_ => _.IsCommitHash("")).Returns(true);
      _gitClientStub.Setup(_ => _.IsOnBranch("release/")).Returns(true);
      _gitClientStub.Setup(_ => _.GetCurrentBranchName()).Returns("branchName");
      _gitClientStub.Setup(_ => _.DoesBranchExist("prerelease/v0.0.0")).Returns(false);
      var nextPossibleVersions = new SemanticVersion().GetNextPossibleVersionsHotfix();

      _inputReaderMock.Setup(_ => _.ReadVersionChoice(It.IsAny<string>(), nextPossibleVersions)).Returns(new SemanticVersion()).Verifiable();

      var rcStep = new ReleaseRCStep(
          _gitClientStub.Object,
          _config,
          _inputReaderMock.Object,
          _ancestorStub.Object,
          _msBuildInvokerMock.Object,
          _continueAlphaBetaMock.Object,
          _consoleStub.Object);

      Assert.That(
          () => rcStep.Execute(new SemanticVersion(), "", false, false, "hotfix/v1.3.5"),
          Throws.Nothing);
      _inputReaderMock.Verify();

    }

    [Test]
    public void Execute_WithPauseForCommit_DoesNotCallNextStep ()
    {
      _gitClientStub.Setup(_ => _.IsWorkingDirectoryClean()).Returns(true);
      _gitClientStub.Setup(_ => _.IsCommitHash("")).Returns(true);
      _gitClientStub.Setup(_ => _.IsOnBranch("release/")).Returns(true);
      _gitClientStub.Setup(_ => _.GetCurrentBranchName()).Returns("branchName");
      _gitClientStub.Setup(_ => _.DoesBranchExist("prerelease/v0.0.0")).Returns(false);
      var nextPossibleVersions = new SemanticVersion().GetNextPossibleVersionsHotfix();
      _continueAlphaBetaMock.Setup(_ => _.Execute(It.IsAny<SemanticVersion>(), It.IsAny<string>(), It.IsAny<string>(), false)).Verifiable();

      _inputReaderMock.Setup(_ => _.ReadVersionChoice(It.IsAny<string>(), nextPossibleVersions)).Returns(new SemanticVersion());

      var rcStep = new ReleaseRCStep(
          _gitClientStub.Object,
          _config,
          _inputReaderMock.Object,
          _ancestorStub.Object,
          _msBuildInvokerMock.Object,
          _continueAlphaBetaMock.Object,
          _consoleStub.Object);

      Assert.That(
          () => rcStep.Execute(new SemanticVersion(), "", true, false, "hotfix/v1.3.5"),
          Throws.Nothing);

      _continueAlphaBetaMock.Verify(_ => _.Execute(It.IsAny<SemanticVersion>(), It.IsAny<string>(), It.IsAny<string>(), false), Times.Never);
    }


    [Test]
    public void Execute_WithoutErrors_CallsNextStep ()
    {
      _gitClientStub.Setup(_ => _.IsWorkingDirectoryClean()).Returns(true);
      _gitClientStub.Setup(_ => _.IsCommitHash("")).Returns(true);
      _gitClientStub.Setup(_ => _.IsOnBranch("release/")).Returns(true);
      _gitClientStub.Setup(_ => _.GetCurrentBranchName()).Returns("branchName");
      _gitClientStub.Setup(_ => _.DoesBranchExist("prerelease/v0.0.0")).Returns(false);
      var nextPossibleVersions = new SemanticVersion().GetNextPossibleVersionsHotfix();
      _continueAlphaBetaMock.Setup(_ => _.Execute(It.IsAny<SemanticVersion>(), It.IsAny<string>(), It.IsAny<string>(), false)).Verifiable();

      _inputReaderMock.Setup(_ => _.ReadVersionChoice(It.IsAny<string>(), nextPossibleVersions)).Returns(new SemanticVersion());

      var rcStep = new ReleaseRCStep(
          _gitClientStub.Object,
          _config,
          _inputReaderMock.Object,
          _ancestorStub.Object,
          _msBuildInvokerMock.Object,
          _continueAlphaBetaMock.Object,
          _consoleStub.Object);

      Assert.That(
          () => rcStep.Execute(new SemanticVersion(), "", false, false, "hotfix/v1.3.5"),
          Throws.Nothing);
      _continueAlphaBetaMock.Verify();
    }

    [Test]
    public void Execute_WithoutErrors_CallsMBuildInvokeAndCommit()
    {
      _gitClientStub.Setup(_ => _.IsWorkingDirectoryClean()).Returns(true);
      _gitClientStub.Setup(_ => _.IsCommitHash("")).Returns(true);
      _gitClientStub.Setup(_ => _.IsOnBranch("release/")).Returns(true);
      _gitClientStub.Setup(_ => _.GetCurrentBranchName()).Returns("branchName");
      _gitClientStub.Setup(_ => _.DoesBranchExist("prerelease/v0.0.0")).Returns(false);
      var nextPossibleVersions = new SemanticVersion().GetNextPossibleVersionsHotfix();
      _msBuildInvokerMock.Setup(_ => _.CallMSBuildStepsAndCommit(MSBuildMode.PrepareNextVersion, new SemanticVersion())).Verifiable();
      _inputReaderMock.Setup(_ => _.ReadVersionChoice(It.IsAny<string>(), nextPossibleVersions)).Returns(new SemanticVersion());

      var rcStep = new ReleaseRCStep(
          _gitClientStub.Object,
          _config,
          _inputReaderMock.Object,
          _ancestorStub.Object,
          _msBuildInvokerMock.Object,
          _continueAlphaBetaMock.Object,
          _consoleStub.Object);

      Assert.That(
          () => rcStep.Execute(new SemanticVersion(), "", false, false, "hotfix/v1.3.5"),
          Throws.Nothing);
      _msBuildInvokerMock.Verify();
    }
  }


}
