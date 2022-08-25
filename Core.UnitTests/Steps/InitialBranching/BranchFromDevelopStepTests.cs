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
using ReleaseProcessAutomation.Extensions;
using ReleaseProcessAutomation.Git;
using ReleaseProcessAutomation.ReadInput;
using ReleaseProcessAutomation.SemanticVersioning;
using ReleaseProcessAutomation.Steps.PipelineSteps;

namespace ReleaseProcessAutomation.UnitTests.Steps.InitialBranching;

[TestFixture]
internal class BranchFromDevelopStepTests
{
  private Mock<IReleaseAlphaBetaStep> _releaseAlphaBetaMock;
  private Mock<IReleaseOnMasterStep> _releaseOnMasterMock;

  [SetUp]
  public void Setup ()
  {
    _releaseAlphaBetaMock = new Mock<IReleaseAlphaBetaStep>();
    _releaseAlphaBetaMock.Setup(_ => _.Execute(It.IsAny<SemanticVersion>(), It.IsAny<string>(), false, false)).Verifiable();

    _releaseOnMasterMock = new Mock<IReleaseOnMasterStep>();
    _releaseOnMasterMock.Setup(_ => _.Execute(It.IsAny<SemanticVersion>(), "", false, false, false)).Verifiable();
  }

  [Test]
  public void Execute_WithoutCurrentVersion_ReadsInput ()
  {
    var semanticVersionedGitRepoStub = new Mock<ISemanticVersionedGitRepository>();
    // semVerMock.Setup(_ => _.DoesCurrentVersionExist("HEAD", "")).Returns(false);
    var version = new SemanticVersion();

    var readInputMock = new Mock<IInputReader>();
    readInputMock.Setup(
        _ => _
            .ReadSemanticVersion(It.IsAny<string>())).Returns(version).Verifiable();

    var step = new BranchFromDevelopStep(
        readInputMock.Object,
        _releaseOnMasterMock.Object,
        _releaseAlphaBetaMock.Object,
        semanticVersionedGitRepoStub.Object
    );

    step.Execute("", false, false, false);

    readInputMock.Verify();
  }

  [Test]
  public void Execute_WithMasterNewer_ReadsPossibleNextVersionsFromMaster ()
  {
    var oldVersion = new SemanticVersion
                     {
                         Major = 1,
                         Minor = 2,
                         Patch = 3
                     };
    var newVersion = new SemanticVersion
                     {
                         Major = 2
                     };
    var possibleVersions = newVersion.GetNextPossibleVersionsDevelop();

    var semanticVersionedGitRepoStub = new Mock<ISemanticVersionedGitRepository>();
    var sequence = new MockSequence();
    semanticVersionedGitRepoStub.InSequence(sequence).Setup(_ => _.TryGetCurrentVersion(out It.Ref<SemanticVersion>.IsAny, "HEAD", "")).Returns(true);
    semanticVersionedGitRepoStub.InSequence(sequence).Setup(_ => _.TryGetCurrentVersion(out oldVersion, "HEAD", ""));
    semanticVersionedGitRepoStub.InSequence(sequence).Setup(_ => _.TryGetCurrentVersion(out newVersion, "master", "")).Returns(true);
    semanticVersionedGitRepoStub.InSequence(sequence).Setup(_ => _.TryGetCurrentVersion(out It.Ref<SemanticVersion>.IsAny, "master", ""))
        .Returns(true);
    var version = new SemanticVersion();

    var readInputMock = new Mock<IInputReader>();
    readInputMock.Setup(
        _ => _
            .ReadVersionChoice(It.IsAny<string>(), possibleVersions)).Returns(version).Verifiable();

    var step = new BranchFromDevelopStep(
        readInputMock.Object,
        _releaseOnMasterMock.Object,
        _releaseAlphaBetaMock.Object,
        semanticVersionedGitRepoStub.Object
    );

    step.Execute("", false, false, false);

    readInputMock.Verify();
  }

