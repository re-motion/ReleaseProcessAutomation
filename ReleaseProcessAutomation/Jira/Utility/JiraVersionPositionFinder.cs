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
using ReleaseProcessAutomation.Jira.ServiceFacadeImplementations;

namespace ReleaseProcessAutomation.Jira.Utility;

public class JiraVersionPositionFinder<T> : IJiraVersionMovePositioner<T>
    where T : IComparable<T>
{
  private readonly List<JiraProjectVersionComparableAdapter<T>> _jiraProjectVersions;
  private readonly IOrderedEnumerable<JiraProjectVersionComparableAdapter<T>> _orderedVersions;
  private readonly List<JiraProjectVersionComparableAdapter<T>> _toBeOrdered;
  private readonly JiraProjectVersionComparableAdapter<T> _createdVersion;

  public JiraVersionPositionFinder (
      List<JiraProjectVersionComparableAdapter<T>> jiraProjectVersions,
      JiraProjectVersionComparableAdapter<T> createdVersion)
  {
    _jiraProjectVersions = jiraProjectVersions;
    _createdVersion = createdVersion;

    _toBeOrdered = _jiraProjectVersions.ToList();
    _toBeOrdered.Add(createdVersion);

    _orderedVersions = _toBeOrdered.OrderBy(x => x.ComparableVersion);
  }

  public JiraProjectVersionComparableAdapter<T> GetCreatedVersion ()
  {
    return _createdVersion;
  }

  public bool HasToBeMoved ()
  {
    if (_jiraProjectVersions.Count == 0)
      return false;

    if (Equals(GetVersionBeforeCreatedVersionOriginalList(), GetVersionBeforeCreatedVersionOrderedList()))
      return false;

    return true;
  }

  public JiraProjectVersionComparableAdapter<T>? GetVersionBeforeCreatedVersionOrderedList ()
  {
    return _orderedVersions.TakeWhile(x => !Equals(x.ComparableVersion, _createdVersion.ComparableVersion)).LastOrDefault();
  }

  private JiraProjectVersionComparableAdapter<T>? GetVersionBeforeCreatedVersionOriginalList ()
  {
    return _toBeOrdered.TakeWhile(x => !Equals(x.ComparableVersion, _createdVersion.ComparableVersion)).LastOrDefault();
  }
}