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
using ReleaseProcessScript.New.Configuration;
using ReleaseProcessScript.New.Git;
using ReleaseProcessScript.New.ReadInput;
using ReleaseProcessScript.New.SemanticVersioning;
using ReleaseProcessScript.New.Steps.PipelineSteps;
using Spectre.Console;

namespace ReleaseProcessScript.New.Tests.Steps.Continues;

internal class ContinueAlphaBetaStepTests
{
  private const string c_configFileName = "ReleaseProcessScript.Test.Config";
  private Configuration.Data.Config _config;

  private Mock<IAnsiConsole> _consoleStub;
  private Mock<IGitClient> _gitClientMock;
  private Mock<IInputReader> _inputReaderStub;
  private Mock<IPushPreReleaseStep> _pushPreReleaseMock;
  private Mock<IAncestorFinder> _ancestorMock;

  [SetUp]
  public void Setup ()
  { _gitClientMock = new Mock<IGitClient>();
    _inputReaderStub = new Mock<IInputReader>();
    _pushPreReleaseMock = new Mock<IPushPreReleaseStep>();
    _ancestorMock = new Mock<IAncestorFinder>();
    _consoleStub = new Mock<IAnsiConsole>();
    
    var path = Path.Join(Environment.CurrentDirectory, c_configFileName);
    _config = new ConfigReader().LoadConfig(path);
  }

  [Test]
  public void Execute_WithoutErrors_CallsNextStep ()
  {
    _gitClientMock.Setup(_ => _.IsWorkingDirectoryClean()).Returns(true);
    _gitClientMock.Setup(_ => _.IsOnBranch("prerelease/")).Returns(true);
    _gitClientMock.Setup(_ => _.GetCurrentBranchName()).Returns("prerelease/v0.0.0");
    _pushPreReleaseMock.Setup(_ => _.Execute("prerelease/v0.0.0", "", "v0.0.0")).Verifiable();

    var continueAlphaBetaStep = new ContinueAlphaBetaStep(
        _gitClientMock.Object,
        _config,
        _inputReaderStub.Object,
        _ancestorMock.Object,
        _pushPreReleaseMock.Object,
        _consoleStub.Object);

    Assert.That(
        () => continueAlphaBetaStep.Execute(new SemanticVersion(), "ancestor","", false),
        Throws.Nothing);
    _pushPreReleaseMock.Verify();
  }

  [Test]
  public void Execute_WithoutErrorsButWithNoPush_DoesNotCallNextStepButCreatesTag ()
  {
    _gitClientMock.Setup(_ => _.IsWorkingDirectoryClean()).Returns(true);
    _gitClientMock.Setup(_ => _.GetCurrentBranchName()).Returns("prerelease/v0.0.0");
    _gitClientMock.Setup(_ => _.ResolveMergeConflicts()).Verifiable();

    var continueAlphaBetaStep = new ContinueAlphaBetaStep(
        _gitClientMock.Object,
        _config,
        _inputReaderStub.Object,
        _ancestorMock.Object,
        _pushPreReleaseMock.Object,
        _consoleStub.Object);

    Assert.That(
        () => continueAlphaBetaStep.Execute(new SemanticVersion(), "ancestor", "", true),
        Throws.Nothing);

    _gitClientMock.Verify();
    _pushPreReleaseMock.Verify(_ => _.Execute(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
  }

  [Test]
  public void Execute_AncestorEmpty_CallsGetAncestor ()
  {
    _gitClientMock.Setup(_ => _.IsWorkingDirectoryClean()).Returns(true);
    _gitClientMock.Setup(_ => _.IsOnBranch("prerelease/")).Returns(true);
    _gitClientMock.Setup(_ => _.GetCurrentBranchName()).Returns("prerelease/v0.0.0");
    _ancestorMock.Setup(_ => _.GetAncestor(It.IsAny<string[]>())).Verifiable();

    var continueAlphaBetaStep = new ContinueAlphaBetaStep(
        _gitClientMock.Object,
        _config,
        _inputReaderStub.Object,
        _ancestorMock.Object,
        _pushPreReleaseMock.Object,
        _consoleStub.Object);

    Assert.That(
        () => continueAlphaBetaStep.Execute(new SemanticVersion(), "","", false),
        Throws.Nothing);

    _ancestorMock.Verify();
  }

  [Test]
  public void Execute_AncestorNotEmpty_DoesNotCallGetAncestor ()
  {
    _gitClientMock.Setup(_ => _.IsWorkingDirectoryClean()).Returns(true);
    _gitClientMock.Setup(_ => _.IsOnBranch("prerelease/")).Returns(true);
    _gitClientMock.Setup(_ => _.GetCurrentBranchName()).Returns("prerelease/v0.0.0");

    var continueAlphaBetaStep = new ContinueAlphaBetaStep(
        _gitClientMock.Object,
        _config,
        _inputReaderStub.Object,
        _ancestorMock.Object,
        _pushPreReleaseMock.Object,
        _consoleStub.Object);

    Assert.That(
        () => continueAlphaBetaStep.Execute(new SemanticVersion(), "ancestor","", false),
        Throws.Nothing);

    _ancestorMock.Verify(_ => _.GetAncestor(It.IsAny<string[]>()), Times.Never);
  }
}