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
using System.IO;
using Moq;
using NUnit.Framework;
using ReleaseProcessAutomation.Configuration;
using ReleaseProcessAutomation.Configuration.Data;
using ReleaseProcessAutomation.Extensions;
using ReleaseProcessAutomation.Git;
using ReleaseProcessAutomation.ReadInput;
using ReleaseProcessAutomation.Scripting;
using ReleaseProcessAutomation.SemanticVersioning;
using ReleaseProcessAutomation.Steps.PipelineSteps;
using Spectre.Console;

namespace ReleaseProcessAutomation.Tests.Steps.Releases
{
  internal class ReleaseAlphaBetaStepTests
  {
    [SetUp]
    public void Setup()
    {
      _gitClientStub = new Mock<IGitClient>();
      _inputReaderStub = new Mock<IInputReader>();
      _msBuildInvokerMock = new Mock<IMSBuildCallAndCommit>();
      _continueAlphaBetaMock = new Mock<IContinueAlphaBetaStep>();
      _consoleMock = new Mock<IAnsiConsole>();

      var path = Path.Join(TestContext.CurrentContext.TestDirectory, c_configFileName);
      _config = new ConfigReader().LoadConfig(path);

    }

    private Mock<IAnsiConsole> _consoleMock;
    private Mock<IGitClient> _gitClientStub;
    private Mock<IInputReader> _inputReaderStub;
    private Configuration.Data.Config _config;
    private Mock<IMSBuildCallAndCommit> _msBuildInvokerMock;
    private Mock<IContinueAlphaBetaStep> _continueAlphaBetaMock;
    private const string c_configFileName = "ReleaseProcessScript.Test.Config";

    [Test]
    public void Execute_OnMasterWithoutErrors_CallsNextStep ()
    {
      _gitClientStub.Setup(_ => _.IsWorkingDirectoryClean()).Returns(true);
      _gitClientStub.Setup(_ => _.IsCommitHash("")).Returns(true);
      _gitClientStub.Setup(_ => _.IsOnBranch("develop")).Returns(true);
      _gitClientStub.Setup(_ => _.GetCurrentBranchName()).Returns("develop");
      var nextPossibleVersions = new SemanticVersion().GetNextPossibleVersionsHotfix();
      _inputReaderStub.Setup(_ => _.ReadVersionChoice(It.IsAny<string>(), nextPossibleVersions)).Returns(new SemanticVersion());
      _continueAlphaBetaMock.Setup(_ => _.Execute(new SemanticVersion(), "develop", "develop", false)).Verifiable();

      var alphaBetaStep = new ReleaseAlphaBetaStep(
          _gitClientStub.Object,
          _config,
          _inputReaderStub.Object,
          _msBuildInvokerMock.Object,
          _continueAlphaBetaMock.Object,
          _consoleMock.Object);

      Assert.That(
          () => alphaBetaStep.Execute(new SemanticVersion(), "", false, false),
          Throws.Nothing);
      _continueAlphaBetaMock.Verify();
    }
    [Test]
    public void Execute_OnMasterWithoutErrorsButWithPauseForCommit_CallsInvokeMBuildAndCommitButNotNextStep()
    {
      _gitClientStub.Setup(_ => _.IsWorkingDirectoryClean()).Returns(true);
      _gitClientStub.Setup(_ => _.IsCommitHash("")).Returns(true);
      _gitClientStub.Setup(_ => _.IsOnBranch("develop")).Returns(true);
      _gitClientStub.Setup(_ => _.GetCurrentBranchName()).Returns("develop");
      var nextPossibleVersions = new SemanticVersion().GetNextPossibleVersionsHotfix();
      _inputReaderStub.Setup(_ => _.ReadVersionChoice(It.IsAny<string>(), nextPossibleVersions)).Returns(new SemanticVersion());
      _msBuildInvokerMock.Setup(_ => _.CallMSBuildStepsAndCommit(It.IsAny<MSBuildMode>(), It.IsAny<SemanticVersion>())).Verifiable();


      var alphaBetaStep = new ReleaseAlphaBetaStep(
          _gitClientStub.Object,
          _config,
          _inputReaderStub.Object,
          _msBuildInvokerMock.Object,
          _continueAlphaBetaMock.Object,
          _consoleMock.Object);



      Assert.That(
          () => alphaBetaStep.Execute(new SemanticVersion(), "", true, false),
          Throws.Nothing);
      
      _msBuildInvokerMock.Verify();
      _continueAlphaBetaMock.Verify(_=>_.Execute(It.IsAny<SemanticVersion>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()),Times.Never);
    }
  }


}
