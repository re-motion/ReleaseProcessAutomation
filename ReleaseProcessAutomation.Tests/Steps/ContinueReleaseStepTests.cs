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
using ReleaseProcessAutomation.Steps;
using ReleaseProcessAutomation.Steps.PipelineSteps;

namespace ReleaseProcessAutomation.Tests.Steps;

[TestFixture]
internal class ContinueReleaseStepTests
{
  [SetUp]
  public void setup ()
  {
    _continueFromPreRelease = new Mock<IBranchFromPreReleaseForContinueVersionStep>();
    _continueFromPreRelease.Setup(_ => _.Execute(It.IsAny<SemanticVersion>(), It.IsAny<string>(), false)).Verifiable();
    _continueFromRelease = new Mock<IBranchFromReleaseForContinueVersionStep>();
    _continueFromRelease.Setup(_ => _.Execute(It.IsAny<SemanticVersion>(), It.IsAny<string>(), false)).Verifiable();
  }

  private Mock<IBranchFromPreReleaseForContinueVersionStep> _continueFromPreRelease;
  private Mock<IBranchFromReleaseForContinueVersionStep> _continueFromRelease;

  [Test]
  public void Execute_OnBranchReleaseWithAncestorEqualToDevelop_ShouldCallNonPreRelease ()
  {
    var gitClientMock = new Mock<IGitClient>();
    gitClientMock.Setup(_ => _.GetCurrentBranchName()).Returns("release/v1.0.0");
    gitClientMock.Setup(_ => _.IsOnBranch("release/")).Returns(true);

    var ancestorMock = new Mock<IAncestorFinder>();
    ancestorMock.Setup(_ => _.GetAncestor("develop", "hotfix/v")).Returns("develop");

    var step = new ContinueReleaseStep(
        gitClientMock.Object,
        _continueFromPreRelease.Object,
        _continueFromRelease.Object);
    step.Execute("", false);

    _continueFromRelease.Verify();
  }
}