  [Test]
  public void Execute_WithDevelopNewer_ReadsPossibleNextVersionsFromDevelop ()
  {
    var oldVersion = new SemanticVersion
                     {
                         Major = 1,
                         Minor = 2,
                         Patch = 3
                     };
    var newVersion = new SemanticVersion
                     {
                         Major = 2
                     };
    var possibleVersions = newVersion.GetNextPossibleVersionsDevelop();

    var semanticVersionedGitRepoStub = new Mock<ISemanticVersionedGitRepository>();
    var sequence = new MockSequence();
    semanticVersionedGitRepoStub.InSequence(sequence).Setup(_ => _.TryGetCurrentVersion(out It.Ref<SemanticVersion>.IsAny, "HEAD", "")).Returns(true);
    semanticVersionedGitRepoStub.InSequence(sequence).Setup(_ => _.TryGetCurrentVersion(out newVersion, "HEAD", ""));
    semanticVersionedGitRepoStub.InSequence(sequence).Setup(_ => _.TryGetCurrentVersion(out oldVersion, "master", ""));
    semanticVersionedGitRepoStub.InSequence(sequence).Setup(_ => _.TryGetCurrentVersion(out It.Ref<SemanticVersion>.IsAny, "master", ""))
        .Returns(true);
    var version = new SemanticVersion();
    var gitClientMock = new Mock<IGitClient>();

    var readInputMock = new Mock<IInputReader>();
    readInputMock.Setup(
        _ => _
            .ReadVersionChoice(It.IsAny<string>(), possibleVersions)).Returns(version).Verifiable();

    var step = new BranchFromDevelopStep(
        readInputMock.Object,
        _releaseOnMasterMock.Object,
        _releaseAlphaBetaMock.Object,
        semanticVersionedGitRepoStub.Object
    );

    step.Execute("", false, false, false);

    readInputMock.Verify();
  }

  [Test]
  public void Execute_WithMasterNewerButDoesNotExist_ReadsPossibleNextVersionsFromDevelop ()
  {
    var oldVersion = new SemanticVersion
                     {
                         Major = 1,
                         Minor = 2,
                         Patch = 3
                     };
    var newVersion = new SemanticVersion
                     {
                         Major = 2
                     };
    var possibleVersions = oldVersion.GetNextPossibleVersionsDevelop();

    var semanticVersionedGitRepoStub = new Mock<ISemanticVersionedGitRepository>();
    var sequence = new MockSequence();

    semanticVersionedGitRepoStub.InSequence(sequence).Setup(_ => _.TryGetCurrentVersion(out It.Ref<SemanticVersion>.IsAny, "HEAD", "")).Returns(true);
    semanticVersionedGitRepoStub.InSequence(sequence).Setup(_ => _.TryGetCurrentVersion(out oldVersion, "HEAD", ""));
    semanticVersionedGitRepoStub.InSequence(sequence).Setup(_ => _.TryGetCurrentVersion(out newVersion, "master", ""));
    semanticVersionedGitRepoStub.InSequence(sequence).Setup(_ => _.TryGetCurrentVersion(out It.Ref<SemanticVersion>.IsAny, "master", ""))
        .Returns(false);
    var version = new SemanticVersion();

    var readInputMock = new Mock<IInputReader>();
    readInputMock.Setup(
        _ => _
            .ReadVersionChoice(It.IsAny<string>(), possibleVersions)).Returns(version).Verifiable();

    var step = new BranchFromDevelopStep(
        readInputMock.Object,
        _releaseOnMasterMock.Object,
        _releaseAlphaBetaMock.Object,
        semanticVersionedGitRepoStub.Object
    );

    step.Execute("", false, false, false);

    readInputMock.Verify();
  }

  [Test]
  public void Execute_WithPreRelease_CallsPreReleaseStep ()
  {
    var semanticVersionedGitRepoStub = new Mock<ISemanticVersionedGitRepository>();
    // semVerMock.Setup(_ => _.DoesCurrentVersionExist("HEAD", "")).Returns(false);
    var version = new SemanticVersion
                  {
                      Major = 1,
                      Pre = PreReleaseStage.alpha,
                      PreReleaseCounter = 1
                  };
    var gitClientMock = new Mock<IGitClient>();

    var readInputStub = new Mock<IInputReader>();
    readInputStub.Setup(
        _ => _
            .ReadSemanticVersion(It.IsAny<string>())).Returns(version).Verifiable();

    var step = new BranchFromDevelopStep(
        readInputStub.Object,
        _releaseOnMasterMock.Object,
        _releaseAlphaBetaMock.Object,
        semanticVersionedGitRepoStub.Object
    );

    step.Execute("", false, false, false);

    _releaseAlphaBetaMock.Verify();
  }

  [Test]
  public void Execute_WithMajorVersion_CallsReleaseOnMasterStep ()
  {
    var semanticVersionedGitRepoStub = new Mock<ISemanticVersionedGitRepository>();
    // semVerMock.Setup(_ => _.DoesCurrentVersionExist("HEAD", "")).Returns(false);
    var version = new SemanticVersion
                  {
                      Major = 1
                  };

    var readInputStub = new Mock<IInputReader>();
    readInputStub.Setup(
        _ => _
            .ReadSemanticVersion(It.IsAny<string>())).Returns(version).Verifiable();

    var step = new BranchFromDevelopStep(
        readInputStub.Object,
        _releaseOnMasterMock.Object,
        _releaseAlphaBetaMock.Object,
        semanticVersionedGitRepoStub.Object
    );

    step.Execute("", false, false, false);

    _releaseOnMasterMock.Verify();
  }
}