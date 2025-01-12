﻿using System;
using System.Collections.Generic;
using Moq;
using NUnit.Framework;
using Remotion.ReleaseProcessAutomation.Configuration.Data;
using Remotion.ReleaseProcessAutomation.Jira;
using Remotion.ReleaseProcessAutomation.Jira.ServiceFacadeImplementations;
using Remotion.ReleaseProcessAutomation.ReadInput;
using Remotion.ReleaseProcessAutomation.SemanticVersioning;
using Remotion.ReleaseProcessAutomation.Steps.SubSteps;
using Spectre.Console.Testing;

namespace Remotion.ReleaseProcessAutomation.UnitTests.Steps;

[TestFixture]
public class ReleaseVersionAndMoveIssuesSubStepTests
{
  private Mock<IJiraVersionReleaser> _jiraReleaserMock;
  private Mock<IJiraVersionCreator> _jiraVersionCreatorMock;
  private Mock<IInputReader> _inputReaderMock;
  private Mock<IJiraIssueService> _jiraIssueServiceMock;
  private TestConsole _console;
  private Configuration.Data.Config _config;

  [SetUp]
  public void Setup ()
  {
    _jiraVersionCreatorMock = new Mock<IJiraVersionCreator>();
    _jiraReleaserMock = new Mock<IJiraVersionReleaser>();
    _jiraVersionCreatorMock.Setup(_ => _.CreateNewVersionWithVersionNumber(It.IsAny<string>()));
    _jiraReleaserMock.Setup(_ => _.ReleaseVersion(It.IsAny<string>(), false));
    _jiraReleaserMock.Setup(
        _ => _.ReleaseVersionAndSquashUnreleased(It.IsAny<string>(), It.IsAny<string>()));
    _jiraIssueServiceMock = new Mock<IJiraIssueService>();

    _inputReaderMock = new Mock<IInputReader>(MockBehavior.Strict);

    _console = new TestConsole();
    _console.Width(int.MaxValue);
    _config = new Configuration.Data.Config();
    _config.Jira = new JiraConfig();
    _config.Jira.StringUseBearer = "true";
    _config.Jira.JiraURL = "https://www.JiraURL.com/url";
    _config.Jira.JiraProjectKey = "JiraProjectKey";
  }

  [Test]
  public void Execute_CreatesTwoVersionsCorrectlyAndDoesNotThrow ()
  {
    var currentVersion = new SemanticVersion();
    var nextVersion = new SemanticVersion { Patch = 1 };

    _jiraVersionCreatorMock.Setup(_ => _.CreateNewVersionWithVersionNumber(currentVersion.ToString()))
        .Returns("current").Verifiable();
    _jiraVersionCreatorMock.Setup(_ => _.CreateNewVersionWithVersionNumber(nextVersion.ToString()))
        .Returns("next").Verifiable();

    _jiraIssueServiceMock.Setup(_ => _.FindAllNonClosedIssues(It.IsAny<string>(), _config.Jira.JiraProjectKey)).Returns(Array.Empty<JiraToBeMovedIssue>());

    var releaseVersionAndMoveIssuesSubStep = new ReleaseVersionAndMoveIssuesSubStep(
        _console,
        _inputReaderMock.Object,
        _jiraIssueServiceMock.Object,
        _jiraVersionCreatorMock.Object,
        _jiraReleaserMock.Object);

    Assert.That(() => releaseVersionAndMoveIssuesSubStep.Execute(currentVersion, nextVersion, _config.Jira.JiraProjectKey, true), Throws.Nothing);

    Assert.That(_console.Output, Does.Contain("Releasing version '0.0.0' on JIRA."));

    _inputReaderMock.Verify();
    _jiraVersionCreatorMock.Verify();
  }

  [Test]
  public void Execute_WithoutSquashUnreleased_ReleasesWithoutSquash ()
  {
    var versionID = "curr";
    var nextID = "next";

    var currentVersion = new SemanticVersion();
    var nextVersion = new SemanticVersion { Patch = 1 };
    _jiraVersionCreatorMock.Setup(_ => _.CreateNewVersionWithVersionNumber(currentVersion.ToString()))
        .Returns(versionID);
    _jiraVersionCreatorMock.Setup(_ => _.CreateNewVersionWithVersionNumber(nextVersion.ToString())).Returns(nextID);
    _jiraIssueServiceMock.Setup(_ => _.FindAllNonClosedIssues(It.IsAny<string>(), _config.Jira.JiraProjectKey)).Returns(Array.Empty<JiraToBeMovedIssue>());

    var jiraSubStep = new ReleaseVersionAndMoveIssuesSubStep(
        _console,
        _inputReaderMock.Object,
        _jiraIssueServiceMock.Object,
        _jiraVersionCreatorMock.Object,
        _jiraReleaserMock.Object);

    Assert.That(() => jiraSubStep.Execute(currentVersion, nextVersion, _config.Jira.JiraProjectKey, false), Throws.Nothing);

    Assert.That(_console.Output, Does.Contain("Releasing version '0.0.0' on JIRA."));

    _inputReaderMock.Verify();
    _jiraReleaserMock.Verify(_ => _.ReleaseVersion(versionID, false), Times.Once);
    _jiraReleaserMock.Verify(
        _ => _.ReleaseVersionAndSquashUnreleased(versionID, nextID),
        Times.Never);
  }

