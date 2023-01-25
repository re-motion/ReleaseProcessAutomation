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

#nullable enable
using System.Collections.Generic;
using Moq;
using NUnit.Framework;
using Remotion.ReleaseProcessAutomation.Jira;
using Remotion.ReleaseProcessAutomation.Jira.ServiceFacadeImplementations;
using Remotion.ReleaseProcessAutomation.ReadInput;
using Remotion.ReleaseProcessAutomation.SemanticVersioning;
using Remotion.ReleaseProcessAutomation.Steps.SubSteps;
using Spectre.Console.Testing;

namespace Remotion.ReleaseProcessAutomation.UnitTests.Steps;

[TestFixture]
public class AddFixVersionsForNewReleaseBranchStepTests
{
  private readonly SemanticVersionParser _parser = new SemanticVersionParser();

  [Test]
  public void Execute_WithConfirmationToAddRCAndNextJiraVersion_AddsRcVersionAndNextJiraVersion ()
  {

    var currentVersion = _parser.ParseVersion("1.0.0");
    var nextJiraVersion = _parser.ParseVersion("1.1.0");
    var rcVersion = _parser.ParseVersion("1.0.0-rc.1");

    const string rcVersionId = "rc";
    const string nextJiraVersionId = "next";

    var issue1 = new JiraToBeMovedIssue
      {
        Fields = new JiraNonClosedIssueFields
          {
            Summary = "First issue"
          },
        ID = "1",
        Key = "K-1"
      };
    var issue2 = new JiraToBeMovedIssue
      {
        Fields = new JiraNonClosedIssueFields
          {
            Summary = "Second issue"
          },
        ID = "2",
        Key = "K-2"
      };

    var issues = new[] { issue1, issue2 };
    JiraProjectVersion? nullVersion = null;

    var testConsole = new TestConsole();
    var inputReaderMock = new Mock<IInputReader>();
    var jiraIssueServiceMock = new Mock<IJiraIssueService>();
    var jiraVersionCreatorMock = new Mock<IJiraVersionCreator>();
    inputReaderMock.Setup(_ => _.ReadConfirmation(It.IsAny<bool>())).Returns(true);
    jiraVersionCreatorMock.Setup(_ => _.CreateNewVersionWithVersionNumber(rcVersion.ToString())).Returns(rcVersionId).Verifiable();
    jiraVersionCreatorMock.Setup(_ => _.FindVersionWithVersionNumber(rcVersion.ToString())).Returns(nullVersion).Verifiable();
    jiraVersionCreatorMock.Setup(_ => _.FindVersionWithVersionNumber(nextJiraVersion.ToString())).Returns(new JiraProjectVersion { id = nextJiraVersionId }).Verifiable();
    jiraIssueServiceMock.Setup(_ => _.FindAllNonClosedIssues(currentVersion.ToString())).Returns(issues).Verifiable();

    var step = new AddFixVersionsForNewReleaseBranchSubStep(testConsole, inputReaderMock.Object, jiraIssueServiceMock.Object, jiraVersionCreatorMock.Object);

    step.Execute(currentVersion, nextJiraVersion);

    jiraVersionCreatorMock.Verify();
    jiraIssueServiceMock.Verify();
    jiraIssueServiceMock.Verify(_ => _.AddFixVersionToIssues(issues, rcVersionId), Times.Once);
    jiraIssueServiceMock.Verify(_ => _.AddFixVersionToIssues(issues, nextJiraVersionId), Times.Once);
    inputReaderMock.Verify(_ => _.ReadConfirmation(It.IsAny<bool>()),Times.Exactly(2));

  }

  [Test]
  public void Execute_WithNoConfirmationGiven_DoesNotAddAnyVersionToIssues()
  {

    var currentVersion = _parser.ParseVersion("1.0.0");
    var nextJiraVersion = _parser.ParseVersion("1.1.0");

    var issue1 = new JiraToBeMovedIssue
      {
        Fields = new JiraNonClosedIssueFields
          {
            Summary = "First issue"
          },
        ID = "1",
        Key = "K-1"
      };
    var issue2 = new JiraToBeMovedIssue
      {
        Fields = new JiraNonClosedIssueFields
          {
            Summary = "Second issue"
          },
        ID = "2",
        Key = "K-2"
      };

    var issues = new[] { issue1, issue2 };

    var testConsole = new TestConsole();
    var inputReaderMock = new Mock<IInputReader>();
    var jiraIssueServiceMock = new Mock<IJiraIssueService>();
    var jiraVersionCreatorMock = new Mock<IJiraVersionCreator>();
    inputReaderMock.Setup(_ => _.ReadConfirmation(It.IsAny<bool>())).Returns(false);
    jiraIssueServiceMock.Setup(_ => _.FindAllNonClosedIssues(currentVersion.ToString())).Returns(issues).Verifiable();

    var step = new AddFixVersionsForNewReleaseBranchSubStep(testConsole, inputReaderMock.Object, jiraIssueServiceMock.Object, jiraVersionCreatorMock.Object);

    step.Execute(currentVersion, nextJiraVersion);

    jiraVersionCreatorMock.Verify(_ => _.FindVersionWithVersionNumber(It.IsAny<string>()), Times.Never);
    jiraVersionCreatorMock.Verify(_ => _.CreateNewVersionWithVersionNumber(It.IsAny<string>()), Times.Never);
    jiraIssueServiceMock.Verify();
    jiraIssueServiceMock.Verify(_ => _.AddFixVersionToIssues(It.IsAny<IReadOnlyCollection<JiraToBeMovedIssue>>(), It.IsAny<string>()), Times.Never);
    inputReaderMock.Verify(_ => _.ReadConfirmation(It.IsAny<bool>()),Times.Exactly(2));
  }
}