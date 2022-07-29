using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using ReleaseProcessAutomation.Configuration;
using ReleaseProcessAutomation.Configuration.Data;
using ReleaseProcessAutomation.Jira.ServiceFacadeImplementations;

namespace ReleaseProcessAutomation.IntegrationTests.Jira;
[TestFixture]
[Explicit]
public class JiraIntegrationTests : IntegrationTestSetup
{
  private string _jiraUsername;
  private string _jiraPassword;
  private Config _config;
  private JiraRestClient _testRestClient;

  [SetUp]
  public void Setup ()
  {
    var testCredentials = JiraTestUtility.GetLocallySavedCredentials();
    _jiraUsername = testCredentials.Username;
    _jiraPassword = testCredentials.Password;
    
    var configPath = Path.Combine(Environment.CurrentDirectory, "Build", "Customizations", c_testConfigName);
    _config = new ConfigReader().LoadConfig(configPath);
    
    _testRestClient = JiraRestClient.CreateWithBasicAuthentication(_config.Jira.JiraURL, testCredentials);
  }
  
  [Test]
  public void TestJiraRunThrough_FromDevelop_MovesOnlyOpenJiraVersions ()
  {
    var prevVersion = "1.0.0";
    var nextVersion = "1.1.0";
    JiraTestUtility.DeleteVersionsIfExistent(_config.Jira.JiraProjectKey, _testRestClient, prevVersion, nextVersion);

    var newVersionID = JiraTestUtility.CreateVersion(_testRestClient, prevVersion, _config.Jira.JiraProjectKey);
    
    var openIssue = JiraTestUtility.AddTestIssueToVersion(
        "Some open issue", 
        false, 
        _config.Jira.JiraProjectKey, 
        _testRestClient, 
      new JiraProjectVersion(){name = prevVersion, id = newVersionID});
    
    var closedIssue = JiraTestUtility.AddTestIssueToVersion(
      "Some closed issue",
      true,
      _config.Jira.JiraProjectKey,
      _testRestClient,
      new JiraProjectVersion() { name = prevVersion, id = newVersionID});
    
    ExecuteGitCommand("commit -m feature --allow-empty");
    ExecuteGitCommand("checkout -b develop");

    //Get release version from user
    TestConsole.Input.PushTextWithEnter(prevVersion);
    //Get next release version from user for jira
    TestConsole.Input.PushTextWithEnter(nextVersion);
    
    TestConsole.Input.PushTextWithEnter(_jiraUsername);
    TestConsole.Input.PushTextWithEnter(_jiraPassword);
    
    TestConsole.Input.PushTextWithEnter("n");
    

    var act = JiraTestUtility.RunProgramWithoutWindowsCredentials(new[] { "Release-Version" });

    Assert.That(act, Is.EqualTo(0));
    
    var isPartOfNewVersion = JiraTestUtility.IsPartOfJiraVersions(_config.Jira.JiraProjectKey, nextVersion, _testRestClient, out var newVersion);
    Assert.That(isPartOfNewVersion, Is.True);
    Assert.That(newVersion.released, Is.False);

    openIssue = JiraTestUtility.GetIssue(openIssue.ID, _testRestClient);
    Assert.That(openIssue.fields.FixVersions.First().ID , Is.EqualTo(newVersion.id));
    
    var isPartOfOldVersion = JiraTestUtility.IsPartOfJiraVersions(_config.Jira.JiraProjectKey, prevVersion, _testRestClient, out var oldVersion);
    Assert.That(isPartOfOldVersion, Is.True);
    Assert.That(oldVersion.released, Is.True);
    
    closedIssue = JiraTestUtility.GetIssue(closedIssue.ID, _testRestClient);
    Assert.That(closedIssue.fields.FixVersions.First().ID, Is.EqualTo(oldVersion.id));
    
    JiraTestUtility.DeleteVersionsIfExistent(_config.Jira.JiraProjectKey, _testRestClient, prevVersion, nextVersion);
    JiraTestUtility.DeleteIssues(_testRestClient, openIssue.ID, closedIssue.ID);
  }
  
