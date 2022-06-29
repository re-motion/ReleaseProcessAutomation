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
using System.Collections.Generic;
using System.Linq;
using ReleaseProcessAutomation.Configuration.Data;
using ReleaseProcessAutomation.Jira.ServiceFacadeImplementations;
using ReleaseProcessAutomation.Jira.ServiceFacadeInterfaces;

namespace ReleaseProcessAutomation.Jira;

public class JiraVersionCreator
    : IJiraVersionCreator
{
  private readonly Config _config;
  private readonly IJiraProjectVersionFinder _projectVersionFinder;
  private readonly IJiraProjectVersionService _jiraProjectVersionService;

  public JiraVersionCreator (Config config, IJiraProjectVersionFinder projectVersionFinder, IJiraProjectVersionService jiraProjectVersionService)
  {
    _config = config;
    _projectVersionFinder = projectVersionFinder;
    _jiraProjectVersionService = jiraProjectVersionService;
  }

  public string CreateNewVersionWithVersionNumber (string versionNumber)
  {
    var jiraProject = _config.Jira.JiraProjectKey;
    if (string.IsNullOrEmpty(jiraProject))
      throw new InvalidOperationException("Jira project was not assigned.");

    var jiraProjectVersionRepairer = new JiraProjectVersionRepairer(_jiraProjectVersionService, _projectVersionFinder);

    var jiraProjectVersion = FindVersionWithVersionNumber(versionNumber);

    string createdVersionID;

    if (jiraProjectVersion != null)
    {
      if (jiraProjectVersion.released)
        throw new JiraException("The Version '" + versionNumber + "' got already released in Jira.");

      if (string.IsNullOrEmpty(jiraProjectVersion.id))
        throw new InvalidOperationException("Jira project id was not assigned.");
      createdVersionID = jiraProjectVersion.id;
    }
    else
    {
      if (string.IsNullOrEmpty(versionNumber))
        throw new InvalidOperationException("Version number was not assigned.");
      createdVersionID = _jiraProjectVersionService.CreateVersion(jiraProject, versionNumber, null);

      jiraProjectVersionRepairer.RepairVersionPosition(createdVersionID);
    }

    return createdVersionID;
  }

  public JiraProjectVersion? FindVersionWithVersionNumber (string versionNumber)
  {
    var versions = _projectVersionFinder.FindVersions(_config.Jira.JiraProjectKey).ToList();
     return versions.FirstOrDefault(x => x.name == versionNumber);
  }
  
  public IReadOnlyList<JiraProjectVersion> FindAllVersionsStartingWithVersionNumber (string versionNumber)
  {
    var versions = _projectVersionFinder.FindVersions(_config.Jira.JiraProjectKey).ToList();
    return versions.Where(x => x.name.StartsWith(versionNumber)).ToList();
  }
}