  [Test]
  public void Execute_WithSquashUnreleased_ReleaseWithSquash ()
  {
    var versionID = "curr";
    var nextID = "next";

    var currentVersion = new SemanticVersion();
    var nextVersion = new SemanticVersion { Patch = 1 };
    _jiraVersionCreatorMock.Setup(_ => _.CreateNewVersionWithVersionNumber(currentVersion.ToString()))
        .Returns(versionID);
    _jiraVersionCreatorMock.Setup(_ => _.CreateNewVersionWithVersionNumber(nextVersion.ToString())).Returns(nextID);

    _jiraIssueServiceMock.Setup(_ => _.FindAllNonClosedIssues(It.IsAny<string>(), _config.Jira.JiraProjectKey)).Returns(Array.Empty<JiraToBeMovedIssue>());

    var jiraSubStep = new ReleaseVersionAndMoveIssuesSubStep(
        _console,
        _inputReaderMock.Object,
        _jiraIssueServiceMock.Object,
        _jiraVersionCreatorMock.Object,
        _jiraReleaserMock.Object);

    Assert.That(() => jiraSubStep.Execute(currentVersion, nextVersion, _config.Jira.JiraProjectKey, true), Throws.Nothing);

    Assert.That(_console.Output, Does.Not.Contain("These are some of the issues that will be moved by releasing the version on jira:"));
    Assert.That(_console.Output, Does.Contain("Releasing version '0.0.0' on JIRA."));

    _jiraReleaserMock.Verify(_ => _.ReleaseVersion(versionID, false), Times.Never);
    _jiraReleaserMock.Verify(_ => _.ReleaseVersionAndSquashUnreleased(versionID, nextID), Times.Once);
  }

  [Test]
  public void Execute_MovesTheClosedIssues ()
  {
    var currentVersion = new SemanticVersion { Pre = PreReleaseStage.alpha, PreReleaseCounter = 1 };
    var nextVersion = new SemanticVersion { Pre = PreReleaseStage.alpha, PreReleaseCounter = 2 };
    var currentProjectVersion = new JiraProjectVersion { id = "curr" };
    var nextProjectVersion = new JiraProjectVersion { id = "next" };
    var fullProjectVersion = new JiraProjectVersion { id = "full" };

    _jiraVersionCreatorMock.Setup(_ => _.FindAllVersionsStartingWithVersionNumber("0.0.0")).Returns(
        new[]
        {
            new JiraProjectVersion()
        });
    _jiraVersionCreatorMock.Setup(_ => _.FindVersionWithVersionNumber(currentVersion.ToString()))
        .Returns(currentProjectVersion);
    _jiraVersionCreatorMock.Setup(_ => _.FindVersionWithVersionNumber(new SemanticVersion().ToString()))
        .Returns(fullProjectVersion);
    _jiraVersionCreatorMock.Setup(_ => _.FindVersionWithVersionNumber(nextVersion.ToString())).Returns(nextProjectVersion);
    _jiraIssueServiceMock.Setup(_ => _.FindAllNonClosedIssues(It.IsAny<string>(), _config.Jira.JiraProjectKey)).Returns(new[]
                                                                                           {
                                                                                               new JiraToBeMovedIssue()
                                                                                           });

    _jiraIssueServiceMock.Setup(
            _ => _.FindIssuesWithOnlyExactFixVersion(
                It.IsAny<IEnumerable<JiraProjectVersion>>(),
                fullProjectVersion,
                _config.Jira.JiraProjectKey))
        .Returns(new[] { new JiraToBeMovedIssue { Key = "key", Fields = new JiraNonClosedIssueFields { Summary = "summary" } } });
    _inputReaderMock.Setup(_ => _.ReadConfirmation(true)).Returns(true);

    var releaseVersionAndMoveIssuesSubStep = new ReleaseVersionAndMoveIssuesSubStep(
        _console,
        _inputReaderMock.Object,
        _jiraIssueServiceMock.Object,
        _jiraVersionCreatorMock.Object,
        _jiraReleaserMock.Object);

    Assert.That(() => releaseVersionAndMoveIssuesSubStep.Execute(currentVersion, nextVersion, _config.Jira.JiraProjectKey, false, true), Throws.Nothing);

    Assert.That(_console.Output, Does.Contain("* key - summary"));

    _jiraIssueServiceMock.Verify(
        _ => _.AddFixVersionToIssues(It.IsAny<IEnumerable<JiraToBeMovedIssue>>(), currentProjectVersion.id),
        Times.Once());
  }

