using System;
using System.IO;
using System.Linq;
using Moq;
using NUnit.Framework;
using ReleaseProcessAutomation.Configuration;
using ReleaseProcessAutomation.Configuration.Data;
using ReleaseProcessAutomation.IntegrationTests.Jira;
using ReleaseProcessAutomation.Jira.ServiceFacadeImplementations;
using ReleaseProcessAutomation.Jira.Utility;

namespace ReleaseProcessAutomation.IntegrationTests;

[Explicit]
[TestFixture]
public class ReleasePreReleaseWithJiraTests : IntegrationTestSetup
{
  private string _jiraUsername;
  private string _jiraPassword;
  private Config _config;
  private JiraRestClient _restClient;
  private Mock<IJiraRestClientProvider> _restClientProviderMock;
  private JiraIssueService _issueService;

  [SetUp]
  public override void Setup ()
  {
    base.Setup();
    var testCredentials = JiraTestUtility.GetLocallySavedCredentials();
    _jiraUsername = testCredentials.Username;
    _jiraPassword = testCredentials.Password;
    
    var configPath = Path.Combine(Environment.CurrentDirectory, "Build", "Customizations", c_testConfigName);
    _config = new ConfigReader().LoadConfig(configPath);
    
    _restClient = JiraRestClient.CreateWithBasicAuthentication(_config.Jira.JiraURL, testCredentials);
    
    _restClientProviderMock = new Mock<IJiraRestClientProvider>();
    _restClientProviderMock.Setup(_ => _.GetJiraRestClient()).Returns(_restClient);
    
    _issueService = new JiraIssueService(_restClientProviderMock.Object);
  }
  
   [Test]
  public void ReleaseVersion_FromReleaseAlphaBetaStepWithJiraActive_MovesClosedIssuesFromOldToNewVersion ()
  {
    var currentVersionName = "1.3.0";
    var nextVersionName = "1.3.0-alpha.1";
    var jiraNextVersionName = "1.3.0-alpha.2";
    
    JiraTestUtility.DeleteVersionsIfExistent(_config.Jira.JiraProjectKey, _restClient, currentVersionName, nextVersionName);
    
    var originalVersionID = JiraTestUtility.CreateVersion(_restClient, currentVersionName, _config.Jira.JiraProjectKey);
    var followingVersionID = JiraTestUtility.CreateVersion(_restClient, nextVersionName, _config.Jira.JiraProjectKey);

    var closedIssue1 = JiraTestUtility.AddTestIssueToVersion("test1", true, _config.Jira.JiraProjectKey, _restClient, originalVersionID);
    var closedIssue2 = JiraTestUtility.AddTestIssueToVersion("test2", true, _config.Jira.JiraProjectKey, _restClient, originalVersionID);
    var closedIssue3 = JiraTestUtility.AddTestIssueToVersion("test3", true, _config.Jira.JiraProjectKey, _restClient, originalVersionID);
    
    var correctLogs =
        $@"*    (HEAD -> develop, origin/develop)Merge branch 'prerelease/v{nextVersionName}' into develop
          |\  
          | *  (tag: v{nextVersionName}, origin/prerelease/v{nextVersionName}, prerelease/v{nextVersionName})Update metadata to version '{nextVersionName}'.
          |/  
          * feature4
          * feature3
          * feature2
          *  (master)feature
          * ConfigAndBuildProject
          *  (origin/master)Initial CommitAll
          ";

    ExecuteGitCommand("commit -m feature --allow-empty");
    ExecuteGitCommand("checkout -b develop");

    ExecuteGitCommand("commit -m feature2 --allow-empty");
    ExecuteGitCommand("commit -m feature3 --allow-empty");
    ExecuteGitCommand("commit -m feature4 --allow-empty");

    //Get release version from user
    TestConsole.Input.PushTextWithEnter(nextVersionName);
    //Get next release version from user for jira
    TestConsole.Input.PushTextWithEnter(jiraNextVersionName);
    //Add jira credentials
    TestConsole.Input.PushTextWithEnter(_jiraUsername);
    TestConsole.Input.PushTextWithEnter(_jiraPassword);
    //Move the issues from 1.3.0 to 1.3.0-alpha.1
    TestConsole.Input.PushTextWithEnter("y");

    var act = Program.Main(new[] { "Release-Version" });

    AssertValidLogs(correctLogs);
    Assert.That(act, Is.EqualTo(0));

    var oldClosedIssues = _issueService.FindAllClosedIssues(originalVersionID);
    Assert.That(oldClosedIssues.Count(), Is.Zero);

    var newClosedIssues = _issueService.FindAllClosedIssues(followingVersionID);
    Assert.That(newClosedIssues.Count(), Is.EqualTo(3));

    JiraTestUtility.DeleteVersionsIfExistent(_config.Jira.JiraProjectKey, _restClient, currentVersionName, nextVersionName);
    JiraTestUtility.DeleteIssues(_restClient, closedIssue1.ID, closedIssue2.ID, closedIssue3.ID);
  }
  
