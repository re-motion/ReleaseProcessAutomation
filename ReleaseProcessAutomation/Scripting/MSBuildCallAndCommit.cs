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
using ReleaseProcessAutomation.Configuration.Data;
using ReleaseProcessAutomation.Extensions;
using ReleaseProcessAutomation.Git;
using ReleaseProcessAutomation.MSBuild;
using ReleaseProcessAutomation.SemanticVersioning;
using Serilog;
using Spectre.Console;

namespace ReleaseProcessAutomation.Scripting;

public interface IMSBuildCallAndCommit
{
  int CallMSBuildStepsAndCommit (MSBuildMode msBuildMode, SemanticVersion version);
}

public class MSBuildCallAndCommit
    : IMSBuildCallAndCommit
{
  private readonly Config _config;
  private readonly IAnsiConsole _console;
  private readonly IGitClient _gitClient;
  private readonly IMSBuild _msBuild;
  private readonly ILogger _log = Log.ForContext<MSBuildCallAndCommit>();

  public MSBuildCallAndCommit (IGitClient gitClient, Config config, IMSBuild msBuild, IAnsiConsole console)
  {
    _gitClient = gitClient;
    _config = config;
    _msBuild = msBuild;
    _console = console;
  }

  public int CallMSBuildStepsAndCommit (MSBuildMode msBuildMode, SemanticVersion version)
  {
    _log.Debug("Calling MSBuild steps");
    var msBuildPath = _config.MSBuildSettings.MSBuildPath;

    if (string.IsNullOrEmpty(msBuildPath))
    {
      _log.Warning("No MSBuild path specified in config, will continue without MSBuild.");
      _console.WriteLine("There was no MSBuild path specified in the config, will continue without invoking MSBuild.");
      return -1;
    }

    if (!File.Exists(msBuildPath))
    {
      _log.Warning("The configured MSBuild path '{1}' does not exist.", msBuildPath);
      _console.WriteLine($"The configured MSBuild path '{msBuildPath}' does not exist.\nPlease configure a proper MSBuild path in the config.\nWill continue without invoking MSBuild.");
      return -1;
    }

    var msBuildSteps = _config.GetMSBuildSteps(msBuildMode);

    foreach (var step in msBuildSteps.Steps)
    {
      var commitMessage = step.CommitMessage;
      if (!string.IsNullOrEmpty(commitMessage) && !_gitClient.IsWorkingDirectoryClean())
      {
        const string message = "Working directory not clean before a call to MSBuild.exe with a commit message defined in config.";
        throw new InvalidOperationException(message);
      }

      var msBuildCallArguments = MSBuildUtilities.GetMSBuildCallString(step, version, _console);

      if (msBuildCallArguments == null)
      {
        _log.Information("No MSBuild arguments available, skipping MSBuild execution.");
        continue;
      }

      _msBuild.CallMSBuild(msBuildPath, msBuildCallArguments);

      if (string.IsNullOrEmpty(commitMessage))
      {
        if (!_gitClient.IsWorkingDirectoryClean())
        {
          const string message = "Working directory not clean after call to MSBuild.exe without commit message. Check your targets in the config and make sure they do not create new files.";
          throw new InvalidOperationException(message);
        }
      }
      else
      {
        _log.Information("Committing MSBuild changes with message '{CommitMessage}'.", commitMessage);
        var versionString = version.ToString();
        var versionedCommitMessage = commitMessage.Replace("{version}", versionString).Replace("{Version}", versionString);
        _gitClient.AddAll();
        _gitClient.CommitAll(versionedCommitMessage);
        _gitClient.ResolveMergeConflicts();
      }
    }

    return 0;
  }
}