  [Test]
  public void Execute_WithoutCurrentVersionInJira_PrintsErrorOutput ()
  {
    JiraProjectVersion nullVersion = null;
    var currentVersion = new SemanticVersion();
    var nextVersion = new SemanticVersion { Pre = PreReleaseStage.alpha, PreReleaseCounter = 1 };
    var currentProjectVersion = new JiraProjectVersion { id = "curr" };
    var nextProjectVersion = new JiraProjectVersion { id = "next" };

    _jiraVersionCreatorMock.Setup(_ => _.FindAllVersionsStartingWithVersionNumber("0.0.0")).Returns(
        new[]
        {
            new JiraProjectVersion()
        });
    _jiraVersionCreatorMock.Setup(_ => _.FindVersionWithVersionNumber(currentVersion.ToString()))
        .Returns(nullVersion);
    _jiraVersionCreatorMock.Setup(_ => _.FindVersionWithVersionNumber(nextVersion.ToString())).Returns(nextProjectVersion);
    _jiraIssueServiceMock.Setup(
            _ => _.FindIssuesWithOnlyExactFixVersion(
                It.IsAny<IEnumerable<JiraProjectVersion>>(),
                currentProjectVersion,
                _config.Jira.JiraProjectKey))
        .Returns(new[] { new JiraToBeMovedIssue { Key = "key", Fields = new JiraNonClosedIssueFields { Summary = "summary" } } });
    _inputReaderMock.Setup(_ => _.ReadConfirmation(true)).Returns(true);

    var releaseVersionAndMoveIssuesSubStep = new ReleaseVersionAndMoveIssuesSubStep(
        _console,
        _inputReaderMock.Object,
        _jiraIssueServiceMock.Object,
        _jiraVersionCreatorMock.Object,
        _jiraReleaserMock.Object);

    releaseVersionAndMoveIssuesSubStep.Execute(currentVersion, nextVersion, _config.Jira.JiraProjectKey, false, true);
    Assert.That(_console.Output, Does.Contain($"Version '{currentVersion}' does not exist in JIRA."));

    _jiraIssueServiceMock.Verify(_ => _.FindAllClosedIssues(It.IsAny<string>(), _config.Jira.JiraProjectKey), Times.Never);
    _jiraIssueServiceMock.Verify(
        _ => _.MoveIssuesToVersion(It.IsAny<IEnumerable<JiraToBeMovedIssue>>(), It.IsAny<string>(), It.IsAny<string>()),
        Times.Never);
  }

  [Test]
  public void Execute_WithoutNextVersionInJira_PrintsErrorOutput ()
  {
    JiraProjectVersion nullVersion = null;
    var currentVersion = new SemanticVersion { Pre = PreReleaseStage.alpha, PreReleaseCounter = 1 };
    var nextVersion = new SemanticVersion { Pre = PreReleaseStage.alpha, PreReleaseCounter = 2 };
    var fullVersion = new SemanticVersion();
    var currentProjectVersion = new JiraProjectVersion { id = "curr" };

    _jiraVersionCreatorMock.Setup(_ => _.FindAllVersionsStartingWithVersionNumber("0.0.0")).Returns(
        new[]
        {
            new JiraProjectVersion()
        });
    _jiraVersionCreatorMock.Setup(_ => _.FindVersionWithVersionNumber(currentVersion.ToString()))
        .Returns(currentProjectVersion);
    _jiraVersionCreatorMock.Setup(_ => _.FindVersionWithVersionNumber(fullVersion.ToString())).Returns(nullVersion);
    _jiraIssueServiceMock.Setup(
            _ => _.FindIssuesWithOnlyExactFixVersion(
                It.IsAny<IEnumerable<JiraProjectVersion>>(),
                currentProjectVersion,
                _config.Jira.JiraProjectKey))
        .Returns(new[] { new JiraToBeMovedIssue { Key = "key", Fields = new JiraNonClosedIssueFields { Summary = "summary" } } });
    _inputReaderMock.Setup(_ => _.ReadConfirmation(true)).Returns(true);

    var releaseVersionAndMoveIssuesSubStep = new ReleaseVersionAndMoveIssuesSubStep(
        _console,
        _inputReaderMock.Object,
        _jiraIssueServiceMock.Object,
        _jiraVersionCreatorMock.Object,
        _jiraReleaserMock.Object);

    releaseVersionAndMoveIssuesSubStep.Execute(currentVersion, nextVersion, _config.Jira.JiraProjectKey, false, true);
    Assert.That(_console.Output, Does.Contain($"Version '{fullVersion}' does not exist in JIRA."));

    _jiraIssueServiceMock.Verify(_ => _.FindAllClosedIssues(It.IsAny<string>(), _config.Jira.JiraProjectKey), Times.Never);
    _jiraIssueServiceMock.Verify(
        _ => _.MoveIssuesToVersion(It.IsAny<IEnumerable<JiraToBeMovedIssue>>(), It.IsAny<string>(), It.IsAny<string>()),
        Times.Never);
  }

