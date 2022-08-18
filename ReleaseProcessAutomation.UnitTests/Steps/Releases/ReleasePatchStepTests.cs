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
internal class ReleasePatchStepTests
{
  private const string c_configFileName = "ReleaseProcessScript.Test.Config";

  private Mock<IAnsiConsole> _consoleStub;
  private Mock<IGitClient> _gitClientStub;
  private Mock<IInputReader> _inputReaderStub;
  private Configuration.Data.Config _config;
  private Mock<IMSBuildCallAndCommit> _msBuildInvokerMock;
  private Mock<IContinueReleasePatchStep> _contineReleasePatchMock;
  private Mock<IReleaseVersionAndMoveIssuesSubStep> _releaseVersionAndMoveIssueMock;
  private Mock<IPushNewReleaseBranchStep> _pushNewReleaseBranchMock;

  [SetUp]
  public void Setup ()
  {
    _gitClientStub = new Mock<IGitClient>();
    _inputReaderStub = new Mock<IInputReader>();
    _msBuildInvokerMock = new Mock<IMSBuildCallAndCommit>();
    _contineReleasePatchMock = new Mock<IContinueReleasePatchStep>();
    _consoleStub = new Mock<IAnsiConsole>();
    _releaseVersionAndMoveIssueMock = new Mock<IReleaseVersionAndMoveIssuesSubStep>();
    _pushNewReleaseBranchMock = new Mock<IPushNewReleaseBranchStep>(MockBehavior.Strict);

    var path = Path.Join(TestContext.CurrentContext.TestDirectory, c_configFileName);
    _config = new ConfigReader().LoadConfig(path);
  }

  [Test]
  public void Execute_OnMaster_CallsNextStep ()
  {
    var nextVersion = new SemanticVersion { Major = 1 };
    var nextJiraVersion = new SemanticVersion();
    _gitClientStub.Setup(_ => _.IsWorkingDirectoryClean()).Returns(true);
    _gitClientStub.Setup(_ => _.IsCommitHash("")).Returns(true);
    _gitClientStub.Setup(_ => _.IsOnBranch("master")).Returns(true);
    _gitClientStub.Setup(_ => _.GetCurrentBranchName()).Returns("master");
    var nextPossibleVersions = nextVersion.GetNextPossibleVersionsHotfix();
    _inputReaderStub.Setup(_ => _.ReadVersionChoice(It.IsAny<string>(), nextPossibleVersions)).Returns(nextJiraVersion);
    _contineReleasePatchMock.Setup(_ => _.Execute(nextVersion, false, true)).Verifiable();

    var patchStep = new ReleasePatchStep(
        _gitClientStub.Object,
        _config,
        _inputReaderStub.Object,
        _msBuildInvokerMock.Object,
        _contineReleasePatchMock.Object,
        _pushNewReleaseBranchMock.Object,
        _consoleStub.Object,
        _releaseVersionAndMoveIssueMock.Object);

    Assert.That(
        () => patchStep.Execute(nextVersion, "", false, false, false, true),
        Throws.Nothing);
    _releaseVersionAndMoveIssueMock.Verify(_ => _.Execute(nextVersion, nextJiraVersion, false, false), Times.Exactly(1));
    _contineReleasePatchMock.Verify();
  }

  [Test]
  public void Execute_OnHotfix_CallsNextStep ()
  {
    var nextVersion = new SemanticVersion { Patch = 1 };
    var nextJiraVersion = new SemanticVersion();
    _gitClientStub.Setup(_ => _.IsWorkingDirectoryClean()).Returns(true);
    _gitClientStub.Setup(_ => _.IsCommitHash("")).Returns(true);
    _gitClientStub.Setup(_ => _.IsOnBranch("hotfix/")).Returns(true);
    _gitClientStub.Setup(_ => _.GetCurrentBranchName()).Returns("hotfix/v0.0.1");
    var nextPossibleVersions = nextVersion.GetNextPossibleVersionsHotfix();
    _inputReaderStub.Setup(_ => _.ReadVersionChoice(It.IsAny<string>(), nextPossibleVersions)).Returns(nextJiraVersion);
    _contineReleasePatchMock.Setup(_ => _.Execute(nextVersion, false, false)).Verifiable();

    var patchStep = new ReleasePatchStep(
        _gitClientStub.Object,
        _config,
        _inputReaderStub.Object,
        _msBuildInvokerMock.Object,
        _contineReleasePatchMock.Object,
        _pushNewReleaseBranchMock.Object,
        _consoleStub.Object,
        _releaseVersionAndMoveIssueMock.Object);

    Assert.That(
        () => patchStep.Execute(nextVersion, "", false, false, false, false),
        Throws.Nothing);
    _releaseVersionAndMoveIssueMock.Verify(_ => _.Execute(nextVersion, nextJiraVersion, false, false), Times.Exactly(1));
    _contineReleasePatchMock.Verify();
  }

