using System;
using Moq;
using NUnit.Framework;
using ReleaseProcessAutomation.Configuration.Data;
using ReleaseProcessAutomation.Jira;
using ReleaseProcessAutomation.SemanticVersioning;
using Spectre.Console.Testing;

namespace ReleaseProcessAutomation.UnitTests.Jira;

[TestFixture]
public class JiraFunctionalityTests
{
  private Mock<IJiraVersionReleaser> _jiraReleaserMock;
  private Mock<IJiraVersionCreator> _jiraCreatorMock;
  private TestConsole _console;
  private Configuration.Data.Config _config;

  [SetUp]
  public void Setup ()
  {
    _jiraCreatorMock = new Mock<IJiraVersionCreator>();
    _jiraReleaserMock = new Mock<IJiraVersionReleaser>();
    _jiraCreatorMock.Setup(_ => _.CreateNewVersionWithVersionNumber(It.IsAny<string>()));
    _jiraReleaserMock.Setup(_ => _.ReleaseVersion(It.IsAny<string>(), It.IsAny<string>(), false));
    _jiraReleaserMock.Setup(
        _ => _.ReleaseVersionAndSquashUnreleased( It.IsAny<string>(), It.IsAny<string>()));

    _console = new TestConsole();
    _config = new Configuration.Data.Config();
    _config.Jira = new JiraConfig();
    _config.Jira.StringUseNTLM = "true";
    _config.Jira.JiraURL = "https://www.JiraURL.com/url";
    _config.Jira.JiraProjectKey = "JiraProjectKey";
  }

  [Test]
  public void CreateAndReleaseJiraVersion_CreatesTwoVersionsCorrectly_ThrowsNothing ()
  {
    var currentVersion = new SemanticVersion();
    var nextVersion = new SemanticVersion { Patch = 1 };
    var jiraFunctionality = new JiraFunctionality(
        _console,
            _jiraCreatorMock.Object,
            _jiraReleaserMock.Object);


    _jiraCreatorMock.Setup(_ => _.CreateNewVersionWithVersionNumber(currentVersion.ToString()))
        .Returns("current").Verifiable();
    _jiraCreatorMock.Setup(_ => _.CreateNewVersionWithVersionNumber(nextVersion.ToString()))
        .Returns("next").Verifiable();

    Assert.That(() => jiraFunctionality.CreateAndReleaseJiraVersion(currentVersion, nextVersion), Throws.Nothing);

    _jiraCreatorMock.Verify();
  }

  [Test]
  public void CreateAndReleaseJiraVersion_WithoutSquashing_ReleaseWithoutSquash ()
  {
    var versionID = "curr";
    var nextID = "next";

    var currentVersion = new SemanticVersion();
    var nextVersion = new SemanticVersion { Patch = 1 };
    _jiraCreatorMock.Setup(_ => _.CreateNewVersionWithVersionNumber(currentVersion.ToString()))
        .Returns(versionID);
    _jiraCreatorMock.Setup(_ => _.CreateNewVersionWithVersionNumber(nextVersion.ToString())).Returns(nextID);

    var jiraFunctionality = new JiraFunctionality(
        _console,
        _jiraCreatorMock.Object,
        _jiraReleaserMock.Object);

    Assert.That(() => jiraFunctionality.CreateAndReleaseJiraVersion(currentVersion, nextVersion), Throws.Nothing);

    _jiraReleaserMock.Verify(_ => _.ReleaseVersion(versionID, nextID, false), Times.Once);
    _jiraReleaserMock.Verify(
        _ => _.ReleaseVersionAndSquashUnreleased(versionID, nextID),
        Times.Never);
  }

  [Test]
  public void CreateAndReleaseJiraVersion_WithoutSquashing_ReleaseWithSquash ()
  {
    var versionID = "curr";
    var nextID = "next";

    var currentVersion = new SemanticVersion();
    var nextVersion = new SemanticVersion { Patch = 1 };
    _jiraCreatorMock.Setup(_ => _.CreateNewVersionWithVersionNumber(currentVersion.ToString()))
        .Returns(versionID);
    _jiraCreatorMock.Setup(_ => _.CreateNewVersionWithVersionNumber(nextVersion.ToString())).Returns(nextID);

    var jiraFunctionality = new JiraFunctionality(
        _console,
            _jiraCreatorMock.Object,
            _jiraReleaserMock.Object);


    Assert.That(() => jiraFunctionality.CreateAndReleaseJiraVersion(currentVersion, nextVersion, true), Throws.Nothing);

    _jiraReleaserMock.Verify(_ => _.ReleaseVersion(versionID, nextID, false), Times.Never);
    _jiraReleaserMock.Verify(_ => _.ReleaseVersionAndSquashUnreleased(versionID, nextID), Times.Once);
  }
}