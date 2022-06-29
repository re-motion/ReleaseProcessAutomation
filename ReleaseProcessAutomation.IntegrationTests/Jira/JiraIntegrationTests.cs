using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Moq;
using NUnit.Framework;
using ReleaseProcessAutomation.Configuration;
using ReleaseProcessAutomation.Configuration.Data;
using ReleaseProcessAutomation.Jira;
using ReleaseProcessAutomation.Jira.ServiceFacadeImplementations;
using ReleaseProcessAutomation.Jira.Utility;
using ReleaseProcessAutomation.ReadInput;
using ReleaseProcessAutomation.SemanticVersioning;
using ReleaseProcessAutomation.Steps.SubSteps;
using Spectre.Console.Testing;

namespace ReleaseProcessAutomation.IntegrationTests.Jira;

[TestFixture]
[Explicit]
public class JiraIntegrationTests : IntegrationTestSetup
{
  private string _jiraUsername;
  private string _jiraPassword;
  private Config _config;
  private JiraRestClient _restClient;
  private Mock<IJiraRestClientProvider> _restClientProviderMock;
  private JiraProjectVersionFinder _versionFinder;
  private JiraIssueService _issueService;
  private JiraProjectVersionService _service;

  [SetUp]
  public void Setup ()
  {
    var testCredentials = JiraTestUtility.GetLocallySavedCredentials();
    _jiraUsername = testCredentials.Username;
    _jiraPassword = testCredentials.Password;

    var configPath = Path.Combine(Environment.CurrentDirectory, "Build", "Customizations", c_testConfigName);
    _config = new ConfigReader().LoadConfig(configPath);

    _restClient = JiraRestClient.CreateWithBasicAuthentication(_config.Jira.JiraURL, testCredentials);

    _restClientProviderMock = new Mock<IJiraRestClientProvider>();
    _restClientProviderMock.Setup(_ => _.GetJiraRestClient()).Returns(_restClient);

    _versionFinder = new JiraProjectVersionFinder(_restClientProviderMock.Object);
    _issueService = new JiraIssueService(_restClientProviderMock.Object);
    _service = new JiraProjectVersionService(_restClientProviderMock.Object, _issueService, _versionFinder);
  }

  [Test]
  public void TestJiraRunThrough_FromDevelop_MovesOnlyOpenJiraVersions ()
  {
    var prevVersion = "1.0.0";
    var nextVersion = "1.1.0";
    JiraTestUtility.DeleteVersionsIfExistent(_config.Jira.JiraProjectKey, _restClient, prevVersion, nextVersion);

    var newVersionID = JiraTestUtility.CreateVersion(_restClient, prevVersion, _config.Jira.JiraProjectKey);

    var openIssue = JiraTestUtility.AddTestIssueToVersion(
        "Some open issue",
        false,
        _config.Jira.JiraProjectKey,
        _restClient,
        new JiraProjectVersion() { name = prevVersion, id = newVersionID });

    var closedIssue = JiraTestUtility.AddTestIssueToVersion(
        "Some closed issue",
        true,
        _config.Jira.JiraProjectKey,
        _restClient,
        new JiraProjectVersion() { name = prevVersion, id = newVersionID });

    ExecuteGitCommand("commit -m feature --allow-empty");
    ExecuteGitCommand("checkout -b develop");

    //Get release version from user
    TestConsole.Input.PushTextWithEnter(prevVersion);
    //Get next release version from user for jira
    TestConsole.Input.PushTextWithEnter(nextVersion);

    TestConsole.Input.PushTextWithEnter(_jiraUsername);
    TestConsole.Input.PushTextWithEnter(_jiraPassword);

    TestConsole.Input.PushTextWithEnter("y");
    TestConsole.Input.PushTextWithEnter("y");
    TestConsole.Input.PushTextWithEnter("n");

    var act = JiraTestUtility.RunProgramWithoutWindowsCredentials(new[] { "Release-Version" });

    Assert.That(act, Is.EqualTo(0));

    var isPartOfNewVersion = JiraTestUtility.IsPartOfJiraVersions(_config.Jira.JiraProjectKey, nextVersion, _restClient, out var newVersion);
    Assert.That(isPartOfNewVersion, Is.True);
    Assert.That(newVersion.released, Is.False);

    openIssue = JiraTestUtility.GetIssue(openIssue.ID, _restClient);
    Assert.That(openIssue.fields.FixVersions.First().ID, Is.EqualTo(newVersion.id));

    var isPartOfOldVersion = JiraTestUtility.IsPartOfJiraVersions(_config.Jira.JiraProjectKey, prevVersion, _restClient, out var oldVersion);
    Assert.That(isPartOfOldVersion, Is.True);
    Assert.That(oldVersion.released, Is.True);

    closedIssue = JiraTestUtility.GetIssue(closedIssue.ID, _restClient);
    Assert.That(closedIssue.fields.FixVersions.First().ID, Is.EqualTo(oldVersion.id));

    JiraTestUtility.DeleteVersionsIfExistent(_config.Jira.JiraProjectKey, _restClient, prevVersion, nextVersion);
    JiraTestUtility.DeleteIssues(_restClient, openIssue.ID, closedIssue.ID);
  }