  [Test]
  public void Execute_WithoutIssues_DoesNotAskForConfirmation ()
  {
    var versionID = "curr";
    var nextID = "next";

    var currentVersion = new SemanticVersion();
    var nextVersion = new SemanticVersion { Patch = 1 };
    _jiraVersionCreatorMock.Setup(_ => _.CreateNewVersionWithVersionNumber(currentVersion.ToString()))
        .Returns(versionID);
    _jiraVersionCreatorMock.Setup(_ => _.CreateNewVersionWithVersionNumber(nextVersion.ToString())).Returns(nextID);

    _jiraIssueServiceMock.Setup(_ => _.FindAllNonClosedIssues(It.IsAny<string>(), _config.Jira.JiraProjectKey)).Returns(new List<JiraToBeMovedIssue>());
    var jiraSubStep = new ReleaseVersionAndMoveIssuesSubStep(
        _console,
        _inputReaderMock.Object,
        _jiraIssueServiceMock.Object,
        _jiraVersionCreatorMock.Object,
        _jiraReleaserMock.Object);

    Assert.That(() => jiraSubStep.Execute(currentVersion, nextVersion, _config.Jira.JiraProjectKey, true), Throws.Nothing);
    Assert.That(_console.Output, Does.Contain("Releasing version '0.0.0' on JIRA."));
    Assert.That(_console.Output, Does.Not.Contain("Moving open issues to '0.0.1'"));
    Assert.That(_console.Output, Does.Not.Contain("These are some of the issues that will be moved by releasing the version on jira:"));

    _jiraReleaserMock.Verify(_ => _.ReleaseVersion(versionID, false), Times.Never);
    _jiraReleaserMock.Verify(_ => _.ReleaseVersionAndSquashUnreleased(versionID, nextID), Times.Once);

    _inputReaderMock.Verify(_ => _.ReadConfirmation(It.IsAny<bool>()), Times.Never);
  }

  [Test]
  public void Execute_WithSameVersionID_DoesNotAskForConfirmation ()
  {
    var versionID = "next";
    var nextID = "next";

    var currentVersion = new SemanticVersion();
    var nextVersion = new SemanticVersion { Patch = 1 };
    _jiraVersionCreatorMock.Setup(_ => _.CreateNewVersionWithVersionNumber(currentVersion.ToString()))
        .Returns(versionID);
    _jiraVersionCreatorMock.Setup(_ => _.CreateNewVersionWithVersionNumber(nextVersion.ToString())).Returns(nextID);

    var jiraSubStep = new ReleaseVersionAndMoveIssuesSubStep(
        _console,
        _inputReaderMock.Object,
        _jiraIssueServiceMock.Object,
        _jiraVersionCreatorMock.Object,
        _jiraReleaserMock.Object);

    Assert.That(() => jiraSubStep.Execute(currentVersion, nextVersion, _config.Jira.JiraProjectKey, true), Throws.Nothing);
    Assert.That(_console.Output, Does.Contain("Releasing version '0.0.0' on JIRA."));
    Assert.That(_console.Output, Does.Not.Contain("Moving open issues to '0.0.1'"));
    Assert.That(_console.Output, Does.Not.Contain("These are some of the issues that will be moved by releasing the version on jira:"));

    _jiraReleaserMock.Verify(_ => _.ReleaseVersion(versionID, false), Times.Never);
    _jiraReleaserMock.Verify(_ => _.ReleaseVersionAndSquashUnreleased(versionID, nextID), Times.Once);

    _inputReaderMock.Verify(_ => _.ReadConfirmation(It.IsAny<bool>()), Times.Never);
  }

