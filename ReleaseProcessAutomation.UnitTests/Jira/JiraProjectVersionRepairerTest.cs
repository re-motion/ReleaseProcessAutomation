using System;
using System.Collections.Generic;
using Moq;
using NUnit.Framework;
using ReleaseProcessAutomation.Jira.ServiceFacadeImplementations;
using ReleaseProcessAutomation.Jira.ServiceFacadeInterfaces;

namespace ReleaseProcessAutomation.UnitTests.Jira;

public class JiraProjectVersionRepairerTest
{
  private const string projectId = "exampleProjectId";

  [Test]
  public void TestAlreadySortedVersion ()
  {
    const string versionId = "exampleId";

    var createdVersion = CreateJiraProjectVersion("1.0.1", versionId);

    var jiraProjectVersions = new List<JiraProjectVersion>();
    jiraProjectVersions.Add(CreateJiraProjectVersion("1.0.0"));

    var jiraProjectVersionServiceStub = new Mock<IJiraProjectVersionService>();

    var jiraProjectVersionFinderStub = new Mock<IJiraProjectVersionFinder>();
    jiraProjectVersionFinderStub.Setup(_ => _.GetVersionById(versionId)).Returns(createdVersion);
    jiraProjectVersionFinderStub.Setup(_ => _.FindVersions(projectId, It.IsAny<string?>())).Returns(jiraProjectVersions);

    var jiraProjectVersionRepairer = new JiraProjectVersionRepairer(jiraProjectVersionServiceStub.Object, jiraProjectVersionFinderStub.Object);

    jiraProjectVersionRepairer.RepairVersionPosition(versionId);

    jiraProjectVersionServiceStub.Verify(_ => _.MoveVersionByPosition(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    jiraProjectVersionServiceStub.Verify(_ => _.MoveVersion(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
  }

  [Test]
  public void TestMoveVersionToFirstPosition ()
  {
    const string versionId = "exampleId";

    var createdVersion = CreateJiraProjectVersion("1.0.0", versionId);

    var jiraProjectVersions = new List<JiraProjectVersion>();
    jiraProjectVersions.Add(CreateJiraProjectVersion("1.0.1"));

    var jiraProjectVersionServiceStub = new Mock<IJiraProjectVersionService>();
    var jiraProjectVersionFinderStub = new Mock<IJiraProjectVersionFinder>();

    jiraProjectVersionFinderStub.Setup(_ => _.GetVersionById(versionId)).Returns(createdVersion);
    jiraProjectVersionFinderStub.Setup(_ => _.FindVersions(projectId, It.IsAny<string?>())).Returns(jiraProjectVersions);

    var jiraProjectVersionRepairer = new JiraProjectVersionRepairer(jiraProjectVersionServiceStub.Object, jiraProjectVersionFinderStub.Object);

    jiraProjectVersionRepairer.RepairVersionPosition(versionId);

    jiraProjectVersionServiceStub.Verify(_ => _.MoveVersionByPosition(versionId, "First"));
  }

  [Test]
  public void TestMoveVersionToCorrectPosition ()
  {
    const string versionId = "exampleId";
    const string beforeUrl = "someBeforeUrl";
    var createdVersion = CreateJiraProjectVersion("1.0.1", versionId);

    var jiraProjectVersions = new List<JiraProjectVersion>();
    var beforeCorrectPositionVersion = CreateJiraProjectVersion("1.0.0");
    beforeCorrectPositionVersion.self = beforeUrl;
    jiraProjectVersions.Add(beforeCorrectPositionVersion);
    jiraProjectVersions.Add(CreateJiraProjectVersion("1.0.2"));

    var jiraProjectVersionServiceStub = new Mock<IJiraProjectVersionService>();
    var jiraProjectVersionFinderStub = new Mock<IJiraProjectVersionFinder>();

    jiraProjectVersionFinderStub.Setup(x => x.GetVersionById(versionId)).Returns(createdVersion);
    jiraProjectVersionFinderStub.Setup(x => x.FindVersions(projectId, It.IsAny<string?>())).Returns(jiraProjectVersions);

    var jiraProjectVersionRepairer = new JiraProjectVersionRepairer(jiraProjectVersionServiceStub.Object, jiraProjectVersionFinderStub.Object);

    jiraProjectVersionRepairer.RepairVersionPosition(versionId);

    jiraProjectVersionServiceStub.Verify(x => x.MoveVersion(versionId, beforeUrl));
  }

  [Test]
  public void TestMoveVersionToCorrectPositionWithReleasedVersionInBetween ()
  {
    const string versionId = "exampleId";
    const string beforeUrl = "someBeforeUrl";
    var createdVersion = CreateJiraProjectVersion("1.0.2", versionId, false);

    var jiraProjectVersions = new List<JiraProjectVersion>
                              {
                                  CreateJiraProjectVersion("1.0.0", released: false),
                                  CreateJiraProjectVersion("1.0.1", released: true, self: beforeUrl),
                                  CreateJiraProjectVersion("1.0.3", released: false)
                              };

    var jiraProjectVersionServiceStub = new Mock<IJiraProjectVersionService>();
    var jiraProjectVersionFinderStub = new Mock<IJiraProjectVersionFinder>();

    jiraProjectVersionFinderStub.Setup(x => x.GetVersionById(versionId)).Returns(createdVersion);
    jiraProjectVersionFinderStub.Setup(x => x.FindVersions(projectId, It.IsAny<string?>())).Returns(jiraProjectVersions);

    var jiraProjectVersionRepairer = new JiraProjectVersionRepairer(jiraProjectVersionServiceStub.Object, jiraProjectVersionFinderStub.Object);

    jiraProjectVersionRepairer.RepairVersionPosition(versionId);

    jiraProjectVersionServiceStub.Verify(x => x.MoveVersion(versionId, beforeUrl));
  }

  private JiraProjectVersion CreateJiraProjectVersion (string name, string id = "", bool released = false, string self = "")
  {
    return new JiraProjectVersion { name = name, projectId = projectId, id = id, released = released, self = self };
  }
}