  [Test]
  public void TestJiraRunThrough_FromRelease_MovesOnlyOpenJiraVersions ()
  {
    var prevVersion = "1.3.5";
    var nextVersion = "1.3.6";
    JiraTestUtility.DeleteVersionsIfExistent(_config.Jira.JiraProjectKey, _restClient, prevVersion, nextVersion);

    var newVersionID = JiraTestUtility.CreateVersion(_restClient, prevVersion, _config.Jira.JiraProjectKey);

    var openIssue = JiraTestUtility.AddTestIssueToVersion(
        "Some open issue",
        false,
        _config.Jira.JiraProjectKey,
        _restClient,
        new JiraProjectVersion() { name = prevVersion, id = newVersionID });

    var closedIssue = JiraTestUtility.AddTestIssueToVersion(
        "Some closed issue",
        true,
        _config.Jira.JiraProjectKey,
        _restClient,
        new JiraProjectVersion() { name = prevVersion, id = newVersionID });

    ExecuteGitCommand("commit -m feature --allow-empty");
    ExecuteGitCommand("tag v1.0.0");

    ExecuteGitCommand("checkout -b support/v1.3");
    ExecuteGitCommand("checkout -b hotfix/v1.3.5");

    ExecuteGitCommand("commit -m feature2 --allow-empty");
    ExecuteGitCommand("commit -m feature3 --allow-empty");
    ExecuteGitCommand("commit -m feature4 --allow-empty");

    ExecuteGitCommand("checkout -b release/v1.3.5");

    //Get release version from user
    TestConsole.Input.PushTextWithEnter(prevVersion);
    //Get next release version from user for jira
    TestConsole.Input.PushTextWithEnter(nextVersion);

    TestConsole.Input.PushTextWithEnter(_jiraUsername);
    TestConsole.Input.PushTextWithEnter(_jiraPassword);

    TestConsole.Input.PushTextWithEnter("y");
    TestConsole.Input.PushTextWithEnter("y");
    TestConsole.Input.PushTextWithEnter("n");

    var act = JiraTestUtility.RunProgramWithoutWindowsCredentials(new[] { "Release-Version" });

    Assert.That(act, Is.EqualTo(0));

    var isPartOfNewVersion = JiraTestUtility.IsPartOfJiraVersions(_config.Jira.JiraProjectKey, nextVersion, _restClient, out var newVersion);
    Assert.That(isPartOfNewVersion, Is.True);
    Assert.That(newVersion.released, Is.False);

    openIssue = JiraTestUtility.GetIssue(openIssue.ID, _restClient);
    Assert.That(openIssue.fields.FixVersions.First().ID, Is.EqualTo(newVersion.id));

    var isPartOfOldVersion = JiraTestUtility.IsPartOfJiraVersions(_config.Jira.JiraProjectKey, prevVersion, _restClient, out var oldVersion);
    Assert.That(isPartOfOldVersion, Is.True);
    Assert.That(oldVersion.released, Is.True);

    closedIssue = JiraTestUtility.GetIssue(closedIssue.ID, _restClient);
    Assert.That(closedIssue.fields.FixVersions.First().ID, Is.EqualTo(oldVersion.id));

    JiraTestUtility.DeleteVersionsIfExistent(_config.Jira.JiraProjectKey, _restClient, prevVersion, nextVersion);
    JiraTestUtility.DeleteIssues(_restClient, openIssue.ID, closedIssue.ID);
  }

