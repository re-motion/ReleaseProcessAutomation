﻿using System;
using System.Linq;
using Moq;
using NUnit.Framework;
using Remotion.ReleaseProcessAutomation.Configuration.Data;
using Remotion.ReleaseProcessAutomation.IntegrationTests.Jira;
using Remotion.ReleaseProcessAutomation.Jira;
using Remotion.ReleaseProcessAutomation.Jira.ServiceFacadeImplementations;
using Remotion.ReleaseProcessAutomation.Jira.Utility;
using Remotion.ReleaseProcessAutomation.ReadInput;
using Remotion.ReleaseProcessAutomation.SemanticVersioning;
using Remotion.ReleaseProcessAutomation.Steps.SubSteps;
using Spectre.Console.Testing;

namespace Remotion.ReleaseProcessAutomation.IntegrationTests.Steps;

[Explicit]
[TestFixture]
public class ReleaseVersionAndMoveIssuesSubStepTests
{
  private const string c_jiraUrl = "https://re-motion.atlassian.net/";
  private const string c_jiraProjectKey = "SRCBLDTEST";

  private Mock<IJiraRestClientProvider> _restClientProviderMock;
  private Mock<IInputReader> _inputReaderMock;
  private TestConsole _console;
  private JiraRestClient _restClient;
  private JiraProjectVersionService _service;
  private JiraProjectVersionRepairer _repairer;
  private JiraProjectVersionFinder _versionFinder;
  private JiraIssueService _issueService;
  private JiraVersionCreator _jiraVersionCreator;
  private JiraVersionReleaser _jiraVersionReleaser;
  private Config _config;
  private SemanticVersionParser _parser = new SemanticVersionParser();

  [SetUp]
  public void SetUp ()
  {
    _inputReaderMock = new Mock<IInputReader>();
    _console = new TestConsole();
    var testCredentials = JiraTestUtility.GetLocallySavedCredentials();

    _restClient = JiraRestClient.CreateWithBasicAuthentication(c_jiraUrl, testCredentials);

    _restClientProviderMock = new Mock<IJiraRestClientProvider>();
    _restClientProviderMock.Setup(_ => _.GetJiraRestClient()).Returns(_restClient);

    _versionFinder = new JiraProjectVersionFinder(_restClientProviderMock.Object);
    _issueService = new JiraIssueService(_restClientProviderMock.Object);
    _service = new JiraProjectVersionService(_restClientProviderMock.Object, _issueService, _versionFinder);
    _repairer = new JiraProjectVersionRepairer(_service, _versionFinder);
    _config = new Config
              {
                  Jira = new JiraConfig
                         {
                             JiraProjectKey = "SRCBLDTEST"
                         }
              };
    _jiraVersionCreator = new JiraVersionCreator(_config, _versionFinder, _service);
    _jiraVersionReleaser = new JiraVersionReleaser(_config, _repairer, _service);
  }

