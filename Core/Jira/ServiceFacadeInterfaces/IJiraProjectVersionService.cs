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

namespace ReleaseProcessAutomation.Jira.ServiceFacadeInterfaces;

public interface IJiraProjectVersionService
{
  /// <summary>
  /// Creates a project version.
  /// </summary>
  /// <returns>New project version ID</returns>
  string CreateVersion (string projectKey, string versionName, DateTime? releaseDate);

  /// <summary>
  /// Creates a subsequent project version.
  /// </summary>
  /// <returns>New project version ID</returns>
  string CreateSubsequentVersion (string projectKey, string versionPattern, int versionComponentToIncrement, DayOfWeek versionReleaseWeekday);

  /// <summary>
  /// Releases a project version and moves all open issues to another project version.
  /// </summary>
  void ReleaseVersion (string versionID);

  /// <summary>
  /// Releases a project and moves all open issues to the next project version
  /// Also deletes all unreleased Versions before the current Version
  /// Moves the closed Issues from deleted Versions to current Version, and open Issues to nextVersion
  /// </summary>
  void ReleaseVersionAndSquashUnreleased (string versionID, string nextVersionID, string projectKey);

  /// <summary>
  /// Deletes a project version.
  /// </summary>
  /// <exception cref="JiraException">Thrown if version does not exist.</exception>
  void DeleteVersion (string projectKey, string versionName);

  /// <summary>
  /// Moves Version to be placed after another given Version
  /// </summary>
  /// <param name="versionId"></param>
  /// <param name="afterVersionUrl"></param>
  void MoveVersion (string versionId, string afterVersionUrl);

  /// <summary>
  /// Moves Version to a given position ('First', 'Last', 'Earlier' or 'Later')
  /// </summary>
  /// <param name="versionId"></param>
  /// <param name="position"></param>
  void MoveVersionByPosition (string versionId, string position);
}