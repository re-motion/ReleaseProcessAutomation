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

internal class ReleaseAlphaBetaStepTests
{
  private const string c_configFileName = "ReleaseProcessScript.Test.Config";

  private Configuration.Data.Config _config;
  private Mock<IAnsiConsole> _consoleMock;
  private Mock<IContinueAlphaBetaStep> _continueAlphaBetaMock;
  private Mock<IGitClient> _gitClientStub;
  private Mock<IInputReader> _inputReaderStub;
  private Mock<IReleaseVersionAndMoveIssuesSubStep> _releaseVersionAndMoveIssuesMock;
  private Mock<IMSBuildCallAndCommit> _msBuildInvokerMock;

  [SetUp]
  public void Setup ()
  {
    _gitClientStub = new Mock<IGitClient>();
    _inputReaderStub = new Mock<IInputReader>();
    _msBuildInvokerMock = new Mock<IMSBuildCallAndCommit>();
    _continueAlphaBetaMock = new Mock<IContinueAlphaBetaStep>();
    _consoleMock = new Mock<IAnsiConsole>();
    _releaseVersionAndMoveIssuesMock = new Mock<IReleaseVersionAndMoveIssuesSubStep>();

    var path = Path.Join(TestContext.CurrentContext.TestDirectory, c_configFileName);
    _config = new ConfigReader().LoadConfig(path);
  }

  [Test]
  public void Execute_OnMaster_CallsNextStep ()
  {
    var nextVersion = new SemanticVersion
                      {
                          Major = 1,
                          Minor = 1,
                          Patch = 1
                      };
    var nextJiraVersion = new SemanticVersion();
    _gitClientStub.Setup(_ => _.IsWorkingDirectoryClean()).Returns(true);
    _gitClientStub.Setup(_ => _.IsCommitHash("")).Returns(true);
    _gitClientStub.Setup(_ => _.IsOnBranch("develop")).Returns(true);
    _gitClientStub.Setup(_ => _.GetCurrentBranchName()).Returns("develop");
    var nextPossibleVersions = nextVersion.GetNextPossibleVersionsDevelop();
    _releaseVersionAndMoveIssuesMock.Setup(_ => _.Execute(nextVersion, nextJiraVersion, _config.Jira.JiraProjectKey, false, true)).Verifiable();

    _inputReaderStub.Setup(_ => _.ReadVersionChoice(It.IsAny<string>(), nextPossibleVersions)).Returns(nextJiraVersion);

    var alphaBetaStep = new ReleaseAlphaBetaStep(
        _gitClientStub.Object,
        _config,
        _inputReaderStub.Object,
        _msBuildInvokerMock.Object,
        _continueAlphaBetaMock.Object,
        _consoleMock.Object,
        _releaseVersionAndMoveIssuesMock.Object);

    Assert.That(
        () => alphaBetaStep.Execute(nextVersion, "", false, false),
        Throws.Nothing);
    _continueAlphaBetaMock.Verify(_ => _.Execute(nextVersion, "develop", "develop", false));
    _releaseVersionAndMoveIssuesMock.Verify();
  }

  [Test]
  public void Execute_OnMasterWithPauseForCommit_CallsInvokeMBuildAndCommitButNotNextStep ()
  {
    var nextVersion = new SemanticVersion
                      {
                          Major = 1,
                          Minor = 1,
                          Patch = 1
                      };
    var nextJiraVersion = new SemanticVersion();
    _gitClientStub.Setup(_ => _.IsWorkingDirectoryClean()).Returns(true);
    _gitClientStub.Setup(_ => _.IsCommitHash("")).Returns(true);
    _gitClientStub.Setup(_ => _.IsOnBranch("develop")).Returns(true);
    _gitClientStub.Setup(_ => _.GetCurrentBranchName()).Returns("develop");
    var nextPossibleVersions = nextVersion.GetNextPossibleVersionsDevelop();
    _inputReaderStub.Setup(_ => _.ReadVersionChoice(It.IsAny<string>(), nextPossibleVersions)).Returns(nextJiraVersion);
    _releaseVersionAndMoveIssuesMock.Setup(_ => _.Execute(nextVersion, nextJiraVersion, _config.Jira.JiraProjectKey, false, true)).Verifiable();

    var alphaBetaStep = new ReleaseAlphaBetaStep(
        _gitClientStub.Object,
        _config,
        _inputReaderStub.Object,
        _msBuildInvokerMock.Object,
        _continueAlphaBetaMock.Object,
        _consoleMock.Object,
        _releaseVersionAndMoveIssuesMock.Object);

    Assert.That(
        () => alphaBetaStep.Execute(nextVersion, "", true, false),
        Throws.Nothing);

    _msBuildInvokerMock.Verify(_ => _.CallMSBuildStepsAndCommit(It.IsAny<MSBuildMode>(), nextVersion));
    _releaseVersionAndMoveIssuesMock.Verify(_ => _.Execute(nextVersion, nextJiraVersion, _config.Jira.JiraProjectKey, false, true), Times.Exactly(1));
    _continueAlphaBetaMock.Verify(_ => _.Execute(nextVersion, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
    _releaseVersionAndMoveIssuesMock.Verify();
  }
}