  [Test]
  public void Execute_WithExtraVersions_DoesNotMoveIssuesFromOtherVersions ()
  {
    JiraTestUtility.DeleteVersionsIfExistent(c_jiraProjectKey, _restClient, "4.1.0", "4.1.1", "4.1.2", "4.2.0");

    _inputReaderMock.Setup(_ => _.ReadConfirmation(It.IsAny<bool>())).Returns(true);
    _service = new JiraProjectVersionService(_restClientProviderMock.Object, _issueService, _versionFinder);

    // Create versions
    _service.CreateVersion(c_jiraProjectKey, "4.1.0", DateTime.Today.AddDays(1));
    _service.CreateSubsequentVersion(c_jiraProjectKey, "4\\.1\\..*", 3, DayOfWeek.Monday);
    _service.CreateSubsequentVersion(c_jiraProjectKey, "4\\.1\\..*", 3, DayOfWeek.Tuesday);
    _service.CreateVersion(c_jiraProjectKey, "4.2.0", DateTime.Today.AddDays(7));

    // Get latest unreleased version
    var versions = _versionFinder.FindUnreleasedVersions(c_jiraProjectKey, "4.1.").ToList();

    var versionToRelease = versions.First(v => v.name == "4.1.0");

    var additionalVersion = _versionFinder.FindUnreleasedVersions(c_jiraProjectKey, "4.2.").Single(v => v.name == "4.2.0");

    // Add issues to versionToRelease
    var myTestIssue = JiraTestUtility.AddTestIssueToVersion("My Test", false, c_jiraProjectKey, _restClient, versionToRelease);

    var myClosedIssue = JiraTestUtility.AddTestIssueToVersion("My closed Test", true, c_jiraProjectKey, _restClient, versionToRelease);
    var myMultipleFixVersionTest = JiraTestUtility.AddTestIssueToVersion(
        "My multiple fixVersion Test",
        false,
        c_jiraProjectKey,
        _restClient,
        versionToRelease,
        additionalVersion);

    // Release version
    var parser = new SemanticVersionParser();
    var jiraSubStep = new ReleaseVersionAndMoveIssuesSubStep(
        _console,
        _inputReaderMock.Object,
        _issueService,
        _jiraVersionCreator,
        _jiraVersionReleaser);

    jiraSubStep.Execute(parser.ParseVersion("4.1.0"), parser.ParseVersion("4.1.1"), _config.Jira.JiraProjectKey);

    // Get latest unreleased version again
    versions = _versionFinder.FindUnreleasedVersions(c_jiraProjectKey, "4.1.").ToList();
    Assert.That(versions.Count(), Is.EqualTo(2));

    var versionThatFollowed = versions.First();
    Assert.That(versionThatFollowed.name, Is.EqualTo("4.1.1"));

    // Check whether versionThatFollowed has all the non-closed issues from versionToRelease
    var issues = _issueService.FindAllNonClosedIssues(versionThatFollowed.id, _config.Jira.JiraProjectKey);
    Assert.That(issues.Count(), Is.EqualTo(2));

    // Check whether the additionalVersion still has its issue
    var additionalVersionIssues = _issueService.FindAllNonClosedIssues(additionalVersion.id, _config.Jira.JiraProjectKey);
    Assert.That(additionalVersionIssues.Count(), Is.EqualTo(1));

    JiraTestUtility.DeleteVersionsIfExistent(c_jiraProjectKey, _restClient, "4.1.0", "4.1.1", "4.1.2", "4.2.0");

    JiraTestUtility.DeleteIssues(_restClient, myTestIssue.ID, myClosedIssue.ID, myMultipleFixVersionTest.ID);
  }

  [Test]
  public void Execute_WithConfirmationToMove_MovesOpenIssues ()
  {
    JiraTestUtility.DeleteVersionsIfExistent(c_jiraProjectKey, _restClient, "4.1.0", "4.1.1");

    _inputReaderMock.Setup(_ => _.ReadConfirmation(It.IsAny<bool>())).Returns(true);
    _service = new JiraProjectVersionService(_restClientProviderMock.Object, _issueService, _versionFinder);
    _service.CreateVersion(c_jiraProjectKey, "4.1.0", DateTime.Now);
    _service.CreateVersion(c_jiraProjectKey, "4.1.1", DateTime.Today.AddDays(3));

    var findUnreleasedVersions = _versionFinder.FindUnreleasedVersions(c_jiraProjectKey, "4.1");
    var initialVersion = findUnreleasedVersions.First(v => v.name == "4.1.0");
    var afterwardsVersion = findUnreleasedVersions.Last(v => v.name == "4.1.1");
    var testIssue1 = JiraTestUtility.AddTestIssueToVersion("Test open Issue 1", false, c_jiraProjectKey, _restClient, initialVersion);
    var testIssue2 = JiraTestUtility.AddTestIssueToVersion("Test open Issue 2", false, c_jiraProjectKey, _restClient, initialVersion);
    var testIssue3 = JiraTestUtility.AddTestIssueToVersion("Test closed Issue 3", true, c_jiraProjectKey, _restClient, initialVersion);
    var testIssue4 = JiraTestUtility.AddTestIssueToVersion("Test closed Issue 4", true, c_jiraProjectKey, _restClient, initialVersion);

    var jiraSubStep = new ReleaseVersionAndMoveIssuesSubStep(
        _console,
        _inputReaderMock.Object,
        _issueService,
        _jiraVersionCreator,
        _jiraVersionReleaser);

    jiraSubStep.Execute(_parser.ParseVersion("4.1.0"), _parser.ParseVersion("4.1.1"), _config.Jira.JiraProjectKey);

    Assert.That(_console.Output, Does.Contain("Test open Issue 1"));
    Assert.That(_console.Output, Does.Contain("Test open Issue 2"));
    Assert.That(_console.Output, Does.Not.Contain("Test closed Issue 3"));
    Assert.That(_console.Output, Does.Not.Contain("Test closed Issue 4"));

    var afterwardsVersionIssues = _issueService.FindAllNonClosedIssues(afterwardsVersion.id, _config.Jira.JiraProjectKey);
    Assert.That(afterwardsVersionIssues.Count(), Is.EqualTo(2));

    var initialVersionIssues = _issueService.FindAllClosedIssues(initialVersion.id, _config.Jira.JiraProjectKey);
    Assert.That(initialVersionIssues.Count(), Is.EqualTo(2));

    JiraTestUtility.DeleteVersionsIfExistent(c_jiraProjectKey, _restClient, "4.1.0", "4.1.1");
    JiraTestUtility.DeleteIssues(_restClient, testIssue1.ID, testIssue2.ID, testIssue3.ID, testIssue4.ID);
  }

