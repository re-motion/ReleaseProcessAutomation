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
using System.IO;
using System.Xml.Serialization;
using ReleaseProcessAutomation.Configuration.Data;
using Serilog;

namespace ReleaseProcessAutomation.Configuration;

public class ConfigReader
{
  private const string c_buildProjectFileName = ".BuildProject";
#pragma warning disable CS8618
  [XmlRoot("configFile")]
  public class ConfigFile
  {
    [XmlElement("path")]
    public string Path { get; set; }
  }
#pragma warning restore CS8618

  private readonly ILogger _log = Log.ForContext<ConfigReader>();

  /// <exception cref="FileNotFoundException">The file could not be found.</exception>
  /// <exception cref="InvalidOperationException">The file is not in the correct format.</exception>
  public Config LoadConfig (string configPath)
  {
    _log.Debug("Loading Config from '{ConfigPath}'", configPath);

    if (!File.Exists(configPath))
    {
      var message = $"Could not Load Config from '{configPath}' because the file does not exist";
      throw new FileNotFoundException(message);
    }

    using TextReader reader = new StreamReader(configPath);
    var serializer = new XmlSerializer(typeof(Config));

    var config = (Config?) serializer.Deserialize(reader);
    if (config == null)
    {
      const string message = "Could not deserialize config, please check your config settings format";
      throw new InvalidOperationException(message);
    }

    return config;
  }

  /// <exception cref="FileNotFoundException">The file could not be found.</exception>
  /// <exception cref="InvalidOperationException">The file is not in the correct format.</exception>
  public string GetConfigPathFromBuildProject (string solutionRoot)
  {
    _log.Debug("Getting Config Path from '" + c_buildProjectFileName + "' file in the solution root '{SolutionRoot}'", solutionRoot);
    var path = Path.Combine(solutionRoot, c_buildProjectFileName);

    if (!File.Exists(path))
    {
      var message = $"Could not get Config path from '{c_buildProjectFileName}' because the file '{path}' does not exist";
      throw new FileNotFoundException(message);
    }

    using TextReader reader = new StreamReader(path);
    var serializer = new XmlSerializer(typeof(ConfigFile));

    var s = ((ConfigFile?) serializer.Deserialize(reader))?.Path;
    if (s == null)
    {
      const string message = "Could not deserialize the '" + c_buildProjectFileName + "' file, please check the format of the file";
      throw new InvalidOperationException(message);
    }

    return s;
  }
}
