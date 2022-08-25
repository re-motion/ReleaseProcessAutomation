using System;
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
using Spectre.Console.Testing;

namespace ReleaseProcessAutomation.UnitTests.Steps.InitialBranching;

[TestFixture]
public class BranchFromReleaseForContinueVersionStepTests
{
  [SetUp]
  public void Setup ()
  {
    _gitClientStub = new Mock<IGitClient>();
    _inputReaderMock = new Mock<IInputReader>();
    _ancestorStub = new Mock<IAncestorFinder>();
    _msBuildInvokerMock = new Mock<IMSBuildCallAndCommit>();
    _continueReleasePatchMock = new Mock<IContinueReleasePatchStep>();
    _continueReleaseOnMasterStepMock = new Mock<IContinueReleaseOnMasterStep>();

    _consoleStub = new Mock<IAnsiConsole>();

    var path = Path.Join(TestContext.CurrentContext.TestDirectory, c_configFileName);
    _config = new ConfigReader().LoadConfig(path);
  }

  private Mock<IAnsiConsole> _consoleStub;
  private Mock<IGitClient> _gitClientStub;
  private Mock<IInputReader> _inputReaderMock;
  private Configuration.Data.Config _config;
  private Mock<IAncestorFinder> _ancestorStub;
  private Mock<IMSBuildCallAndCommit> _msBuildInvokerMock;
  private Mock<IContinueReleasePatchStep> _continueReleasePatchMock;
  private Mock<IContinueReleaseOnMasterStep> _continueReleaseOnMasterStepMock;
  private const string c_configFileName = "ReleaseProcessScript.Test.Config";

  [Test]
  public void Execute_WithHotfixAncestor_CallsContinueReleasePatch ()
  {
    var nextVersion = new SemanticVersion();

    var branch = new BranchFromReleaseForContinueVersionStep(
        _gitClientStub.Object,
        _config,
        _inputReaderMock.Object,
        _ancestorStub.Object,
        _continueReleaseOnMasterStepMock.Object,
        _continueReleasePatchMock.Object,
        new TestConsole());

    branch.Execute(nextVersion, "hotfix/v1.2.0", false);

    _continueReleasePatchMock.Verify(_ => _.Execute(nextVersion, false, false), Times.Once);
  }

  [Test]
  public void Execute_WithDevelopAncestor_CallsContinueReleaseOnMaster ()
  {
    var nextVersion = new SemanticVersion();

    var branch = new BranchFromReleaseForContinueVersionStep(
        _gitClientStub.Object,
        _config,
        _inputReaderMock.Object,
        _ancestorStub.Object,
        _continueReleaseOnMasterStepMock.Object,
        _continueReleasePatchMock.Object,
        new TestConsole());

    branch.Execute(nextVersion, "develop", false);

    _continueReleaseOnMasterStepMock.Verify(_ => _.Execute(nextVersion, false), Times.Once);
  }

  [Test]
  public void Execute_WithInvalidAncestor_ThrowsException ()
  {
    var nextVersion = new SemanticVersion();

    var branch = new BranchFromReleaseForContinueVersionStep(
        _gitClientStub.Object,
        _config,
        _inputReaderMock.Object,
        _ancestorStub.Object,
        _continueReleaseOnMasterStepMock.Object,
        _continueReleasePatchMock.Object,
        new TestConsole());
    
    var ancestor = "notAnAncestor";
    Assert.That(() => branch.Execute(nextVersion, ancestor, false), Throws.InstanceOf<InvalidOperationException>()
        .With.Message.EqualTo($"Ancestor has to be either 'develop' or a 'hotfix/v*.*.*' branch but was '{ancestor}'."));

  }
}