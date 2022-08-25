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

#nullable disable

namespace ReleaseProcessAutomation.Jira.ServiceFacadeImplementations;

public class JiraIssue
{
  public string ID { get; set; }
  public string Summary { get; set; }

  public JiraFields fields { get; set; }

  public string Issuetype { get; set; }
  public string Project { get; set; }

  public string Key { get; set; }
}

public class JiraNonClosedIssues
{
  public List<JiraToBeMovedIssue> Issues { get; set; }
}

public class JiraToBeMovedIssue
{
  public string ID { get; set; }

  public string Key { get; set; }

  public JiraNonClosedIssueFields Fields { get; set; }
}

public class JiraNonClosedIssueFields
{
  public List<JiraVersion> FixVersions { get; set; }
  
  public string Summary { get; set; }
}

public class JiraVersion
{
  public string ID { get; set; }
}

public class JiraFields
{
  public List<JiraVersion> FixVersions { get; set; }
}