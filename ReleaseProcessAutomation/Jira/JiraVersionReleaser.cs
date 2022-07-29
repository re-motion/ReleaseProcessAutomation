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
using ReleaseProcessAutomation.Configuration.Data;
using ReleaseProcessAutomation.Jira.ServiceFacadeImplementations;
using ReleaseProcessAutomation.Jira.ServiceFacadeInterfaces;

namespace ReleaseProcessAutomation.Jira;

public class JiraVersionReleaser
    : IJiraVersionReleaser
{
  private readonly Config _config;
  private readonly IJiraProjectVersionRepairer _projectVersionRepairer;
  private readonly IJiraProjectVersionService _projectVersionService;

  public JiraVersionReleaser (Config config, IJiraProjectVersionRepairer projectVersionRepairer, IJiraProjectVersionService projectVersionService)
  {
    _config = config;
    _projectVersionRepairer = projectVersionRepairer;
    _projectVersionService = projectVersionService;
  }

  public void ReleaseVersion (string versionID, string nextVersionID, bool sortReleasedVersion)
  {
    _projectVersionService.ReleaseVersion(versionID, nextVersionID);

    if (sortReleasedVersion)
      _projectVersionRepairer.RepairVersionPosition(versionID);
  }

  public void ReleaseVersionAndSquashUnreleased (string versionID, string nextVersionID)
  {
    _projectVersionService.ReleaseVersionAndSquashUnreleased(versionID, nextVersionID, _config.Jira.JiraProjectKey);
  }
}