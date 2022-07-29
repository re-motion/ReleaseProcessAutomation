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

using System;
using ReleaseProcessAutomation.SemanticVersioning;
using Serilog;
using Spectre.Console;

namespace ReleaseProcessAutomation.Jira;

public class JiraFunctionality : IJiraFunctionality
{
  private readonly IAnsiConsole _console;
  private readonly IJiraVersionCreator _jiraVersionCreator;
  private readonly IJiraVersionReleaser _jiraVersionReleaser;
  private readonly ILogger _log = Log.ForContext<JiraFunctionality>();

  public JiraFunctionality (
      IAnsiConsole console,
      IJiraVersionCreator jiraVersionCreator,
      IJiraVersionReleaser jiraVersionReleaser)
  {
    _console = console;
    _jiraVersionCreator = jiraVersionCreator;
    _jiraVersionReleaser = jiraVersionReleaser;
  }

  public void CreateAndReleaseJiraVersion (SemanticVersion currentVersion, SemanticVersion nextVersion, bool squashUnreleased = false)
  {
    var currentVersionID = CreateVersion(currentVersion);
    var nextVersionID = CreateVersion(nextVersion);

    var releaseMessage = $"Releasing version '{currentVersion}' on JIRA. ";
    _log.Information(releaseMessage);
    _console.WriteLine(releaseMessage);

    var moveMessage = $"Moving open issues to '{nextVersion}'.";
    _log.Information(moveMessage);
    _console.WriteLine(moveMessage);

    ReleaseVersion(currentVersionID, nextVersionID, squashUnreleased);
  }

  private string CreateVersion (SemanticVersion version)
  {
    return _jiraVersionCreator.CreateNewVersionWithVersionNumber(version.ToString());
  }

  private void ReleaseVersion (string currentVersionID, string nextVersionID, bool squashUnreleased)
  {
    if (squashUnreleased)
      _jiraVersionReleaser.ReleaseVersionAndSquashUnreleased(currentVersionID, nextVersionID);
    else
      _jiraVersionReleaser.ReleaseVersion(currentVersionID, nextVersionID, false);
  }
}