  [Test]
  public void Execute_OnHotfixWithStartReleasePhase_DoesNotCallNextStepAndInvokeAndCommitButPushReleaseBranchStep ()
  {
    var nextVersion = new SemanticVersion { Patch = 1 };
    var nextJiraVersion = new SemanticVersion();
    _gitClientStub.Setup(_ => _.IsWorkingDirectoryClean()).Returns(true);
    _gitClientStub.Setup(_ => _.IsCommitHash("")).Returns(true);
    _gitClientStub.Setup(_ => _.IsOnBranch("hotfix/")).Returns(true);
    _gitClientStub.Setup(_ => _.GetCurrentBranchName()).Returns("hotfix/v0.0.1");
    var nextPossibleVersions = nextVersion.GetNextPossibleVersionsHotfix();
    _inputReaderStub.Setup(_ => _.ReadVersionChoice(It.IsAny<string>(), nextPossibleVersions)).Returns(nextJiraVersion);
    _pushNewReleaseBranchMock.Setup(_ => _.Execute("release/v0.0.1", "hotfix/v0.0.1" ));

    
    var patchStep = new ReleasePatchStep(
        _gitClientStub.Object,
        _config,
        _inputReaderStub.Object,
        _msBuildInvokerMock.Object,
        _contineReleasePatchMock.Object,
        _pushNewReleaseBranchMock.Object,
        _consoleStub.Object,
        _releaseVersionAndMoveIssueMock.Object);

    Assert.That(
        () => patchStep.Execute(nextVersion, "", true, false, false, false),
        Throws.Nothing);
    
    _pushNewReleaseBranchMock.Verify(_ => _.Execute("release/v0.0.1", "hotfix/v0.0.1" ));
    _releaseVersionAndMoveIssueMock.Verify(_ => _.Execute(nextVersion, nextJiraVersion, false, false), Times.Never);
    _msBuildInvokerMock.Verify(_ => _.CallMSBuildStepsAndCommit(It.IsAny<MSBuildMode>(), It.IsAny<SemanticVersion>()), Times.Never);
    _contineReleasePatchMock.Verify(_ => _.Execute(It.IsAny<SemanticVersion>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
  }

  [Test]
  public void Execute_OnHotfixWithPauseForCommit_DoesNotCallNextStepButDoesCallInvokeBuildAndCommitAndJira ()
  {
    var nextVersion = new SemanticVersion { Patch = 1 };
    var nextJiraVersion = new SemanticVersion();
    _gitClientStub.Setup(_ => _.IsWorkingDirectoryClean()).Returns(true);
    _gitClientStub.Setup(_ => _.IsCommitHash("")).Returns(true);
    _gitClientStub.Setup(_ => _.IsOnBranch("hotfix/")).Returns(true);
    _gitClientStub.Setup(_ => _.GetCurrentBranchName()).Returns("hotfix/v0.0.1");
    var nextPossibleVersions = nextVersion.GetNextPossibleVersionsHotfix();
    _inputReaderStub.Setup(_ => _.ReadVersionChoice(It.IsAny<string>(), nextPossibleVersions)).Returns(nextJiraVersion);

    var patchStep = new ReleasePatchStep(
        _gitClientStub.Object,
        _config,
        _inputReaderStub.Object,
        _msBuildInvokerMock.Object,
        _contineReleasePatchMock.Object,
        _pushNewReleaseBranchMock.Object,
        _consoleStub.Object,
        _releaseVersionAndMoveIssueMock.Object);

    Assert.That(
        () => patchStep.Execute(nextVersion, "", false, true, false, false),
        Throws.Nothing);
    _releaseVersionAndMoveIssueMock.Verify(_ => _.Execute(nextVersion, nextJiraVersion, false, false), Times.Exactly(1));
    _msBuildInvokerMock.Verify(_ => _.CallMSBuildStepsAndCommit(It.IsAny<MSBuildMode>(), It.IsAny<SemanticVersion>()));
    _contineReleasePatchMock.Verify(_ => _.Execute(nextVersion, It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
  }
}