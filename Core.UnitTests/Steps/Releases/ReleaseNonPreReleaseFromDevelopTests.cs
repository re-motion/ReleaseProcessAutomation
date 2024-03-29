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

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using Moq;
using NUnit.Framework;
using Remotion.ReleaseProcessAutomation.Configuration;
using Remotion.ReleaseProcessAutomation.Configuration.Data;
using Remotion.ReleaseProcessAutomation.Git;
using Remotion.ReleaseProcessAutomation.ReadInput;
using Remotion.ReleaseProcessAutomation.Scripting;
using Remotion.ReleaseProcessAutomation.SemanticVersioning;
using Remotion.ReleaseProcessAutomation.Steps.PipelineSteps;
using Remotion.ReleaseProcessAutomation.Steps.SubSteps;
using Spectre.Console;

namespace Remotion.ReleaseProcessAutomation.UnitTests.Steps.Releases;

[TestFixture]
internal class ReleaseNonPreReleaseFromDevelopTests
{
  private const string c_configFileName = "ReleaseProcessScript.Test.Config";

  private Mock<IAnsiConsole> _consoleMock;
  private Configuration.Data.Config _config;
  private Mock<IMSBuildCallAndCommit> _msBuildInvokerMock;
  private Mock<IContinueReleaseOnMasterStep> _continueReleaseOnMasterMock;
  private Mock<IPushNewReleaseBranchStep> _pushNewReleaseBranchMock;
  private Mock<IReleaseVersionAndMoveIssuesSubStep> _releaseVersionAndMoveIssuesMock;
  private Mock<IAddFixVersionsForNewReleaseBranchSubStep> _addFixVersionsSubStepMock;

  [SetUp]
  public void Setup ()
  {
    var path = Path.Join(TestContext.CurrentContext.TestDirectory, c_configFileName);
    _config = new ConfigReader().LoadConfig(path);
    _msBuildInvokerMock = new Mock<IMSBuildCallAndCommit>();
    _continueReleaseOnMasterMock = new Mock<IContinueReleaseOnMasterStep>();
    _consoleMock = new Mock<IAnsiConsole>();
    _pushNewReleaseBranchMock = new Mock<IPushNewReleaseBranchStep>(MockBehavior.Strict);
    _releaseVersionAndMoveIssuesMock = new Mock<IReleaseVersionAndMoveIssuesSubStep>();
    _addFixVersionsSubStepMock = new Mock<IAddFixVersionsForNewReleaseBranchSubStep>();
  }

