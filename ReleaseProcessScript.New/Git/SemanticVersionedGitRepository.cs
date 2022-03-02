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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using ReleaseProcessScript.New.ReadInput;
using ReleaseProcessScript.New.SemanticVersioning;
using Serilog;

namespace ReleaseProcessScript.New.Git;

public class SemanticVersionedGitRepository
    : ISemanticVersionedGitRepository
{
  private readonly IGitClient _gitClient;
  private readonly ILogger _log = Log.ForContext<SemanticVersionedGitRepository>();
  public SemanticVersionedGitRepository (IGitClient gitClient)
  {
    _gitClient = gitClient;
  }

  public IReadOnlyList<SemanticVersion> GetVersionsSorted (string from = "HEAD", string to = "")
  {
    _log.Debug("Trying to get versions sorted from '{From}' to '{To}'", from, to);

    var parser = new SemanticVersionParser();
    var allVersions = _gitClient.GetTags(from, to);
    var validVersions = allVersions
        .Where(version => version.StartsWith('v'))
        .Where(version => parser.IsSemver(version.Substring(1)));

    var validParsedVersions = validVersions.Select(version => parser.ParseVersion(version.Substring(1)));

    return validParsedVersions.OrderByDescending(i => i).ToList();
  }

  public bool TryGetCurrentVersion ([MaybeNullWhen(false)] out SemanticVersion version, string from = "HEAD", string to = "")
  {
    _log.Debug("Trying to get first version from '{From}' to '{To}'", from, to);

    var validVersions = GetVersionsSorted(from, to);
    version = validVersions.FirstOrDefault();

    return version != null;
  }

  public SemanticVersion GetMostRecentHotfixVersion ()
  {
    _log.Debug("Trying to get most recent hotfix version");

    var currentBranchName = _gitClient.GetCurrentBranchName();
    if (string.IsNullOrEmpty(currentBranchName))
    {
      const string message = "Could not find the current branch while trying to get next hotfix version";
      _log.Error(message);
      throw new InvalidOperationException(message);
    }

    var currentVersion = new SemanticVersionParser().ParseVersionFromBranchName(currentBranchName);
    var supportVersion = $"support/v{currentVersion.Major}.{currentVersion.Minor}";

    if (TryGetCurrentVersion(out var mostRecentVersion, "HEAD", supportVersion))
      return mostRecentVersion.Pre != null ? mostRecentVersion : currentVersion;

    return currentVersion;
  }
}
