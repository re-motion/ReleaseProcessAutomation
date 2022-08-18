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

namespace ReleaseProcessAutomation.UnitTests.Steps.InitialBranching;

[TestFixture]
internal class BranchFromReleaseStepTests
{
  private Mock<IGitClient> _gitClient;
  private Mock<IInputReader> _inputReader;
  private Mock<IReleaseRCStep> _releaseRCStep;
  private Mock<IReleaseWithRCStep> _releaseWithRCStep;

  [SetUp]
  public void Setup ()
  {
    _gitClient = new Mock<IGitClient>();
    _inputReader = new Mock<IInputReader>();
    _releaseRCStep = new Mock<IReleaseRCStep>();
    _releaseWithRCStep = new Mock<IReleaseWithRCStep>();
  }

  [Test]
  public void FindNextRC_WithSeveralExistingTags_ReturnsVersionWithNextRcVersionAndCallsRCStep ()
  {
    var version = new SemanticVersion
                  {
                      Major = 1,
                      Minor = 3,
                      Patch = 5,
                      Pre = PreReleaseStage.rc,
                      PreReleaseCounter = 6
                  };
    _gitClient.Setup(_ => _.GetCurrentBranchName()).Returns("release/v1.3.5-rc.3");
    _gitClient.Setup(_ => _.DoesTagExist("v1.3.5-rc.4")).Returns(true);
    _gitClient.Setup(_ => _.DoesTagExist("v1.3.5-rc.5")).Returns(true);
    _releaseRCStep.Setup(_ => _.Execute(version, "", false, false, "")).Verifiable();
    _inputReader.Setup(_ => _.ReadVersionChoice(It.IsAny<string>(), It.IsAny<IReadOnlyCollection<SemanticVersion>>())).Returns(version);

    var branchStep = new BranchFromReleaseStep(_gitClient.Object, _inputReader.Object, _releaseRCStep.Object, _releaseWithRCStep.Object);

    Assert.That(
        () => branchStep.Execute("", false, false),
        Throws.Nothing);

    _releaseRCStep.Verify();
  }

  [Test]
  public void Execute_WithNonRCVersion_CallsReleaseWithRCStep ()
  {
    var version = new SemanticVersion
                  {
                      Major = 1,
                      Minor = 3,
                      Patch = 5,
                      Pre = PreReleaseStage.rc,
                      PreReleaseCounter = 6
                  };
    _gitClient.Setup(_ => _.GetCurrentBranchName()).Returns("release/v1.3.5-rc.3");
    _gitClient.Setup(_ => _.DoesTagExist("v1.3.5-rc.4")).Returns(true);
    _gitClient.Setup(_ => _.DoesTagExist("v1.3.5-rc.5")).Returns(true);
    _releaseWithRCStep.Setup(_ => _.Execute(false, false, "")).Verifiable();
    _inputReader.Setup(_ => _.ReadVersionChoice(It.IsAny<string>(), It.IsAny<IReadOnlyCollection<SemanticVersion>>())).Returns(new SemanticVersion());

    var branchStep = new BranchFromReleaseStep(_gitClient.Object, _inputReader.Object, _releaseRCStep.Object, _releaseWithRCStep.Object);

    Assert.That(
        () => branchStep.Execute("", false, false),
        Throws.Nothing);

    _releaseWithRCStep.Verify();
  }
}