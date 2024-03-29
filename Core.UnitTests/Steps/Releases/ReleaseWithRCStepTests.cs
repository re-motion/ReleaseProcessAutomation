﻿// Copyright (c) rubicon IT GmbH, www.rubicon.eu
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

using System.IO;
using Moq;
using NUnit.Framework;
using Remotion.ReleaseProcessAutomation.Configuration;
using Remotion.ReleaseProcessAutomation.Configuration.Data;
using Remotion.ReleaseProcessAutomation.Extensions;
using Remotion.ReleaseProcessAutomation.Git;
using Remotion.ReleaseProcessAutomation.ReadInput;
using Remotion.ReleaseProcessAutomation.Scripting;
using Remotion.ReleaseProcessAutomation.SemanticVersioning;
using Remotion.ReleaseProcessAutomation.Steps.PipelineSteps;
using Remotion.ReleaseProcessAutomation.Steps.SubSteps;
using Spectre.Console;

namespace Remotion.ReleaseProcessAutomation.UnitTests.Steps.Releases;

[TestFixture]
internal class ReleaseWithRCStepTests
{
  private const string c_configFileName = "ReleaseProcessScript.Test.Config";

  private Mock<IAnsiConsole> _consoleStub;
  private Mock<IGitClient> _gitClientStub;
  private Mock<IInputReader> _inputReaderMock;
  private Configuration.Data.Config _config;
  private Mock<IAncestorFinder> _ancestorStub;
  private Mock<IMSBuildCallAndCommit> _msBuildInvokerMock;
  private Mock<IContinueReleaseOnMasterStep> _continueReleaseOnMasterMock;
  private Mock<IContinueReleasePatchStep> _continueReleasePatchMock;
  private Mock<IReleaseVersionAndMoveIssuesSubStep> _releaseVersionAndMoveIssuesMock;

  [SetUp]
  public void Setup ()
  {
    _gitClientStub = new Mock<IGitClient>();
    _inputReaderMock = new Mock<IInputReader>();
    _ancestorStub = new Mock<IAncestorFinder>();
    _msBuildInvokerMock = new Mock<IMSBuildCallAndCommit>();
    _continueReleaseOnMasterMock = new Mock<IContinueReleaseOnMasterStep>();
    _continueReleasePatchMock = new Mock<IContinueReleasePatchStep>();
    _releaseVersionAndMoveIssuesMock = new Mock<IReleaseVersionAndMoveIssuesSubStep>();

    _consoleStub = new Mock<IAnsiConsole>();

    var path = Path.Join(TestContext.CurrentContext.TestDirectory, c_configFileName);
    _config = new ConfigReader().LoadConfig(path);
  }

  [Test]
  public void Execute_WithHotfixAncestor_UsesNextPossibleVersionsFromHotfix ()
  {
    var nextVersion = new SemanticVersion { Patch = 1 };
    var nextJiraVersion = new SemanticVersion();
    _gitClientStub.Setup(_ => _.IsWorkingDirectoryClean()).Returns(true);
    _gitClientStub.Setup(_ => _.IsOnBranch("release/")).Returns(true);
    _gitClientStub.Setup(_ => _.GetCurrentBranchName()).Returns("release/v0.0.1");
    _gitClientStub.Setup(_ => _.DoesBranchExist("prerelease/v0.0.0")).Returns(false);
    var nextPossibleVersions = nextVersion.GetNextPossibleVersionsForReleaseBranchFromHotfix();
    _inputReaderMock.Setup(_ => _.ReadVersionChoice(It.IsAny<string>(), nextPossibleVersions)).Returns(nextJiraVersion).Verifiable();

    var releaseStep = new ReleaseFullVersionFromReleaseBranchStep (
        _gitClientStub.Object,
        _config,
        _inputReaderMock.Object,
        _ancestorStub.Object,
        _msBuildInvokerMock.Object,
        _continueReleaseOnMasterMock.Object,
        _continueReleasePatchMock.Object,
        _consoleStub.Object,
        _releaseVersionAndMoveIssuesMock.Object);

    Assert.That(
        () => releaseStep.Execute(false, false, "hotfix/v1.3.5"),
        Throws.Nothing);
    _inputReaderMock.Verify();
  }