  [Test]
  public void ReleaseVersion_FromDevelopWithoutMovingClosedIssues_DoesNotMoveVersions ()
  {
    var currentVersionName = "1.3.0";
    var nextVersionName = "1.3.0-alpha.1";
    var jiraNextVersionName = "1.3.0-alpha.2";
    
    JiraTestUtility.DeleteVersionsIfExistent(_config.Jira.JiraProjectKey, _restClient, currentVersionName, nextVersionName);
    
    var originalVersionID = JiraTestUtility.CreateVersion(_restClient, currentVersionName, _config.Jira.JiraProjectKey);
    var followingVersionID = JiraTestUtility.CreateVersion(_restClient, nextVersionName, _config.Jira.JiraProjectKey);

    var closedIssue1 = JiraTestUtility.AddTestIssueToVersion("test1", true, _config.Jira.JiraProjectKey, _restClient, originalVersionID);
    var closedIssue2 = JiraTestUtility.AddTestIssueToVersion("test2", true, _config.Jira.JiraProjectKey, _restClient, originalVersionID);
    var closedIssue3 = JiraTestUtility.AddTestIssueToVersion("test3", true, _config.Jira.JiraProjectKey, _restClient, originalVersionID);
    
    var correctLogs =
        $@"*    (HEAD -> develop, origin/develop)Merge branch 'prerelease/v{nextVersionName}' into develop
          |\  
          | *  (tag: v{nextVersionName}, origin/prerelease/v{nextVersionName}, prerelease/v{nextVersionName})Update metadata to version '{nextVersionName}'.
          |/  
          * feature4
          * feature3
          * feature2
          *  (master)feature
          * ConfigAndBuildProject
          *  (origin/master)Initial CommitAll
          ";

    ExecuteGitCommand("commit -m feature --allow-empty");
    ExecuteGitCommand("checkout -b develop");

    ExecuteGitCommand("commit -m feature2 --allow-empty");
    ExecuteGitCommand("commit -m feature3 --allow-empty");
    ExecuteGitCommand("commit -m feature4 --allow-empty");

    //Get release version from user
    TestConsole.Input.PushTextWithEnter(nextVersionName);
    //Get next release version from user for jira
    TestConsole.Input.PushTextWithEnter(jiraNextVersionName);
    //Add jira credentials
    TestConsole.Input.PushTextWithEnter(_jiraUsername);
    TestConsole.Input.PushTextWithEnter(_jiraPassword);
    //Move the issues from 1.3.0 to 1.3.0-alpha.1
    TestConsole.Input.PushTextWithEnter("n");

    var act = Program.Main(new[] { "Release-Version" });

    AssertValidLogs(correctLogs);
    Assert.That(act, Is.EqualTo(0));

    var oldClosedIssues = _issueService.FindAllClosedIssues(originalVersionID);
    Assert.That(oldClosedIssues.Count(), Is.EqualTo(3));

    var newClosedIssues = _issueService.FindAllClosedIssues(followingVersionID);
    Assert.That(newClosedIssues.Count(), Is.Zero);

    JiraTestUtility.DeleteVersionsIfExistent(_config.Jira.JiraProjectKey, _restClient, currentVersionName, nextVersionName);
    JiraTestUtility.DeleteIssues(_restClient, closedIssue1.ID, closedIssue2.ID, closedIssue3.ID);
  }
}