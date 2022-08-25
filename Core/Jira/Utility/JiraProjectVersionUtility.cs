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
using ReleaseProcessAutomation.Jira.ServiceFacadeImplementations;
using ReleaseProcessAutomation.SemanticVersioning;

namespace ReleaseProcessAutomation.Jira.Utility;

public class JiraProjectVersionUtility
{
  public List<JiraProjectVersionSemVerAdapter> JiraProjectVersionsToJiraProjectVersionSemVerAdapters (
      SemanticVersionParser semVerParser,
      IEnumerable<JiraProjectVersion> jiraProjectVersions)
  {
    var versionList = new List<JiraProjectVersionSemVerAdapter>();

    foreach (var version in jiraProjectVersions)
      try
      {
        versionList.Add(
            new JiraProjectVersionSemVerAdapter()
            {
                JiraProjectVersion = version,
                SemanticVersion = semVerParser.ParseVersion(
                    version.name ?? throw new InvalidOperationException("The given version did not have a name assigned."))
            });
      }
      catch (ArgumentException)
      {
        //Empty Catch. Invalid versions are not relevant for us
      }

    return versionList;
  }
}