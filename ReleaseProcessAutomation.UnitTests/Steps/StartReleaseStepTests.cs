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
using Moq;
using NUnit.Framework;
using ReleaseProcessAutomation.Git;
using ReleaseProcessAutomation.Steps;
using ReleaseProcessAutomation.Steps.PipelineSteps;
using Spectre.Console.Testing;

namespace ReleaseProcessAutomation.UnitTests.Steps;

[TestFixture]
internal class StartReleaseStepTests
{
  private TestConsole _console;
  private Mock<IBranchFromDevelopStep> _developBranchMock;
  private Mock<IBranchFromReleaseStep> _releaseBranchStub;
  private Mock<IBranchFromMasterStep> _masterBranchStub;
  private Mock<IBranchFromHotfixStep> _hotfixBranchStub;

  [SetUp]
  public void Setup ()
  {
    _developBranchMock = new Mock<IBranchFromDevelopStep>();
    _releaseBranchStub = new Mock<IBranchFromReleaseStep>();
    _masterBranchStub = new Mock<IBranchFromMasterStep>();
    _hotfixBranchStub = new Mock<IBranchFromHotfixStep>();
    _console = new TestConsole();
  }

  [Test]
  public void Execute_WithFaultyCommitHash_throwsException ()
  {
    var gitClientStub = new Mock<IGitClient>();
    var commitHash = "1029384781827834äöäöä";
    gitClientStub.Setup(_ => _.IsCommitHash(commitHash));
    var startReleaseStep = new StartReleaseStep(
        gitClientStub.Object,
        _console,
        _developBranchMock.Object,
        _releaseBranchStub.Object,
        _hotfixBranchStub.Object,
        _masterBranchStub.Object);

    Assert.That(
        () => startReleaseStep.Execute(commitHash, false, false),
        Throws.InstanceOf<ArgumentException>()
            .With.Message.EqualTo("The given commit hash was not found in the repository"));
  }

  [Test]
  public void Execute_OnNonReleaseBranch_OutputsMessageToConsole ()
  {
    var gitClientStub = new Mock<IGitClient>();
    gitClientStub.Setup(_ => _.IsOnBranch("release/")).Returns(false);
    gitClientStub.Setup(_ => _.IsOnBranch("develop")).Returns(true);

    var startReleaseStep = new StartReleaseStep(
        gitClientStub.Object,
        _console,
        _developBranchMock.Object,
        _releaseBranchStub.Object,
        _hotfixBranchStub.Object,
        _masterBranchStub.Object);

    startReleaseStep.Execute(null);

    Assert.That(
        _console.Output.ReplaceLineEndings(""),
        Is.EqualTo(
            "As you are not on a release branch, you won't be able to release a release candidate version.To create a release branch, use the command [green]'New-Release-Branch'[/]"));
  }

  [Test]
  public void Execute_OnNonReleaseBranchWithStartReleasePhase_OutputsNothingToConsole ()
  {
    var gitClientStub = new Mock<IGitClient>();
    gitClientStub.Setup(_ => _.IsOnBranch("release/")).Returns(false);
    gitClientStub.Setup(_ => _.IsOnBranch("develop")).Returns(true);

    var startReleaseStep = new StartReleaseStep(
        gitClientStub.Object,
        _console,
        _developBranchMock.Object,
        _releaseBranchStub.Object,
        _hotfixBranchStub.Object,
        _masterBranchStub.Object);

    startReleaseStep.Execute(null, startReleasePhase: true);

    Assert.That(_console.Output.ReplaceLineEndings(""), Is.EqualTo(string.Empty));
  }

  [Test]
  public void Execute_OnReleaseBranch_OutputsNoMessageToConsole ()
  {
    var gitClientStub = new Mock<IGitClient>();
    gitClientStub.Setup(_ => _.IsOnBranch("release/")).Returns(true);

    var startReleaseStep = new StartReleaseStep(
        gitClientStub.Object,
        _console,
        _developBranchMock.Object,
        _releaseBranchStub.Object,
        _hotfixBranchStub.Object,
        _masterBranchStub.Object);

    startReleaseStep.Execute(null);

    Assert.That(_console.Output.ReplaceLineEndings(""), Is.EqualTo(string.Empty));
  }

  [Test]
  public void Execute_OnBranchDevelop_StartsReleaseFromDevelop ()
  {
    var gitClientStub = new Mock<IGitClient>();
    var commitHash = "";

    gitClientStub.Setup(_ => _.IsCommitHash(commitHash)).Returns(true);
    gitClientStub.Setup(_ => _.IsOnBranch("develop")).Returns(true);
    _developBranchMock.Setup(_ => _.Execute(commitHash, false, false, false)).Verifiable();

    var startReleaseStep = new StartReleaseStep(
        gitClientStub.Object,
        _console,
        _developBranchMock.Object,
        _releaseBranchStub.Object,
        _hotfixBranchStub.Object,
        _masterBranchStub.Object);

    Assert.That(
        () => startReleaseStep.Execute(commitHash, false, false),
        Throws.Nothing);
    _developBranchMock.Verify();
  }
}