  [Test]
  public void TestJiraRunThrough_WithManyDifferentVersionsWithCloseNames_ReleasesCorrectJiraVersions ()
  {
    JiraTestUtility.DeleteVersionsIfExistent(_config.Jira.JiraProjectKey, _restClient, "1.0.0", "1.1.0", "1.0.1", "1.0.2");

    var newVersionID = JiraTestUtility.CreateVersion(_restClient, "1.0.0", _config.Jira.JiraProjectKey);

    JiraTestUtility.CreateVersion(_restClient, "1.0.1", _config.Jira.JiraProjectKey);
    JiraTestUtility.CreateVersion(_restClient, "1.0.2", _config.Jira.JiraProjectKey);

    var openIssue = JiraTestUtility.AddTestIssueToVersion(
        "Some open issue",
        false,
        _config.Jira.JiraProjectKey,
        _restClient,
        new JiraProjectVersion() { name = "1.0.0", id = newVersionID });

    var closedIssue = JiraTestUtility.AddTestIssueToVersion(
        "Some closed issue",
        true,
        _config.Jira.JiraProjectKey,
        _restClient,
        new JiraProjectVersion() { name = "1.0.0", id = newVersionID });

    ExecuteGitCommand("commit -m feature --allow-empty");
    ExecuteGitCommand("checkout -b develop");

    //Get release version from user
    TestConsole.Input.PushTextWithEnter("1.0.0");
    //Get next release version from user for jira
    TestConsole.Input.PushTextWithEnter("1.1.0");

    TestConsole.Input.PushTextWithEnter(_jiraUsername);
    TestConsole.Input.PushTextWithEnter(_jiraPassword);

    TestConsole.Input.PushTextWithEnter("y");
    TestConsole.Input.PushTextWithEnter("y");
    TestConsole.Input.PushTextWithEnter("n");

    var act = JiraTestUtility.RunProgramWithoutWindowsCredentials(new[] { "Release-Version" });

    Assert.That(act, Is.EqualTo(0));

    var is110JiraVersion = JiraTestUtility.IsPartOfJiraVersions(_config.Jira.JiraProjectKey, "1.1.0", _restClient, out var version110);
    Assert.That(is110JiraVersion, Is.True);
    Assert.That(version110.released, Is.False);

    openIssue = JiraTestUtility.GetIssue(openIssue.ID, _restClient);
    Assert.That(openIssue.fields.FixVersions.First().ID, Is.EqualTo(version110.id));

    var is100JiraVersion = JiraTestUtility.IsPartOfJiraVersions(_config.Jira.JiraProjectKey, "1.0.0", _restClient, out var version100);
    Assert.That(is100JiraVersion, Is.True);
    Assert.That(version100.released, Is.True);

    closedIssue = JiraTestUtility.GetIssue(closedIssue.ID, _restClient);
    Assert.That(closedIssue.fields.FixVersions.First().ID, Is.EqualTo(version100.id));

    JiraTestUtility.DeleteVersionsIfExistent(_config.Jira.JiraProjectKey, _restClient, "1.0.0", "1.1.0", "1.0.1", "1.0.2");
    JiraTestUtility.DeleteIssues(_restClient, openIssue.ID, closedIssue.ID);
  }

