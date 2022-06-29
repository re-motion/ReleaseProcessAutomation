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
using ReleaseProcessAutomation.Jira;
using ReleaseProcessAutomation.ReadInput;
using ReleaseProcessAutomation.Scripting;
using ReleaseProcessAutomation.SemanticVersioning;
using ReleaseProcessAutomation.Steps.PipelineSteps;
using ReleaseProcessAutomation.Steps.SubSteps;
using Spectre.Console;

namespace ReleaseProcessAutomation.UnitTests.Steps.Releases;

[TestFixture]
internal class ReleaseRcStepTests
{
  private const string c_configFileName = "ReleaseProcessScript.Test.Config";

  private Mock<IAnsiConsole> _consoleStub;
  private Mock<IGitClient> _gitClientStub;
  private Mock<IInputReader> _inputReaderMock;
  private Configuration.Data.Config _config;
  private Mock<IAncestorFinder> _ancestorStub;
  private Mock<IMSBuildCallAndCommit> _msBuildInvokerMock;
  private Mock<IReleaseVersionAndMoveIssuesSubStep> _releaseVersionAndMoveIssueMock;
  private Mock<IContinueAlphaBetaStep> _continueAlphaBetaMock;

  [SetUp]
  public void Setup ()
  {
    _gitClientStub = new Mock<IGitClient>();
    _inputReaderMock = new Mock<IInputReader>();
    _ancestorStub = new Mock<IAncestorFinder>();
    _msBuildInvokerMock = new Mock<IMSBuildCallAndCommit>();
    _continueAlphaBetaMock = new Mock<IContinueAlphaBetaStep>();
    _releaseVersionAndMoveIssueMock = new Mock<IReleaseVersionAndMoveIssuesSubStep>();

    _consoleStub = new Mock<IAnsiConsole>();

    var path = Path.Join(TestContext.CurrentContext.TestDirectory, c_configFileName);
    _config = new ConfigReader().LoadConfig(path);
  }

  [Test]
  public void Execute_callsNextVersionFromDevelop_WithProperVersionCollection ()
  {
    var nextVersion = new SemanticVersion { Major = 1 };
    var nextJiraVersion = new SemanticVersion();
    _gitClientStub.Setup(_ => _.IsWorkingDirectoryClean()).Returns(true);
    _gitClientStub.Setup(_ => _.IsCommitHash("")).Returns(true);
    _gitClientStub.Setup(_ => _.IsOnBranch("release/")).Returns(true);
    _gitClientStub.Setup(_ => _.GetCurrentBranchName()).Returns("branchName");
    _gitClientStub.Setup(_ => _.DoesBranchExist("prerelease/v0.0.0")).Returns(false);
    var nextPossibleVersions = nextVersion.GetNextPossibleVersionsDevelop();

    _inputReaderMock.Setup(_ => _.ReadVersionChoice(It.IsAny<string>(), nextPossibleVersions)).Returns(nextJiraVersion).Verifiable();

    var rcStep = new ReleaseRCStep(
        _gitClientStub.Object,
        _config,
        _inputReaderMock.Object,
        _ancestorStub.Object,
        _msBuildInvokerMock.Object,
        _continueAlphaBetaMock.Object,
        _consoleStub.Object,
        _releaseVersionAndMoveIssueMock.Object);

    Assert.That(
        () => rcStep.Execute(nextVersion, "", false, false, "develop"),
        Throws.Nothing);
    _releaseVersionAndMoveIssueMock.Verify(_ => _.Execute(nextVersion, nextJiraVersion, false, false), Times.Exactly(1));
    _inputReaderMock.Verify();
  }

