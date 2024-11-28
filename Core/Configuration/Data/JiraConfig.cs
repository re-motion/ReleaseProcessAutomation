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

using System.Xml.Serialization;

#pragma warning disable CS8618

namespace Remotion.ReleaseProcessAutomation.Configuration.Data;

public class JiraConfig
{
  public bool UseBearer =>
      StringUseBearer.ToUpper() switch
        {
          "Y" => true,
          "YES" => true,
          "T" => true,
          "TRUE" => true,
          _ => false
        };

  [XmlElement("jiraUrl")]
  public string JiraURL { get; set; }

  [XmlElement("jiraProjectKey")]
  public string JiraProjectKey { get; set; }


  [XmlElement("useBearerAuth")]
  public string StringUseBearer { get; set; } = "false";
}