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
using ReleaseProcessAutomation.SemanticVersioning;
using ReleaseProcessAutomation.Steps.PipelineSteps;

namespace ReleaseProcessAutomation.UnitTests.Steps.InitialBranching;

[TestFixture]
internal class BranchFromMasterStepTests
{
  private Mock<IGitClient> _gitClientStub;
  private Mock<ISemanticVersionedGitRepository> _semanticVersionedGitRepoStub;
  private Mock<IReleasePatchStep> _releasePatchStepMock;

  [SetUp]
  public void Setup ()
  {
    _gitClientStub = new Mock<IGitClient>();
    _semanticVersionedGitRepoStub = new Mock<ISemanticVersionedGitRepository>();
    _releasePatchStepMock = new Mock<IReleasePatchStep>();
  }

  [Test]
  public void Execute_WithSeveralTags_ReturnsFirstVersionAfterTagAndCallsReleasePatchStepForThisVersion ()
  {
    var currVersion = new SemanticVersion
                      {
                          Major = 1,
                          Minor = 3,
                          Patch = 3
                      };

    var version = new SemanticVersion
                  {
                      Major = 1,
                      Minor = 3,
                      Patch = 6
                  };
    _semanticVersionedGitRepoStub.Setup(_ => _.TryGetCurrentVersion(out currVersion, "master", ""));
    _gitClientStub.Setup(_ => _.DoesTagExist("v1.3.4")).Returns(true);
    _gitClientStub.Setup(_ => _.DoesTagExist("v1.3.5")).Returns(true);
    _releasePatchStepMock.Setup(_ => _.Execute(version, "", false, false, false, true)).Verifiable();

    var branchStep = new BranchFromMasterStep(_gitClientStub.Object, _semanticVersionedGitRepoStub.Object, _releasePatchStepMock.Object);

    Assert.That(
        () => branchStep.Execute("", false, false, false),
        Throws.Nothing);
    _releasePatchStepMock.Verify();
  }
}