  [Test]
  public void ReleaseVersionAndMoveIssues_MovesProperIssues ()
  {
    const string currentVersionName = "1.3.0-alpha.2";
    const string nextVersionName = "1.3.0-alpha.3";
    const string previousFullVersion = "1.3.0";
    const string additionalPreviousVersion = "1.3.0-alpha.1";

    JiraTestUtility.DeleteVersionsIfExistent(
        _config.Jira.JiraProjectKey,
        _restClient,
        currentVersionName,
        nextVersionName,
        additionalPreviousVersion,
        previousFullVersion);

    var originalVersionID = JiraTestUtility.CreateVersion(_restClient, currentVersionName, _config.Jira.JiraProjectKey);
    var followingVersionID = JiraTestUtility.CreateVersion(_restClient, nextVersionName, _config.Jira.JiraProjectKey);
    var previousFullVersionID = JiraTestUtility.CreateVersion(_restClient, previousFullVersion, _config.Jira.JiraProjectKey);
    var additionalPreviousVersionID = JiraTestUtility.CreateVersion(_restClient, additionalPreviousVersion, _config.Jira.JiraProjectKey);

    var originalIssue1 = JiraTestUtility.AddTestIssueToVersion("test1", true, _config.Jira.JiraProjectKey, _restClient, previousFullVersionID);
    var originalIssue2 = JiraTestUtility.AddTestIssueToVersion("test1", true, _config.Jira.JiraProjectKey, _restClient, previousFullVersionID);
    var originalOpenIssue = JiraTestUtility.AddTestIssueToVersion("open Issue", false, _config.Jira.JiraProjectKey, _restClient, originalVersionID);

    var previousVersionClosedIssue = JiraTestUtility.AddTestIssueToVersion(
        "not moved issue",
        true,
        _config.Jira.JiraProjectKey,
        _restClient,
        additionalPreviousVersionID);

    var followingVersionClosedIssue = JiraTestUtility.AddTestIssueToVersion(
        "test5",
        true,
        _config.Jira.JiraProjectKey,
        _restClient,
        followingVersionID);

    var jiraVersionCreator = new JiraVersionCreator(_config, _versionFinder, _service);

    var testConsole = new TestConsole();
    var inputReader = new Mock<IInputReader>();
    inputReader.Setup(_ => _.ReadConfirmation(true)).Returns(true);
    var jiraVersionRepairer = new JiraProjectVersionRepairer(_service, _versionFinder);
    var jiraVersionReleaser = new JiraVersionReleaser(_config, jiraVersionRepairer, _service);

    var releaseVersionAndMoveIssues = new ReleaseVersionAndMoveIssuesSubStep(
        testConsole,
        inputReader.Object,
        _issueService,
        jiraVersionCreator,
        jiraVersionReleaser);
    var parser = new SemanticVersionParser();

    Assert.That(
        () => releaseVersionAndMoveIssues.Execute(parser.ParseVersion(currentVersionName), parser.ParseVersion(nextVersionName), false, true),
        Throws.Nothing);

    var movedAndOldClosedIssues = _issueService.FindAllClosedIssues(originalVersionID);
    Assert.That(movedAndOldClosedIssues.Count(), Is.EqualTo(2));

    var shouldBeNoClosedIssues = _issueService.FindAllClosedIssues(previousFullVersionID);
    Assert.That(shouldBeNoClosedIssues.Count(), Is.EqualTo(0));

    var additionalClosedIssues = _issueService.FindAllClosedIssues(additionalPreviousVersionID);
    Assert.That(additionalClosedIssues.Count(), Is.EqualTo(1));

    var newOpenIssues = _issueService.FindAllNonClosedIssues(followingVersionID);
    Assert.That(newOpenIssues.Count(), Is.EqualTo(1));

    JiraTestUtility.DeleteVersionsIfExistent(
        _config.Jira.JiraProjectKey,
        _restClient,
        currentVersionName,
        nextVersionName,
        additionalPreviousVersion,
        previousFullVersion);
    JiraTestUtility.DeleteIssues(
        _restClient,
        originalIssue1.ID,
        originalIssue2.ID,
        followingVersionClosedIssue.ID,
        originalOpenIssue.ID,
        previousVersionClosedIssue.ID);
  }

