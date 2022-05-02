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
using System.Xml.Serialization;

#pragma warning disable CS8618

namespace ReleaseProcessAutomation.Configuration.Data;

[XmlRoot("settings")]
public class Config
{
  [XmlElement("jira")]
  public JiraConfig Jira { get; set; }

  [XmlElement("remoteRepositories")]
  public RemoteRepositoryNames RemoteRepositories { get; set; }

  [XmlElement("prereleaseMergeIgnoreList")]
  public IgnoreList PreReleaseMergeIgnoreList { get; set; }

  [XmlElement("TagStableMergeIgnoreList")]
  public IgnoreList TagStableMergeIgnoreList { get; set; }

  [XmlElement("developStableMergeIgnoreList")]
  public IgnoreList DevelopStableMergeIgnoreList { get; set; }

  [XmlElement("msBuildSettings")]
  public Settings MSBuildSettings { get; set; }

  [XmlElement("prepareNextVersionMsBuildSteps")]
  public MSBuildSteps PrepareNextVersionMSBuildSteps { get; set; }

  [XmlElement("developmentForNextReleaseMsBuildSteps")]
  public MSBuildSteps DevelopmentForNextReleaseMSBuildSteps { get; set; }

  [XmlElement("resourceStrings")]
  public ResourceStrings ResourceStrings { get; set; }
  
}

#pragma warning restore CS8618