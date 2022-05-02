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
using Moq;
using NUnit.Framework;
using ReleaseProcessAutomation.Git;
using ReleaseProcessAutomation.ReadInput;
using ReleaseProcessAutomation.SemanticVersioning;
using ReleaseProcessAutomation.Steps.PipelineSteps;

namespace ReleaseProcessAutomation.Tests.Steps.InitialBranching;

[TestFixture]
internal class BranchFromHotfixStepTests
{
  [SetUp]
  public void Setup ()
  {
    _gitClientStub = new Mock<IGitClient>();
    _inputReaderStub = new Mock<IInputReader>();
    _semanticVersionedGitRepoStub = new Mock<ISemanticVersionedGitRepository>();
    _releasePatchStepMock = new Mock<IReleasePatchStep>();
    _releaseAlphaBetaStepMock = new Mock<IReleaseAlphaBetaStep>();
  }

  private Mock<IGitClient> _gitClientStub;
  private Mock<IInputReader> _inputReaderStub;
  private Mock<ISemanticVersionedGitRepository> _semanticVersionedGitRepoStub;
  private Mock<IReleasePatchStep> _releasePatchStepMock;
  private Mock<IReleaseAlphaBetaStep> _releaseAlphaBetaStepMock;

  [Test]
  public void GetCurrentHotFixVersion_WithoutBranchAndWithStartReleasePhase_ThrowsException ()
  {
    _gitClientStub.Setup(_ => _.GetCurrentBranchName()).Returns("");

    var hotfixBranch = new BranchFromHotfixStep(
        _gitClientStub.Object,
        _inputReaderStub.Object,
        _semanticVersionedGitRepoStub.Object,
        _releasePatchStepMock.Object,
        _releaseAlphaBetaStepMock.Object);

    Assert.That(() => hotfixBranch.Execute("", true, false, false),
        Throws.InstanceOf<InvalidOperationException>()
            .With.Message.EqualTo("Could not find the current branch while trying to get next hotfix version."));
  }

  [Test]
  public void GetCurrentHotFixVersion_WithBranchAndWithStartReleasePhase_CallsPatchReleaseWithNextVersion()
  {
    _gitClientStub.Setup(_ => _.GetCurrentBranchName()).Returns("hotfix/v1.3.5");
    var version = new SemanticVersion
                  {
                      Major = 1,
                      Minor = 3,
                      Patch = 5
                  };

    _releasePatchStepMock.Setup(_ => _.Execute(version, "", true, false, false, false)).Verifiable();

    var hotfixBranch = new BranchFromHotfixStep(
        _gitClientStub.Object,
        _inputReaderStub.Object,
        _semanticVersionedGitRepoStub.Object,
        _releasePatchStepMock.Object,
        _releaseAlphaBetaStepMock.Object);
    
    Assert.That(() => hotfixBranch.Execute("", true, false, false),
        Throws.Nothing);

    _releasePatchStepMock.Verify();
    
  }

  [Test]
  public void Execute_CallsPreReleaseWithNextVersion()
  {

    var version = new SemanticVersion
                  {
                      Major = 1,
                      Minor = 3,
                      Patch = 5
                  };

    var nextVersion = version;
    nextVersion.Patch = 6;
    nextVersion.Pre = PreReleaseStage.alpha;
    nextVersion.PreReleaseCounter = 1;

    _inputReaderStub.Setup(_ => _.ReadVersionChoice("Please choose next Version:",It.IsAny<IReadOnlyCollection<SemanticVersion>>())).Returns(nextVersion);
    _gitClientStub.Setup(_ => _.GetCurrentBranchName()).Returns("hotfix/v1.3.5");
    _semanticVersionedGitRepoStub.Setup(_ => _.GetMostRecentHotfixVersion()).Returns(new SemanticVersion());


    _releaseAlphaBetaStepMock.Setup(_ => _.Execute(version, "", false, false)).Verifiable();

    var hotfixBranch = new BranchFromHotfixStep(
        _gitClientStub.Object,
        _inputReaderStub.Object,
        _semanticVersionedGitRepoStub.Object,
        _releasePatchStepMock.Object,
        _releaseAlphaBetaStepMock.Object);

    Assert.That(() => hotfixBranch.Execute("", false, false, false),
        Throws.Nothing);

    _releaseAlphaBetaStepMock.Verify();

  }
}