  [Test]
  public void Execute_WithoutConfirmationToContinue_DoesNotMoveOpenIssues ()
  {
    JiraTestUtility.DeleteVersionsIfExistent(c_jiraProjectKey, _restClient, "4.1.0", "4.1.1");

    _inputReaderMock.Setup(_ => _.ReadConfirmation(It.IsAny<bool>())).Returns(false);
    _service = new JiraProjectVersionService(_restClientProviderMock.Object, _issueService, _versionFinder);

    _service.CreateVersion(c_jiraProjectKey, "4.1.0", DateTime.Now);
    _service.CreateVersion(c_jiraProjectKey, "4.1.1", DateTime.Today.AddDays(3));

    var findUnreleasedVersions = _versionFinder.FindUnreleasedVersions(c_jiraProjectKey, "4.1");
    var initialVersion = findUnreleasedVersions.First(v => v.name == "4.1.0");
    var afterwardsVersion = findUnreleasedVersions.Last(v => v.name == "4.1.1");
    var testIssue1 = JiraTestUtility.AddTestIssueToVersion("Test open Issue 1", false, c_jiraProjectKey, _restClient, initialVersion);
    var testIssue2 = JiraTestUtility.AddTestIssueToVersion("Test open Issue 2", false, c_jiraProjectKey, _restClient, initialVersion);
    var testIssue3 = JiraTestUtility.AddTestIssueToVersion("Test closed Issue 3", true, c_jiraProjectKey, _restClient, initialVersion);
    var testIssue4 = JiraTestUtility.AddTestIssueToVersion("Test closed Issue 4", true, c_jiraProjectKey, _restClient, initialVersion);

    var jiraSubStep = new ReleaseVersionAndMoveIssuesSubStep(
        _console,
        _inputReaderMock.Object,
        _issueService,
        _jiraVersionCreator,
        _jiraVersionReleaser);

    jiraSubStep.Execute(_parser.ParseVersion("4.1.0"), _parser.ParseVersion("4.1.1"), _config.Jira.JiraProjectKey);

    Assert.That(_console.Output, Does.Contain("Test open Issue 1"));
    Assert.That(_console.Output, Does.Contain("Test open Issue 2"));
    Assert.That(_console.Output, Does.Not.Contain("Test closed Issue 3"));
    Assert.That(_console.Output, Does.Not.Contain("Test closed Issue 4"));

    var closedIssues = _issueService.FindAllClosedIssues(initialVersion.id, _config.Jira.JiraProjectKey);
    Assert.That(closedIssues.Count(), Is.EqualTo(2));
    var openIssues = _issueService.FindAllNonClosedIssues(initialVersion.id, _config.Jira.JiraProjectKey);
    Assert.That(openIssues.Count(), Is.EqualTo(2));
    var afterwardsOpenIssues = _issueService.FindAllNonClosedIssues(afterwardsVersion.id, _config.Jira.JiraProjectKey);
    Assert.That(afterwardsOpenIssues.Count(), Is.EqualTo(0));

    JiraTestUtility.DeleteVersionsIfExistent(c_jiraProjectKey, _restClient, "4.1.0", "4.1.1");
    JiraTestUtility.DeleteIssues(_restClient, testIssue1.ID, testIssue2.ID, testIssue3.ID, testIssue4.ID);
  }
}