  [Test]
  public void Execute_WithIssues_AsksForConfirmationAndMovesIssues ()
  {
    var versionID = "curr";
    var nextID = "next";

    var currentVersion = new SemanticVersion();
    var nextVersion = new SemanticVersion { Patch = 1 };
    _jiraVersionCreatorMock.Setup(_ => _.CreateNewVersionWithVersionNumber(currentVersion.ToString()))
        .Returns(versionID);
    _jiraVersionCreatorMock.Setup(_ => _.CreateNewVersionWithVersionNumber(nextVersion.ToString())).Returns(nextID);

    _inputReaderMock.Setup(_ => _.ReadConfirmation(It.IsAny<bool>())).Returns(true).Verifiable();

    var jiraIssue1 = new JiraToBeMovedIssue
      {
        Fields = new JiraNonClosedIssueFields
          {
            Summary = "firstSummary"
          },
        Key = "firstKey"
      };

    var jiraIssue2 = new JiraToBeMovedIssue
      {
        Fields = new JiraNonClosedIssueFields
          {
            Summary = "secondSummary"
          },
        Key = "secondKey"
      };

    var jiraIssue3 = new JiraToBeMovedIssue
      {
        Fields = new JiraNonClosedIssueFields
          {
            Summary = "thirdSummary"
          },
        Key = "thirdKey"
      };

    var jiraIssue4 = new JiraToBeMovedIssue
      {
        Fields = new JiraNonClosedIssueFields
          {
            Summary = "fourthSummary"
          },
        Key = "fourthKey"
      };

    var jiraIssue5 = new JiraToBeMovedIssue
      {
        Fields = new JiraNonClosedIssueFields
          {
            Summary = "Incredibly long summary that definitely can not be fully displayed here"
          },
        Key = "VeryLongKey"
      };

    var jiraIssue6 = new JiraToBeMovedIssue
      {
        Fields = new JiraNonClosedIssueFields
          {
            Summary = "Issue summary that should not be visible"
          },
        Key = "InvisibleKey"
      };

    var jiraIssueArray = new[] { jiraIssue1, jiraIssue2, jiraIssue3, jiraIssue4, jiraIssue5, jiraIssue6 };
    _jiraIssueServiceMock.Setup(_ => _.FindAllNonClosedIssues(It.IsAny<string>(), _config.Jira.JiraProjectKey)).Returns(jiraIssueArray);
    var jiraSubStep = new ReleaseVersionAndMoveIssuesSubStep(
        _console,
        _inputReaderMock.Object,
        _jiraIssueServiceMock.Object,
        _jiraVersionCreatorMock.Object,
        _jiraReleaserMock.Object);

    _console.Profile.Width = 75;

    Assert.That(() => jiraSubStep.Execute(currentVersion, nextVersion, _config.Jira.JiraProjectKey, true), Throws.Nothing);
    Assert.That(_console.Output, Does.Contain("Releasing version '0.0.0' on JIRA."));
    Assert.That(_console.Output, Does.Contain("Moving open issues to '0.0.1'"));
    Assert.That(_console.Output, Does.Contain("These are some of the issues that will be moved by releasing version \n'0.0.0' on JIRA:"));
    Assert.That(_console.Output, Does.Contain("* firstKey - firstSummary"));
    Assert.That(_console.Output, Does.Contain("* secondKey - secondSummary"));
    Assert.That(_console.Output, Does.Contain("* thirdKey - thirdSummary"));
    Assert.That(_console.Output, Does.Contain("* fourthKey - fourthSummary"));
    Assert.That(_console.Output, Does.Contain("* VeryLongKey - Incredibly long summary that definitely can not be fully..."));
    Assert.That(_console.Output, Does.Contain("..."));
    Assert.That(_console.Output, Does.Not.Contain("InvisibleKey"));


    _jiraReleaserMock.Verify(_ => _.ReleaseVersion(versionID, false), Times.Never);
    _jiraReleaserMock.Verify(_ => _.ReleaseVersionAndSquashUnreleased(versionID, nextID), Times.Once);

    _jiraIssueServiceMock.Verify(_ => _.MoveIssuesToVersion(jiraIssueArray, versionID, nextID), Times.Once);
    _inputReaderMock.Verify();
  }
}