  [Test]
  public void Execute_WithDevelopAncestor_UsesNextPossibleVersionsFromDevelop ()
  {
    var nextVersion = new SemanticVersion { Patch = 1 };
    var nextJiraVersion = new SemanticVersion();
    _gitClientStub.Setup(_ => _.IsWorkingDirectoryClean()).Returns(true);
    _gitClientStub.Setup(_ => _.IsOnBranch("release/")).Returns(true);
    _gitClientStub.Setup(_ => _.GetCurrentBranchName()).Returns("release/v0.0.1");
    _gitClientStub.Setup(_ => _.DoesBranchExist("prerelease/v0.0.0")).Returns(false);
    var nextPossibleVersions = nextVersion.GetNextPossibleVersionsForReleaseBranchFromDevelop();
    _inputReaderMock.Setup(_ => _.ReadVersionChoice(It.IsAny<string>(), nextPossibleVersions)).Returns(nextJiraVersion).Verifiable();

    var releaseStep = new ReleaseFullVersionFromReleaseBranchStep (
        _gitClientStub.Object,
        _config,
        _inputReaderMock.Object,
        _ancestorStub.Object,
        _msBuildInvokerMock.Object,
        _continueReleaseOnMasterMock.Object,
        _continueReleasePatchMock.Object,
        _consoleStub.Object,
        _releaseVersionAndMoveIssuesMock.Object);

    Assert.That(
        () => releaseStep.Execute(false, false, "develop"),
        Throws.Nothing);
    _inputReaderMock.Verify();
  }

  [Test]
  public void Execute_CallsMBuildInvokeAndCommitAndJira ()
  {
    var nextVersion = new SemanticVersion { Patch = 1 };
    var nextJiraVersion = new SemanticVersion();
    _gitClientStub.Setup(_ => _.IsWorkingDirectoryClean()).Returns(true);
    _gitClientStub.Setup(_ => _.IsOnBranch("release/")).Returns(true);
    _gitClientStub.Setup(_ => _.GetCurrentBranchName()).Returns("release/v0.0.1");
    _gitClientStub.Setup(_ => _.DoesBranchExist("prerelease/v0.0.0")).Returns(false);
    var nextPossibleVersions = nextVersion.GetNextPossibleVersionsForReleaseBranchFromDevelop();
    _inputReaderMock.Setup(_ => _.ReadVersionChoice(It.IsAny<string>(), nextPossibleVersions)).Returns(nextJiraVersion);

    var releaseStep = new ReleaseFullVersionFromReleaseBranchStep (
        _gitClientStub.Object,
        _config,
        _inputReaderMock.Object,
        _ancestorStub.Object,
        _msBuildInvokerMock.Object,
        _continueReleaseOnMasterMock.Object,
        _continueReleasePatchMock.Object,
        _consoleStub.Object,
        _releaseVersionAndMoveIssuesMock.Object);

    Assert.That(
        () => releaseStep.Execute(false, false, "develop"),
        Throws.Nothing);
    _releaseVersionAndMoveIssuesMock.Verify(_ => _.Execute(nextVersion, nextJiraVersion, _config.Jira.JiraProjectKey, false, false), Times.Exactly(1));
    _msBuildInvokerMock.Verify(_ => _.CallMSBuildStepsAndCommit(MSBuildMode.PrepareNextVersion, nextVersion));
  }

  [Test]
  public void Execute_WithHotfixAncestor_CallsNextStepAndJira ()
  {
    var nextVersion = new SemanticVersion { Patch = 1 };
    var nextJiraVersion = new SemanticVersion();
    _gitClientStub.Setup(_ => _.IsWorkingDirectoryClean()).Returns(true);
    _gitClientStub.Setup(_ => _.IsOnBranch("release/")).Returns(true);
    _gitClientStub.Setup(_ => _.GetCurrentBranchName()).Returns("release/v0.0.1");
    _gitClientStub.Setup(_ => _.DoesBranchExist("prerelease/v0.0.0")).Returns(false);
    var nextPossibleVersions = nextVersion.GetNextPossibleVersionsForReleaseBranchFromHotfix();
    _inputReaderMock.Setup(_ => _.ReadVersionChoice(It.IsAny<string>(), nextPossibleVersions)).Returns(nextJiraVersion);

    var releaseStep = new ReleaseFullVersionFromReleaseBranchStep (
        _gitClientStub.Object,
        _config,
        _inputReaderMock.Object,
        _ancestorStub.Object,
        _msBuildInvokerMock.Object,
        _continueReleaseOnMasterMock.Object,
        _continueReleasePatchMock.Object,
        _consoleStub.Object,
        _releaseVersionAndMoveIssuesMock.Object);

    Assert.That(
        () => releaseStep.Execute(false, false, "hotfix/v1.3.5"),
        Throws.Nothing);

    _releaseVersionAndMoveIssuesMock.Verify(_ => _.Execute(nextVersion, nextJiraVersion, _config.Jira.JiraProjectKey, false, false), Times.Exactly(1));
    _continueReleasePatchMock.Verify(_ => _.Execute(nextVersion, false, false));
  }