  [Test]
  public void TestJiraRunThrough_FromRelease_MovesOnlyOpenJiraVersions ()
  {
    var prevVersion = "1.3.5";
    var nextVersion = "1.3.6";
    JiraTestUtility.DeleteVersionsIfExistent(_config.Jira.JiraProjectKey, _testRestClient, prevVersion, nextVersion);

    var newVersionID = JiraTestUtility.CreateVersion(_testRestClient, prevVersion, _config.Jira.JiraProjectKey);
    
    var openIssue = JiraTestUtility.AddTestIssueToVersion(
        "Some open issue", 
        false, 
        _config.Jira.JiraProjectKey, 
        _testRestClient, 
      new JiraProjectVersion(){name = prevVersion, id = newVersionID});
    
    var closedIssue = JiraTestUtility.AddTestIssueToVersion(
      "Some closed issue",
      true,
      _config.Jira.JiraProjectKey,
      _testRestClient,
      new JiraProjectVersion() { name = prevVersion, id = newVersionID});
    
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
    
    TestConsole.Input.PushTextWithEnter("n");
    

    var act = JiraTestUtility.RunProgramWithoutWindowsCredentials(new[] { "Release-Version" });

    Assert.That(act, Is.EqualTo(0));
    
    var isPartOfNewVersion = JiraTestUtility.IsPartOfJiraVersions(_config.Jira.JiraProjectKey, nextVersion, _testRestClient, out var newVersion);
    Assert.That(isPartOfNewVersion, Is.True);
    Assert.That(newVersion.released, Is.False);

    openIssue = JiraTestUtility.GetIssue(openIssue.ID, _testRestClient);
    Assert.That(openIssue.fields.FixVersions.First().ID , Is.EqualTo(newVersion.id));
    
    var isPartOfOldVersion = JiraTestUtility.IsPartOfJiraVersions(_config.Jira.JiraProjectKey, prevVersion, _testRestClient, out var oldVersion);
    Assert.That(isPartOfOldVersion, Is.True);
    Assert.That(oldVersion.released, Is.True);
    
    closedIssue = JiraTestUtility.GetIssue(closedIssue.ID, _testRestClient);
    Assert.That(closedIssue.fields.FixVersions.First().ID, Is.EqualTo(oldVersion.id));
    
    JiraTestUtility.DeleteVersionsIfExistent(_config.Jira.JiraProjectKey, _testRestClient, prevVersion, nextVersion);
    JiraTestUtility.DeleteIssues(_testRestClient, openIssue.ID, closedIssue.ID);
  }
  
   [Test]
  public void TestJiraRunThrough_WithManyDifferentVersionsWithCloseNames_ReleasesCorrectJiraVersions ()
  {
    JiraTestUtility.DeleteVersionsIfExistent(_config.Jira.JiraProjectKey, _testRestClient, "1.0.0", "1.1.0", "1.0.1", "1.0.2");
    
    var newVersionID = JiraTestUtility.CreateVersion(_testRestClient, "1.0.0", _config.Jira.JiraProjectKey);
    
    JiraTestUtility.CreateVersion(_testRestClient, "1.0.1", _config.Jira.JiraProjectKey);
    JiraTestUtility.CreateVersion(_testRestClient, "1.0.2", _config.Jira.JiraProjectKey);
    
    var openIssue = JiraTestUtility.AddTestIssueToVersion(
        "Some open issue", 
        false, 
        _config.Jira.JiraProjectKey, 
        _testRestClient, 
      new JiraProjectVersion(){name = "1.0.0", id = newVersionID});
    
    var closedIssue = JiraTestUtility.AddTestIssueToVersion(
      "Some closed issue",
      true,
      _config.Jira.JiraProjectKey,
      _testRestClient,
      new JiraProjectVersion() { name = "1.0.0", id = newVersionID});
    
    ExecuteGitCommand("commit -m feature --allow-empty");
    ExecuteGitCommand("checkout -b develop");

    //Get release version from user
    TestConsole.Input.PushTextWithEnter("1.0.0");
    //Get next release version from user for jira
    TestConsole.Input.PushTextWithEnter("1.1.0");
    
    TestConsole.Input.PushTextWithEnter(_jiraUsername);
    TestConsole.Input.PushTextWithEnter(_jiraPassword);
    
    TestConsole.Input.PushTextWithEnter("n");
    

    var act = JiraTestUtility.RunProgramWithoutWindowsCredentials(new[] { "Release-Version" });

    Assert.That(act, Is.EqualTo(0));
    
    var is110JiraVersion = JiraTestUtility.IsPartOfJiraVersions(_config.Jira.JiraProjectKey, "1.1.0", _testRestClient, out var version110);
    Assert.That(is110JiraVersion, Is.True);
    Assert.That(version110.released, Is.False);

    openIssue = JiraTestUtility.GetIssue(openIssue.ID, _testRestClient);
    Assert.That(openIssue.fields.FixVersions.First().ID , Is.EqualTo(version110.id));
    
    var is100JiraVersion = JiraTestUtility.IsPartOfJiraVersions(_config.Jira.JiraProjectKey, "1.0.0", _testRestClient, out var version100);
    Assert.That(is100JiraVersion, Is.True);
    Assert.That(version100.released, Is.True);
    
    closedIssue = JiraTestUtility.GetIssue(closedIssue.ID, _testRestClient);
    Assert.That(closedIssue.fields.FixVersions.First().ID, Is.EqualTo(version100.id));
    
    JiraTestUtility.DeleteVersionsIfExistent(_config.Jira.JiraProjectKey, _testRestClient, "1.0.0", "1.1.0", "1.0.1", "1.0.2");
    JiraTestUtility.DeleteIssues(_testRestClient, openIssue.ID, closedIssue.ID);
  }
  
}