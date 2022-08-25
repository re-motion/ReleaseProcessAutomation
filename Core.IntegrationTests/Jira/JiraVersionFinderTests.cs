using System;
using System.Linq;
using Moq;
using NUnit.Framework;
using ReleaseProcessAutomation.Jira.ServiceFacadeImplementations;
using ReleaseProcessAutomation.Jira.Utility;
using ReleaseProcessAutomation.ReadInput;

namespace ReleaseProcessAutomation.IntegrationTests.Jira;

[Explicit]
[TestFixture]
public class JiraVersionFinderTests
{
  private const string c_jiraUrl = "https://re-motion.atlassian.net/";
  private const string c_jiraProjectKey = "SRCBLDTEST";

  private Mock<IJiraRestClientProvider> _restClientProviderMock;
  private JiraRestClient _restClient;
  private JiraProjectVersionService _service;
  private JiraProjectVersionFinder _versionFinder;
  private JiraIssueService _issueService;

  [SetUp]
  public void SetUp ()
  {
    var testCredentials = JiraTestUtility.GetLocallySavedCredentials();

    _restClient = JiraRestClient.CreateWithBasicAuthentication(c_jiraUrl, testCredentials);

    _restClientProviderMock = new Mock<IJiraRestClientProvider>();
    _restClientProviderMock.Setup(_ => _.GetJiraRestClient()).Returns(_restClient);

    _versionFinder = new JiraProjectVersionFinder(_restClientProviderMock.Object);
    _issueService = new JiraIssueService(_restClientProviderMock.Object);
    _service = new JiraProjectVersionService(_restClientProviderMock.Object, _issueService, _versionFinder);
  }

  [Test]
  public void FindUnreleasedVersions_WithFourVersions_FindsProperOnes ()
  {
    JiraTestUtility.DeleteVersionsIfExistent(c_jiraProjectKey, _restClient, "4.1.0", "4.1.1", "4.1.2", "4.2.0");

    _service = new JiraProjectVersionService(_restClientProviderMock.Object, _issueService, _versionFinder);

    // Create versions
    _service.CreateVersion(c_jiraProjectKey, "4.1.0", DateTime.Today.AddDays(1));
    _service.CreateSubsequentVersion(c_jiraProjectKey, "4\\.1\\..*", 3, DayOfWeek.Monday);
    _service.CreateSubsequentVersion(c_jiraProjectKey, "4\\.1\\..*", 3, DayOfWeek.Tuesday);
    _service.CreateVersion(c_jiraProjectKey, "4.2.0", DateTime.Today.AddDays(7));

    // Get latest unreleased version
    var versions = _versionFinder.FindUnreleasedVersions(c_jiraProjectKey, "4.1.").ToList();
    Assert.That(versions.Select(v => v.name), Is.EquivalentTo(new[] { "4.1.0", "4.1.1", "4.1.2" }));

    var versions2 = _versionFinder.FindUnreleasedVersions(c_jiraProjectKey, "4.2.");
    Assert.That(versions2.Select(v => v.name), Is.EquivalentTo(new[] { "4.2.0" }));

    JiraTestUtility.DeleteVersionsIfExistent(c_jiraProjectKey, _restClient, "4.1.0", "4.1.1", "4.1.2", "4.2.0");
  }
}