  [Test]
  public void ReleaseVersionAndMoveIssues_NoIssuesToMove_DoesntThrow ()
  {
    const string currentVersionName = "1.3.0-alpha.2";
    const string nextVersionName = "1.3.0-alpha.3";

    JiraTestUtility.DeleteVersionsIfExistent(_config.Jira.JiraProjectKey, _restClient, currentVersionName, nextVersionName);

    var originalVersionID = JiraTestUtility.CreateVersion(_restClient, currentVersionName, _config.Jira.JiraProjectKey);
    var followingVersionID = JiraTestUtility.CreateVersion(_restClient, nextVersionName, _config.Jira.JiraProjectKey);

    var originalOpenIssue = JiraTestUtility.AddTestIssueToVersion("open Issue", false, _config.Jira.JiraProjectKey, _restClient, originalVersionID);

    var followingVersionClosedIssue = JiraTestUtility.AddTestIssueToVersion(
        "test5",
        true,
        _config.Jira.JiraProjectKey,
        _restClient,
        followingVersionID);

    var jiraVersionCreator = new JiraVersionCreator(_config, _versionFinder, _service);

    var testConsole = new TestConsole();
    var inputReader = new Mock<IInputReader>();
    inputReader.Setup(_ => _.ReadConfirmation(true)).Returns(true);
    var jiraVersionRepairer = new JiraProjectVersionRepairer(_service, _versionFinder);
    var jiraVersionReleaser = new JiraVersionReleaser(_config, jiraVersionRepairer, _service);

    var releaseVersionAndMoveIssues = new ReleaseVersionAndMoveIssuesSubStep(
        testConsole,
        inputReader.Object,
        _issueService,
        jiraVersionCreator,
        jiraVersionReleaser);
    var parser = new SemanticVersionParser();

    Assert.That(
        () => releaseVersionAndMoveIssues.Execute(parser.ParseVersion(currentVersionName), parser.ParseVersion(nextVersionName), false, true),
        Throws.Nothing);

    var movedAndOldClosedIssues = _issueService.FindAllClosedIssues(originalVersionID);
    Assert.That(movedAndOldClosedIssues.Count(), Is.EqualTo(0));

    var newOpenIssues = _issueService.FindAllNonClosedIssues(followingVersionID);
    Assert.That(newOpenIssues.Count(), Is.EqualTo(1));

    JiraTestUtility.DeleteVersionsIfExistent(_config.Jira.JiraProjectKey, _restClient, currentVersionName, nextVersionName);
    JiraTestUtility.DeleteIssues(_restClient, followingVersionClosedIssue.ID, originalOpenIssue.ID);
  }

  [Test]
  public void FindVersionWithVersionNumber_FindsProperVersion ()
  {
    var versionName = "1.3.0-alpha.1";

    JiraTestUtility.DeleteVersionIfExistent(_config.Jira.JiraProjectKey, versionName, _restClient);

    var versionID = JiraTestUtility.CreateVersion(_restClient, versionName, _config.Jira.JiraProjectKey);

    var jiraVersionCreator = new JiraVersionCreator(_config, _versionFinder, _service);

    var output = jiraVersionCreator.FindVersionWithVersionNumber(versionName);

    Assert.That(output, Is.Not.Null);
    Assert.That(output.id, Is.EqualTo(versionID));
  }

  [Test]
  public void FindAllVersionsWithVersionNumber_FindsProperVersions ()
  {
    var fullVersionName = "1.3.0";
    var alphaVersionName = "1.3.0-alpha.1";
    var betaVersionName = "1.3.0-beta.3";

    JiraTestUtility.DeleteVersionsIfExistent(_config.Jira.JiraProjectKey, _restClient, fullVersionName, alphaVersionName, betaVersionName);

    var fullVersionID = JiraTestUtility.CreateVersion(_restClient, fullVersionName, _config.Jira.JiraProjectKey);
    var alphaVersionID = JiraTestUtility.CreateVersion(_restClient, alphaVersionName, _config.Jira.JiraProjectKey);
    var betaVersionID = JiraTestUtility.CreateVersion(_restClient, betaVersionName, _config.Jira.JiraProjectKey);

    var jiraVersionCreator = new JiraVersionCreator(_config, _versionFinder, _service);

    var output = jiraVersionCreator.FindAllVersionsStartingWithVersionNumber(fullVersionName);

    var jiraProjectVersions = output as JiraProjectVersion[] ?? output.ToArray();

    Assert.That(jiraProjectVersions.Length, Is.GreaterThan(3).Or.EqualTo(3));
    Assert.That(jiraProjectVersions.Where(v => v.id.Equals(fullVersionID)), Is.Not.Null);
    Assert.That(jiraProjectVersions.Where(v => v.id.Equals(alphaVersionID)), Is.Not.Null);
    Assert.That(jiraProjectVersions.Where(v => v.id.Equals(betaVersionID)), Is.Not.Null);

    JiraTestUtility.DeleteVersionsIfExistent(_config.Jira.JiraProjectKey, _restClient, fullVersionName, alphaVersionName, betaVersionName);
  }