  [Test]
  public void Execute_callsNextVersionFromRelease_WithProperVersionCollection ()
  {
    var nextVersion = new SemanticVersion { Major = 1 };
    var nextJiraVersion = new SemanticVersion();
    _gitClientStub.Setup(_ => _.IsWorkingDirectoryClean()).Returns(true);
    _gitClientStub.Setup(_ => _.IsCommitHash("")).Returns(true);
    _gitClientStub.Setup(_ => _.IsOnBranch("release/")).Returns(true);
    _gitClientStub.Setup(_ => _.GetCurrentBranchName()).Returns("branchName");
    _gitClientStub.Setup(_ => _.DoesBranchExist("prerelease/v0.0.0")).Returns(false);
    var nextPossibleVersions = nextVersion.GetNextPossibleVersionsDevelop();

    _inputReaderMock.Setup(_ => _.ReadVersionChoice(It.IsAny<string>(), nextPossibleVersions)).Returns(nextJiraVersion).Verifiable();

    var rcStep = new ReleaseRCStep(
        _gitClientStub.Object,
        _config,
        _inputReaderMock.Object,
        _ancestorStub.Object,
        _msBuildInvokerMock.Object,
        _continueAlphaBetaMock.Object,
        _consoleStub.Object,
        _releaseVersionAndMoveIssueMock.Object);

    Assert.That(
        () => rcStep.Execute(nextVersion, "", false, false, "release/v1.3.5"),
        Throws.Nothing);
    _releaseVersionAndMoveIssueMock.Verify(_ => _.Execute(nextVersion, nextJiraVersion, false, false), Times.Exactly(1));
    _inputReaderMock.Verify();
  }

  [Test]
  public void Execute_callsNextVersioFromHotfix_WithProperVersionCollection ()
  {
    var nextVersion = new SemanticVersion { Major = 1 };
    var nextJiraVersion = new SemanticVersion();
    _gitClientStub.Setup(_ => _.IsWorkingDirectoryClean()).Returns(true);
    _gitClientStub.Setup(_ => _.IsCommitHash("")).Returns(true);
    _gitClientStub.Setup(_ => _.IsOnBranch("release/")).Returns(true);
    _gitClientStub.Setup(_ => _.GetCurrentBranchName()).Returns("branchName");
    _gitClientStub.Setup(_ => _.DoesBranchExist("prerelease/v0.0.0")).Returns(false);
    var nextPossibleVersions = nextVersion.GetNextPossibleVersionsHotfix();

    _inputReaderMock.Setup(_ => _.ReadVersionChoice(It.IsAny<string>(), nextPossibleVersions)).Returns(nextJiraVersion).Verifiable();

    var rcStep = new ReleaseRCStep(
        _gitClientStub.Object,
        _config,
        _inputReaderMock.Object,
        _ancestorStub.Object,
        _msBuildInvokerMock.Object,
        _continueAlphaBetaMock.Object,
        _consoleStub.Object,
        _releaseVersionAndMoveIssueMock.Object);

    Assert.That(
        () => rcStep.Execute(nextVersion, "", false, false, "hotfix/v1.3.5"),
        Throws.Nothing);
    _releaseVersionAndMoveIssueMock.Verify(_ => _.Execute(nextVersion, nextJiraVersion, false, false), Times.Exactly(1));
    _inputReaderMock.Verify();
  }

  [Test]
  public void Execute_WithPauseForCommit_DoesNotCallNextStep ()
  {
    var nextVersion = new SemanticVersion { Major = 1 };
    var nextJiraVersion = new SemanticVersion();
    _gitClientStub.Setup(_ => _.IsWorkingDirectoryClean()).Returns(true);
    _gitClientStub.Setup(_ => _.IsCommitHash("")).Returns(true);
    _gitClientStub.Setup(_ => _.IsOnBranch("release/")).Returns(true);
    _gitClientStub.Setup(_ => _.GetCurrentBranchName()).Returns("branchName");
    _gitClientStub.Setup(_ => _.DoesBranchExist("prerelease/v0.0.0")).Returns(false);
    var nextPossibleVersions = nextVersion.GetNextPossibleVersionsHotfix();

    _inputReaderMock.Setup(_ => _.ReadVersionChoice(It.IsAny<string>(), nextPossibleVersions)).Returns(nextJiraVersion);

    var rcStep = new ReleaseRCStep(
        _gitClientStub.Object,
        _config,
        _inputReaderMock.Object,
        _ancestorStub.Object,
        _msBuildInvokerMock.Object,
        _continueAlphaBetaMock.Object,
        _consoleStub.Object,
        _releaseVersionAndMoveIssueMock.Object);

    Assert.That(
        () => rcStep.Execute(nextVersion, "", true, false, "hotfix/v1.3.5"),
        Throws.Nothing);

    _releaseVersionAndMoveIssueMock.Verify(_ => _.Execute(nextVersion, nextJiraVersion, false, false), Times.Exactly(1));
    _continueAlphaBetaMock.Verify(_ => _.Execute(nextVersion, It.IsAny<string>(), It.IsAny<string>(), false), Times.Never);
  }