  [Test]
  public void Execute_WithWorkingDirectoryNotCleanAndNoConfirmationToContinue_ThrowsException ()
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
        _pushNewReleaseBranchMock.Object,
        _config,
        _msBuildInvokerMock.Object,
        _consoleMock.Object,
        _releaseVersionAndMoveIssuesMock.Object,
        _addFixVersionsSubStepMock.Object);

    Assert.That(
        () => step.Execute(new SemanticVersion(), "", false, false, false),
        Throws.InstanceOf<Exception>()
            .With.Message.EqualTo("Working directory not clean, user does not want to continue. Release process stopped."));
  }

  [Test]
  public void Execute_WithoutCurrentBranchName_ThrowsException ()
  {
    var nextVersion = new SemanticVersion
                      {
                          Major = 1,
                          Minor = 1,
                          Patch = 1
                      };
    var gitClientStub = new Mock<IGitClient>();
    gitClientStub.Setup(_ => _.IsWorkingDirectoryClean()).Returns(true);
    gitClientStub.Setup(_ => _.IsOnBranch("develop")).Returns(true);
    gitClientStub.Setup(_ => _.GetCurrentBranchName()).Returns("develop");
    gitClientStub.Setup(_ => _.DoesBranchExist($"release/v{nextVersion}")).Returns(true);

    var readInputStub = new Mock<IInputReader>();

    var step = new ReleaseOnMasterStep(
        gitClientStub.Object,
        readInputStub.Object,
        _continueReleaseOnMasterMock.Object,
        _pushNewReleaseBranchMock.Object,
        _config,
        _msBuildInvokerMock.Object,
        _consoleMock.Object,
        _releaseVersionAndMoveIssuesMock.Object,
        _addFixVersionsSubStepMock.Object);

    Assert.That(
        () => step.Execute(nextVersion, "commitHash", false, false, false),
        Throws.InstanceOf<Exception>()
            .With.Message.EqualTo("The branch 'release/v1.1.1' already exists."));
  }

  [Test]
  public void Execute_CallsNextStep ()
  {
    var nextVersion = new SemanticVersion
                      {
                          Major = 1,
                          Minor = 1,
                          Patch = 1
                      };
    var nextJiraVersion = new SemanticVersion();
    var gitClientStub = new Mock<IGitClient>();
    gitClientStub.Setup(_ => _.IsWorkingDirectoryClean()).Returns(true);
    gitClientStub.Setup(_ => _.IsOnBranch("develop")).Returns(true);
    gitClientStub.Setup(_ => _.GetCurrentBranchName()).Returns("develop");
    gitClientStub.Setup(_ => _.DoesBranchExist($"release/v{nextVersion}")).Returns(false);

    var readInputStub = new Mock<IInputReader>();
    readInputStub.Setup(_ => _.ReadVersionChoice(It.IsAny<string>(), It.IsAny<IReadOnlyCollection<SemanticVersion>>())).Returns(nextJiraVersion);

    var step = new ReleaseOnMasterStep(
        gitClientStub.Object,
        readInputStub.Object,
        _continueReleaseOnMasterMock.Object,
        _pushNewReleaseBranchMock.Object,
        _config,
        _msBuildInvokerMock.Object,
        _consoleMock.Object,
        _releaseVersionAndMoveIssuesMock.Object,
        _addFixVersionsSubStepMock.Object);

    step.Execute(nextVersion, "commitHash", false, false, false);

    _releaseVersionAndMoveIssuesMock.Verify(_ => _.Execute(nextVersion, nextJiraVersion, _config.Jira.JiraProjectKey, false, false), Times.Exactly(1));
    _continueReleaseOnMasterMock.Verify(_ => _.Execute(nextVersion, It.IsAny<bool>()));
  }

  [Test]
  public void Execute_WithStartReleasePhase_DoesNotCallNextStepButPushReleaseBranchStepAndAddFixVersionSubStep ()
  {
    var nextVersion = new SemanticVersion
                      {
                          Major = 1,
                          Minor = 1,
                          Patch = 1
                      };
    var nextJiraVersion = new SemanticVersion();
    var gitClientStub = new Mock<IGitClient>();
    gitClientStub.Setup(_ => _.IsWorkingDirectoryClean()).Returns(true);
    gitClientStub.Setup(_ => _.IsOnBranch("develop")).Returns(true);
    gitClientStub.Setup(_ => _.GetCurrentBranchName()).Returns("develop");
    gitClientStub.Setup(_ => _.DoesBranchExist($"release/v{nextVersion}")).Returns(false);

    _pushNewReleaseBranchMock.Setup(_ => _.Execute($"release/v{nextVersion}", "develop"));
    
    var readInputStub = new Mock<IInputReader>();
    readInputStub.Setup(_ => _.ReadVersionChoice(It.IsAny<string>(), It.IsAny<IReadOnlyCollection<SemanticVersion>>())).Returns(nextJiraVersion);

    var step = new ReleaseOnMasterStep(
        gitClientStub.Object,
        readInputStub.Object,
        _continueReleaseOnMasterMock.Object,
        _pushNewReleaseBranchMock.Object,
        _config,
        _msBuildInvokerMock.Object,
        _consoleMock.Object,
        _releaseVersionAndMoveIssuesMock.Object,
        _addFixVersionsSubStepMock.Object);

    step.Execute(nextVersion, "commitHash", true, false, false);

    _addFixVersionsSubStepMock.Verify(_ => _.Execute(nextVersion, nextJiraVersion, _config.Jira.JiraProjectKey), Times.Once);
    _releaseVersionAndMoveIssuesMock.Verify(_ => _.Execute(nextVersion, nextJiraVersion, _config.Jira.JiraProjectKey, false, false), Times.Never);
    _pushNewReleaseBranchMock.Verify(_ => _.Execute($"release/v{nextVersion}", "develop"));
    _continueReleaseOnMasterMock.Verify(_ => _.Execute(nextVersion, It.IsAny<bool>()), Times.Never);
  }

  [Test]
  public void Execute_WithStartReleasePhase_UpdatesTheVersionPropsToTheFullVersionOnDevelop ()
  {
    var nextVersion = new SemanticVersion
      {
        Major = 1,
        Minor = 1,
        Patch = 1
      };
    var nextJiraVersion = new SemanticVersion
      {
        Major = 2,
        Minor = 1,
        Patch = 3,
        Pre = PreReleaseStage.alpha,
        PreReleaseCounter = 1
      };

    var gitClientStub = new Mock<IGitClient>();
    gitClientStub.Setup(_ => _.IsWorkingDirectoryClean()).Returns(true);
    gitClientStub.Setup(_ => _.IsOnBranch("develop")).Returns(true);
    gitClientStub.Setup(_ => _.GetCurrentBranchName()).Returns("develop");
    gitClientStub.Setup(_ => _.DoesBranchExist($"release/v{nextVersion}")).Returns(false);
    _pushNewReleaseBranchMock.Setup(_ => _.Execute($"release/v{nextVersion}", "develop"));


    var readInputStub = new Mock<IInputReader>();
    readInputStub.Setup(_ => _.ReadVersionChoice(It.IsAny<string>(), It.IsAny<IReadOnlyCollection<SemanticVersion>>())).Returns(nextJiraVersion);

    var step = new ReleaseOnMasterStep(
        gitClientStub.Object,
        readInputStub.Object,
        _continueReleaseOnMasterMock.Object,
        _pushNewReleaseBranchMock.Object,
        _config,
        _msBuildInvokerMock.Object,
        _consoleMock.Object,
        _releaseVersionAndMoveIssuesMock.Object,
        _addFixVersionsSubStepMock.Object);

    step.Execute(nextVersion, "commitHash", true, false, false);

    var fullVersionForNextJiraVersion = new SemanticVersion
      {
        Major = 2,
        Minor = 1,
        Patch = 3
      };
    _msBuildInvokerMock.Verify(_ => _.CallMSBuildStepsAndCommit(MSBuildMode.PrepareNextVersion, fullVersionForNextJiraVersion));

    _addFixVersionsSubStepMock.Verify(_ => _.Execute(nextVersion, nextJiraVersion, _config.Jira.JiraProjectKey), Times.Once);
    _releaseVersionAndMoveIssuesMock.Verify(_ => _.Execute(nextVersion, nextJiraVersion, _config.Jira.JiraProjectKey, false, false), Times.Never);
    _continueReleaseOnMasterMock.Verify(_ => _.Execute(nextVersion, It.IsAny<bool>()), Times.Never);
  }

  [Test]
  public void Execute_WithoutStartReleasePhase_DoesNotCallPushReleaseBranchStepAndAddFixVersionSubStep ()
  {
    var nextVersion = new SemanticVersion
                      {
                          Major = 1,
                          Minor = 1,
                          Patch = 1
                      };
    var nextJiraVersion = new SemanticVersion();
    var gitClientStub = new Mock<IGitClient>();
    gitClientStub.Setup(_ => _.IsWorkingDirectoryClean()).Returns(true);
    gitClientStub.Setup(_ => _.IsOnBranch("develop")).Returns(true);
    gitClientStub.Setup(_ => _.GetCurrentBranchName()).Returns("develop");
    gitClientStub.Setup(_ => _.DoesBranchExist($"release/v{nextVersion}")).Returns(false);

    _pushNewReleaseBranchMock.Setup(_ => _.Execute($"release/v{nextVersion}", "develop"));

    var readInputStub = new Mock<IInputReader>();
    readInputStub.Setup(_ => _.ReadVersionChoice(It.IsAny<string>(), It.IsAny<IReadOnlyCollection<SemanticVersion>>())).Returns(nextJiraVersion);

    var step = new ReleaseOnMasterStep(
        gitClientStub.Object,
        readInputStub.Object,
        _continueReleaseOnMasterMock.Object,
        _pushNewReleaseBranchMock.Object,
        _config,
        _msBuildInvokerMock.Object,
        _consoleMock.Object,
        _releaseVersionAndMoveIssuesMock.Object,
        _addFixVersionsSubStepMock.Object);

    step.Execute(nextVersion, "commitHash", false, false, false);

    _addFixVersionsSubStepMock.Verify(_ => _.Execute(It.IsAny<SemanticVersion>(), It.IsAny<SemanticVersion>(), It.IsAny<string>()), Times.Never);
    _pushNewReleaseBranchMock.Verify(_ => _.Execute(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
  }

  [Test]
  public void Execute_WithPauseForCommit_DoesNotCallNextStep ()
  {
    var nextVersion = new SemanticVersion
                      {
                          Major = 1,
                          Minor = 1,
                          Patch = 1
                      };
    var nextJiraVersion = new SemanticVersion();

    var gitClientStub = new Mock<IGitClient>();
    gitClientStub.Setup(_ => _.IsWorkingDirectoryClean()).Returns(true);
    gitClientStub.Setup(_ => _.IsOnBranch("develop")).Returns(true);
    gitClientStub.Setup(_ => _.GetCurrentBranchName()).Returns("develop");
    gitClientStub.Setup(_ => _.DoesBranchExist($"release/v{nextVersion}")).Returns(false);

    var readInputStub = new Mock<IInputReader>();
    readInputStub.Setup(_ => _.ReadVersionChoice(It.IsAny<string>(), It.IsAny<IReadOnlyCollection<SemanticVersion>>())).Returns(nextJiraVersion);

    var step = new ReleaseOnMasterStep(
        gitClientStub.Object,
        readInputStub.Object,
        _continueReleaseOnMasterMock.Object, 
        _pushNewReleaseBranchMock.Object,
        _config,
        _msBuildInvokerMock.Object,
        _consoleMock.Object,
        _releaseVersionAndMoveIssuesMock.Object,
        _addFixVersionsSubStepMock.Object);

    step.Execute(nextVersion, "commitHash", false, true, false);
    _releaseVersionAndMoveIssuesMock.Verify(_ => _.Execute(nextVersion, nextJiraVersion, _config.Jira.JiraProjectKey, false, false), Times.Exactly(1));

    _continueReleaseOnMasterMock.Verify(_ => _.Execute(nextVersion, It.IsAny<bool>()), Times.Never);
  }
}