  [Test]
  public void FindIssuesWithOnlyExactFixVersion_ReturnsFoundVersions ()
  {
    var fullVersionName = "1.3.0";
    var alphaVersionName = "1.3.0-alpha.1";
    var betaVersionName = "1.3.0-beta.3";

    JiraTestUtility.DeleteVersionsIfExistent(_config.Jira.JiraProjectKey, _restClient, fullVersionName, alphaVersionName, betaVersionName);

    var fullVersionID = JiraTestUtility.CreateVersion(_restClient, fullVersionName, _config.Jira.JiraProjectKey);
    var alphaVersionID = JiraTestUtility.CreateVersion(_restClient, alphaVersionName, _config.Jira.JiraProjectKey);
    var betaVersionID = JiraTestUtility.CreateVersion(_restClient, betaVersionName, _config.Jira.JiraProjectKey);

    var closedIssueOnlyFull = JiraTestUtility.AddTestIssueToVersion(
        "Only full version fix",
        true,
        _config.Jira.JiraProjectKey,
        _restClient,
        fullVersionID);
    var closedIssueFull1 = JiraTestUtility.AddTestIssueToVersion(
        "test1",
        true,
        _config.Jira.JiraProjectKey,
        _restClient,
        alphaVersionID,
        fullVersionID);
    var closedIssueFull2 = JiraTestUtility.AddTestIssueToVersion(
        "test2",
        true,
        _config.Jira.JiraProjectKey,
        _restClient,
        betaVersionID,
        fullVersionID);
    var otherClosedIssue1 = JiraTestUtility.AddTestIssueToVersion("test3", true, _config.Jira.JiraProjectKey, _restClient, alphaVersionID);
    var otherClosedIssue2 = JiraTestUtility.AddTestIssueToVersion("test3", true, _config.Jira.JiraProjectKey, _restClient, betaVersionID);

    var versionCreator = new JiraVersionCreator(_config, _versionFinder, _service);

    var allVersionsWithFullVersion = versionCreator.FindAllVersionsStartingWithVersionNumber(fullVersionName);

    var outputIssues = _issueService.FindIssuesWithOnlyExactFixVersion(allVersionsWithFullVersion, new JiraProjectVersion { id = fullVersionID })
        .ToArray();

    Assert.That(outputIssues.Select(i => i.ID), Does.Contain(closedIssueOnlyFull.ID));
    Assert.That(outputIssues.Select(i => i.ID), Does.Not.Contain(otherClosedIssue1.ID));
    Assert.That(outputIssues.Select(i => i.ID), Does.Not.Contain(otherClosedIssue2.ID));
    Assert.That(outputIssues.Select(i => i.ID), Does.Not.Contain(closedIssueFull1.ID));
    Assert.That(outputIssues.Select(i => i.ID), Does.Not.Contain(closedIssueFull2.ID));

    JiraTestUtility.DeleteVersionsIfExistent(_config.Jira.JiraProjectKey, _restClient, fullVersionName, alphaVersionName, betaVersionName);
    JiraTestUtility.DeleteIssues(
        _restClient,
        closedIssueFull1.ID,
        closedIssueFull2.ID,
        otherClosedIssue1.ID,
        otherClosedIssue2.ID,
        closedIssueOnlyFull.ID);
  }
}