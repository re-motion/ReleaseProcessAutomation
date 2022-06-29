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
using System.ComponentModel.Design;
using System.IO;
using Moq;
using NUnit.Framework;
using ReleaseProcessAutomation.Configuration;
using ReleaseProcessAutomation.Git;
using ReleaseProcessAutomation.ReadInput;
using ReleaseProcessAutomation.Scripting;
using ReleaseProcessAutomation.SemanticVersioning;
using ReleaseProcessAutomation.Steps.PipelineSteps;
using Spectre.Console;

namespace ReleaseProcessAutomation.Tests.Steps.Continues;

[TestFixture]
internal class ContinueReleaseOnMasterStepTests
{
  [SetUp]
  public void Setup ()
  {
    var path = Path.Join(TestContext.CurrentContext.TestDirectory, c_configFileName);
    _config = new ConfigReader().LoadConfig(path);

    _nextReleaseStepMock = new Mock<IPushMasterReleaseStep>();
    _nextReleaseStepMock.Setup(_ => _.Execute(It.IsAny<SemanticVersion>())).Verifiable();
    _consoleStub = new Mock<IAnsiConsole>();
    _msBuildCallAndCommitStub = new Mock<IMSBuildCallAndCommit>();
  }

  private Mock<IAnsiConsole> _consoleStub;
  private Configuration.Data.Config _config;
  private Mock<IPushMasterReleaseStep> _nextReleaseStepMock;
  private Mock<IMSBuildCallAndCommit> _msBuildCallAndCommitStub;
  private const string c_configFileName = "ReleaseProcessScript.Test.Config";

  [Test]
  public void CreateTagAndMerge_WithEmptyBranchName_ThrowsException ()
  {
    var version = new SemanticVersion();
    var gitClientStub = new Mock<IGitClient>();
    gitClientStub.Setup(_ => _.IsOnBranch("release/")).Returns(true);
    gitClientStub.Setup(_ => _.IsWorkingDirectoryClean()).Returns(true);
    gitClientStub.Setup(_ => _.GetCurrentBranchName()).Returns((string) null);

    var readInputStub = new Mock<IInputReader>();
    readInputStub.Setup(
            _ => _
                .ReadVersionChoice(It.IsAny<string>(), It.IsAny<IReadOnlyCollection<SemanticVersion>>()))
        .Returns(new SemanticVersion());

    var step = new ContinueReleaseOnMasterStep(
        gitClientStub.Object,
        _config,
        readInputStub.Object,
        _nextReleaseStepMock.Object,
        _consoleStub.Object,
        _msBuildCallAndCommitStub.Object
    );

    Assert.That(
        () => step.Execute(version, false),
        Throws.InstanceOf<InvalidOperationException>()
            .With.Message.EqualTo("Could not create tag and merge because there was no current branch found."));
  }

  [Test]
  public void CreateTagAndMerge_WithAlreadySetTag_ThrowsException ()
  {
    var version = new SemanticVersion();
    var gitClientStub = new Mock<IGitClient>();
    gitClientStub.Setup(_ => _.IsWorkingDirectoryClean()).Returns(true);
    gitClientStub.Setup(_ => _.IsOnBranch("release/")).Returns(true);
    gitClientStub.Setup(_ => _.GetCurrentBranchName()).Returns("release/v1.0.0");
    gitClientStub.Setup(_ => _.GetHash(It.IsAny<string>(), It.IsAny<string>())).Returns("branch");
    gitClientStub.Setup(_ => _.DoesTagExist("v1.0.0")).Returns(true);

    var readInputStub = new Mock<IInputReader>();
    readInputStub.Setup(
            _ => _
                .ReadVersionChoice(It.IsAny<string>(), It.IsAny<IReadOnlyCollection<SemanticVersion>>()))
        .Returns(new SemanticVersion());

    var step = new ContinueReleaseOnMasterStep(
        gitClientStub.Object,
        _config,
        readInputStub.Object,
        _nextReleaseStepMock.Object,
        _consoleStub.Object,
        _msBuildCallAndCommitStub.Object
    );

    Assert.That(
        () => step.Execute(version, false),
        Throws.InstanceOf<Exception>()
            .With.Message.EqualTo("There is already a commit tagged with 'v1.0.0'."));
  }