  [Test]
  public void Execute_WithoutErrors_CallsNextStep ()
  {
    var nextVersion = new SemanticVersion { Major = 1 };
    var nextJiraVersion = new SemanticVersion();
    _gitClientStub.Setup(_ => _.IsWorkingDirectoryClean()).Returns(true);
    _gitClientStub.Setup(_ => _.IsCommitHash("")).Returns(true);
    _gitClientStub.Setup(_ => _.IsOnBranch("release/")).Returns(true);
    _gitClientStub.Setup(_ => _.GetCurrentBranchName()).Returns("branchName");
    _gitClientStub.Setup(_ => _.DoesBranchExist("prerelease/v0.0.0")).Returns(false);
    var nextPossibleVersions = nextVersion.GetNextPossibleVersionsHotfix();

    _inputReaderMock.Setup(_ => _.ReadVersionChoice(It.IsAny<string>(), nextPossibleVersions)).Returns(nextJiraVersion);

    var rcStep = new ReleaseRCStep(
        _gitClientStub.Object,
        _config,
        _inputReaderMock.Object,
        _ancestorStub.Object,
        _msBuildInvokerMock.Object,
        _continueAlphaBetaMock.Object,
        _consoleStub.Object,
        _releaseVersionAndMoveIssueMock.Object);

    Assert.That(
        () => rcStep.Execute(nextVersion, "", false, false, "hotfix/v1.3.5"),
        Throws.Nothing);
    _releaseVersionAndMoveIssueMock.Verify(_ => _.Execute(nextVersion, nextJiraVersion, false, false), Times.Exactly(1));
    _continueAlphaBetaMock.Verify(_ => _.Execute(It.IsAny<SemanticVersion>(), It.IsAny<string>(), It.IsAny<string>(), false));
  }

  [Test]
  public void Execute_WithoutErrors_CallsMBuildInvokeAndCommit ()
  {
    var nextVersion = new SemanticVersion { Major = 1 };
    var nextJiraVersion = new SemanticVersion();
    _gitClientStub.Setup(_ => _.IsWorkingDirectoryClean()).Returns(true);
    _gitClientStub.Setup(_ => _.IsCommitHash("")).Returns(true);
    _gitClientStub.Setup(_ => _.IsOnBranch("release/")).Returns(true);
    _gitClientStub.Setup(_ => _.GetCurrentBranchName()).Returns("branchName");
    _gitClientStub.Setup(_ => _.DoesBranchExist("prerelease/v0.0.0")).Returns(false);
    var nextPossibleVersions = nextVersion.GetNextPossibleVersionsHotfix();
    _inputReaderMock.Setup(_ => _.ReadVersionChoice(It.IsAny<string>(), nextPossibleVersions)).Returns(nextJiraVersion);

    var rcStep = new ReleaseRCStep(
        _gitClientStub.Object,
        _config,
        _inputReaderMock.Object,
        _ancestorStub.Object,
        _msBuildInvokerMock.Object,
        _continueAlphaBetaMock.Object,
        _consoleStub.Object,
        _releaseVersionAndMoveIssueMock.Object);

    Assert.That(
        () => rcStep.Execute(nextVersion, "", false, false, "hotfix/v1.3.5"),
        Throws.Nothing);
    _releaseVersionAndMoveIssueMock.Verify(_ => _.Execute(nextVersion, nextJiraVersion, false, false), Times.Exactly(1));
    _msBuildInvokerMock.Verify(_ => _.CallMSBuildStepsAndCommit(MSBuildMode.PrepareNextVersion, nextVersion));
  }
}