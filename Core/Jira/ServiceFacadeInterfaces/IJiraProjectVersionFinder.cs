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
using JetBrains.Annotations;
using ReleaseProcessAutomation.Jira.ServiceFacadeImplementations;

namespace ReleaseProcessAutomation.Jira.ServiceFacadeInterfaces;

public interface IJiraProjectVersionFinder
{
  /// <summary>
  /// Returns all versions of the project.
  /// </summary
  /// <returns>List of project versions or empty sequence</returns>
  IEnumerable<JiraProjectVersion> GetVersions (string projectKey);

  /// <summary>
  /// Returns all versions of the project.
  /// Filters by Regex.IsMatch(name, versionPattern) if versionPattern is not null.
  /// If no version pattern is given, returns all versions
  /// </summary
  /// <returns>List of project versions or empty sequence</returns>
  IEnumerable<JiraProjectVersion> FindVersions (string projectKey, string? versionPattern = null);

  /// <summary>
  /// Returns all unreleased versions of the project.
  /// Filters by Regex.IsMatch(name, versionPattern) if versionPattern is not null.
  /// </summary
  /// <returns>List of project versions or empty sequence</returns>
  IEnumerable<JiraProjectVersion> FindUnreleasedVersions (string projectKey, string versionPattern);

  /// <summary>
  /// Returns a specific version by id
  /// </summary>
  JiraProjectVersion GetVersionById (string versionId);
}