  [Test]
  public void CreateTagAndMerge_DoesNotMergeIntoDevelop_ContinuesToNextStep ()
  {
      var version = new SemanticVersionParser().ParseVersion("1.0.0");
    var gitClientMock = new Mock<IGitClient>();
    bool developLast = false;
    gitClientMock.Setup(_ => _.IsWorkingDirectoryClean()).Returns(true);
    gitClientMock.Setup(_ => _.GetCurrentBranchName()).Returns("release/v1.0.0");
    gitClientMock.Setup(_ => _.IsOnBranch("release/")).Returns(true);
    gitClientMock.Setup(_ => _.GetHash(It.IsAny<string>(), It.IsAny<string>())).Returns("branch");
    gitClientMock.Setup(_ => _.DoesTagExist("v1.0.0")).Returns(false);
    gitClientMock.Setup(_ => _.Reset(It.IsAny<string>())).Verifiable();
    gitClientMock.Setup(_ => _.Checkout("develop")).Callback(() => developLast = true);
    gitClientMock.Setup(_ => _.Checkout("master")).Callback(() => developLast = false);
    
    //checks that the current branch when calling merge branch is not develop, therefore not merging into it
    gitClientMock.Setup(_ => _.MergeBranchToOnlyContainChangesFromMergedBranch(It.IsAny<string>())).Callback(() => Assert.That(developLast, Is.False));
    
    var readInputStub = new Mock<IInputReader>();
    readInputStub.Setup(
            _ => _
                .ReadVersionChoice(It.IsAny<string>(), It.IsAny<IReadOnlyCollection<SemanticVersion>>()))
        .Returns(new SemanticVersion());
    _config.DevelopStableMergeIgnoreList.FileName = new string[] { };

    var step = new ContinueReleaseOnMasterStep(
        gitClientMock.Object,
        _config,
        readInputStub.Object,
        _nextReleaseStepMock.Object,
        _consoleStub.Object,
        _msBuildCallAndCommitStub.Object
    );

    Assert.That(() => step.Execute(version, false), Throws.Nothing);

    gitClientMock.Verify(_ => _.MergeBranchToOnlyContainChangesFromMergedBranch("release/v1.0.0"), Times.Once);
    _nextReleaseStepMock.Verify(_=>_.Execute(It.IsAny<SemanticVersion>()), Times.Once);
  }

  [Test]
  public void Execute_WithOutErrors_CallsNextExecute ()
  {
    var version = new SemanticVersion();
    var gitClientStub = new Mock<IGitClient>();
    gitClientStub.Setup(_ => _.IsWorkingDirectoryClean()).Returns(true);
    gitClientStub.Setup(_ => _.IsOnBranch("release/")).Returns(true);
    gitClientStub.Setup(_ => _.GetCurrentBranchName()).Returns("release/v1.0.0");
    gitClientStub.Setup(_ => _.GetHash(It.IsAny<string>(), It.IsAny<string>())).Returns("branch");
    gitClientStub.Setup(_ => _.DoesTagExist("v1.0.0")).Returns(false);

    var readInputStub = new Mock<IInputReader>();
    readInputStub.Setup(
            _ => _
                .ReadVersionChoice(It.IsAny<string>(), It.IsAny<IReadOnlyCollection<SemanticVersion>>()))
        .Returns(new SemanticVersion());
    _config.DevelopStableMergeIgnoreList.FileName = new string[] { };

    var step = new ContinueReleaseOnMasterStep(
        gitClientStub.Object,
        _config,
        readInputStub.Object,
        _nextReleaseStepMock.Object,
        _consoleStub.Object,
        _msBuildCallAndCommitStub.Object
    );

    step.Execute(version, false);

    _nextReleaseStepMock.Verify();
  }

  [Test]
  public void Execute_WithOutErrorsButWithNoPush_DoesNotCallNextExecute ()
  {
    var version = new SemanticVersion();
    var gitClientStub = new Mock<IGitClient>();
    gitClientStub.Setup(_ => _.IsWorkingDirectoryClean()).Returns(true);
    gitClientStub.Setup(_ => _.IsOnBranch("release/")).Returns(true);
    gitClientStub.Setup(_ => _.GetCurrentBranchName()).Returns("release/v1.0.0");
    gitClientStub.Setup(_ => _.GetHash(It.IsAny<string>(), It.IsAny<string>())).Returns("branch");
    gitClientStub.Setup(_ => _.DoesTagExist("v1.0.0")).Returns(false);

    var readInputStub = new Mock<IInputReader>();
    readInputStub.Setup(
            _ => _
                .ReadVersionChoice(It.IsAny<string>(), It.IsAny<IReadOnlyCollection<SemanticVersion>>()))
        .Returns(new SemanticVersion());
    _config.DevelopStableMergeIgnoreList.FileName = new string[] { };

    var step = new ContinueReleaseOnMasterStep(
        gitClientStub.Object,
        _config,
        readInputStub.Object,
        _nextReleaseStepMock.Object,
        _consoleStub.Object,
        _msBuildCallAndCommitStub.Object
    );

    step.Execute(version, true);

    _nextReleaseStepMock.Verify(n => n.Execute(It.IsAny<SemanticVersion>()), Times.Never);
  }
}