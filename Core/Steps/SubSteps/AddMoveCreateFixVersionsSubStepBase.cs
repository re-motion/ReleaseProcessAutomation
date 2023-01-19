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

using System.Collections.Generic;
using System.Linq;
using Remotion.ReleaseProcessAutomation.Jira;
using Remotion.ReleaseProcessAutomation.Jira.ServiceFacadeImplementations;
using Remotion.ReleaseProcessAutomation.ReadInput;
using Remotion.ReleaseProcessAutomation.SemanticVersioning;
using Spectre.Console;

namespace Remotion.ReleaseProcessAutomation.Steps.SubSteps;

public abstract class AddMoveCreateFixVersionsSubStepBase
{
  protected readonly IAnsiConsole Console;
  protected readonly IInputReader InputReader;
  protected readonly IJiraIssueService JiraIssueService;
  protected readonly IJiraVersionCreator JiraVersionCreator;

  protected AddMoveCreateFixVersionsSubStepBase (IAnsiConsole console, IInputReader inputReader, IJiraIssueService jiraIssueService, IJiraVersionCreator jiraVersionCreator)
  {
    Console = console;
    InputReader = inputReader;
    JiraIssueService = jiraIssueService;
    JiraVersionCreator = jiraVersionCreator;
  }

  protected void AddFixVersionToIssues (
      string versionToAdd,
      IEnumerable<JiraToBeMovedIssue> issues)
  {
    var currentJiraVersionID =
        JiraVersionCreator.FindVersionWithVersionNumber(versionToAdd)?.id
        ?? JiraVersionCreator.CreateNewVersionWithVersionNumber(versionToAdd);

    JiraIssueService.AddFixVersionToIssues(issues, currentJiraVersionID);
  }

  protected string CreateVersion (SemanticVersion version)
  {
    return JiraVersionCreator.CreateNewVersionWithVersionNumber(version.ToString());
  }

  protected void PrintIssueListing (IReadOnlyList<JiraToBeMovedIssue> issues, int maxLines = 5)
  {
    const string elipses = "...";
    var maxLength = Console.Profile.Width;
    foreach (var issue in issues.Take(maxLines))
    {
      var message = $"* {issue.Key} - {issue.Fields.Summary}";

      if (message.Length > maxLength)
        message = message.Substring(0, maxLength - elipses.Length).TrimEnd() + elipses;

      Console.WriteLine(message);
    }
    if (issues.Count > maxLines)
      Console.WriteLine(elipses);
  }
}