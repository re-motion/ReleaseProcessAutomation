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
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using Serilog;

namespace ReleaseProcessAutomation.SemanticVersioning;

public class SemanticVersionParser
{
  [RegexPattern]
  private readonly string _versionPattern = @"^(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)(-(?<pre>alpha|beta|rc)\.(?<preversion>\d+))?$";
  private readonly ILogger _log = Log.ForContext<SemanticVersionParser>();

  public SemanticVersion ParseVersion (string version)
  {
    if (!Regex.IsMatch(version, _versionPattern, RegexOptions.Multiline))
    {
      throw new ArgumentException("Version has an invalid format. Expected equivalent to '1.2.3' or '1.2.3-alpha.4'");
    }

    return ParseVersionInternal(version);
  }

  public SemanticVersion ParseVersionFromBranchName (string branchName)
  {
    _log.Debug("Parsing version branch '{BranchName}'", branchName);

    var splitBranchName = branchName.Split("/v", StringSplitOptions.RemoveEmptyEntries);
    if (splitBranchName.Length != 2)
    {
      var message = $"Could not parse version from branch name '{branchName}' because it is not in a valid format. Expected equivalent to 'release/v1.2.3'";
      throw new InvalidOperationException(message);
    }

    return ParseVersion(splitBranchName[1]);
  }

  public bool TryParseVersion (string version, [MaybeNullWhen(false)] out SemanticVersion output)
  {
    _log.Debug("Parsing version string '{Version}'", version);

    if (!Regex.IsMatch(version, _versionPattern, RegexOptions.Multiline))
    {
      _log.Warning("'{Version}' is not a valid semantic version", version);
      output = null;
      return false;
    }

    var parsedVersion = ParseVersionInternal(version);
    output = parsedVersion;
    return true;
  }

  public bool IsSemver (string version) => Regex.IsMatch(version, _versionPattern, RegexOptions.Multiline);

  private SemanticVersion ParseVersionInternal (string version)
  {
    var semanticVersion = new SemanticVersion();

    var match = Regex.Match(version, _versionPattern);
    semanticVersion.Major = int.Parse(match.Groups["major"].ToString());
    semanticVersion.Minor = int.Parse(match.Groups["minor"].ToString());
    semanticVersion.Patch = int.Parse(match.Groups["patch"].ToString());

    if (!match.Groups["pre"].Success)
      return semanticVersion;

    Enum.TryParse(match.Groups["pre"].ToString(), out PreReleaseStage preReleaseStage);
    semanticVersion.Pre = preReleaseStage;

    semanticVersion.PreReleaseCounter = int.Parse(match.Groups["preversion"].ToString());

    return semanticVersion;
  }
}