  [Test]
  public void Execute_WithDevelopAncestor_CallsNextStepAndJira ()
  {
    var nextVersion = new SemanticVersion { Patch = 1 };
    var nextJiraVersion = new SemanticVersion();
    _gitClientStub.Setup(_ => _.IsWorkingDirectoryClean()).Returns(true);
    _gitClientStub.Setup(_ => _.IsOnBranch("release/")).Returns(true);
    _gitClientStub.Setup(_ => _.GetCurrentBranchName()).Returns("release/v0.0.1");
    _gitClientStub.Setup(_ => _.DoesBranchExist("prerelease/v0.0.1")).Returns(false);
    var nextPossibleVersions = nextVersion.GetNextPossibleVersionsForReleaseBranchFromDevelop();
    _inputReaderMock.Setup(_ => _.ReadVersionChoice(It.IsAny<string>(), nextPossibleVersions)).Returns(nextJiraVersion);

    var releaseStep = new ReleaseFullVersionFromReleaseBranchStep (
        _gitClientStub.Object,
        _config,
        _inputReaderMock.Object,
        _ancestorStub.Object,
        _msBuildInvokerMock.Object,
        _continueReleaseOnMasterMock.Object,
        _continueReleasePatchMock.Object,
        _consoleStub.Object,
        _releaseVersionAndMoveIssuesMock.Object);

    Assert.That(
        () => releaseStep.Execute(false, false, "develop"),
        Throws.Nothing);

    _releaseVersionAndMoveIssuesMock.Verify(_ => _.Execute(nextVersion, nextJiraVersion, _config.Jira.JiraProjectKey, false, false), Times.Exactly(1));
    _continueReleaseOnMasterMock.Verify(_ => _.Execute(nextVersion, false));
  }

  [Test]
  public void Execute_WithDevelopAncestorAndPause_DoesNotCallNextStepDoesCallMsBuildAndJira ()
  {
    var nextVersion = new SemanticVersion { Patch = 1 };
    var nextJiraVersion = new SemanticVersion();
    _gitClientStub.Setup(_ => _.IsWorkingDirectoryClean()).Returns(true);
    _gitClientStub.Setup(_ => _.IsOnBranch("release/")).Returns(true);
    _gitClientStub.Setup(_ => _.GetCurrentBranchName()).Returns("release/v0.0.1");
    _gitClientStub.Setup(_ => _.DoesBranchExist("prerelease/v0.0.1")).Returns(false);
    var nextPossibleVersions = new SemanticVersion().GetNextPossibleVersionsForReleaseBranchFromDevelop();
    _inputReaderMock.Setup(_ => _.ReadVersionChoice(It.IsAny<string>(), nextPossibleVersions)).Returns(nextJiraVersion);

    var releaseStep = new ReleaseFullVersionFromReleaseBranchStep (
        _gitClientStub.Object,
        _config,
        _inputReaderMock.Object,
        _ancestorStub.Object,
        _msBuildInvokerMock.Object,
        _continueReleaseOnMasterMock.Object,
        _continueReleasePatchMock.Object,
        _consoleStub.Object,
        _releaseVersionAndMoveIssuesMock.Object);

    Assert.That(
        () => releaseStep.Execute(true, false, "develop"),
        Throws.Nothing);

    _releaseVersionAndMoveIssuesMock.Verify(_ => _.Execute(nextVersion, nextJiraVersion, _config.Jira.JiraProjectKey, false, false), Times.Exactly(1));
    _msBuildInvokerMock.Verify(_ => _.CallMSBuildStepsAndCommit(It.IsAny<MSBuildMode>(), nextVersion));
    _continueReleaseOnMasterMock.Verify(_ => _.Execute(nextVersion, It.IsAny<bool>()), Times.Never);
    _continueReleasePatchMock.Verify(_ => _.Execute(nextVersion, It.IsAny<bool>(), false), Times.Never);
  }
}