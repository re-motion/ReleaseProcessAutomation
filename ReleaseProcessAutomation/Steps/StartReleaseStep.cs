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
using ReleaseProcessAutomation.Git;
using ReleaseProcessAutomation.Steps.PipelineSteps;
using Serilog;
using Spectre.Console;

namespace ReleaseProcessAutomation.Steps;

/// <summary>
///   Called by new-version and new-release-branch.
///   Has no specific prerequisites other than:
///   the config being properly set,
///   currently being on a hotfix, develop, master or release branch.
///   Checks the commit hash so the further steps don't have to.
///   Determines the next steps based on the current branch.
///   ---!!! the behaviour is outdated when called from a master branch !!!---
/// </summary>
public interface IStartReleaseStep
{
  void Execute (string? commitHash, bool pauseForCommit = false, bool noPush = false, bool startReleasePhase = false);
}

/// <inheritdoc cref="IStartReleaseStep" />
public class StartReleaseStep
    : IStartReleaseStep
{
  private readonly IBranchFromDevelopStep _branchFromDevelopStep;
  private readonly IBranchFromHotfixStep _branchFromHotfixStep;
  private readonly IBranchFromMasterStep _branchFromMasterStep;
  private readonly IBranchFromReleaseStep _branchFromReleaseStep;
  private readonly IGitClient _gitClient;
  private readonly IAnsiConsole _console;
  private readonly ILogger _log = Log.ForContext<StartReleaseStep>();

  public StartReleaseStep (
      IGitClient gitClient,
      IAnsiConsole console,
      IBranchFromDevelopStep branchFromDevelopStep,
      IBranchFromReleaseStep branchFromReleaseStep,
      IBranchFromHotfixStep branchFromHotfixStep,
      IBranchFromMasterStep branchFromMasterStep)
  {
    _gitClient = gitClient;
    _console = console;
    _branchFromDevelopStep = branchFromDevelopStep;
    _branchFromReleaseStep = branchFromReleaseStep;
    _branchFromHotfixStep = branchFromHotfixStep;
    _branchFromMasterStep = branchFromMasterStep;
  }

  public void Execute (string? commitHash, bool pauseForCommit = false, bool noPush = false, bool startReleasePhase = false)
  {
    if (commitHash != null && !_gitClient.IsCommitHash(commitHash))
    {
      const string message = "The given commit hash was not found in the repository";
      throw new ArgumentException(message);
    }

    if (_gitClient.IsOnBranch("release/"))
    {
      _log.Debug("On branch 'release', calling branch from release");
      _branchFromReleaseStep.Execute(commitHash, pauseForCommit, noPush);
    }
    else
    {
      if(!startReleasePhase)
        _console.WriteLine(
            "As you are not on a release branch, you won't be able to release a release candidate version.\nTo create a release branch, use the command [green]'New-Release-Branch'[/]");
      
      if (_gitClient.IsOnBranch("hotfix/"))
      {
        _log.Debug("On branch 'hotfix', calling branch from hotfix");
        _branchFromHotfixStep.Execute(commitHash, startReleasePhase, pauseForCommit, noPush);
      }
      else if (_gitClient.IsOnBranch("develop"))
      {      
        _log.Debug("On branch 'develop', calling branch from develop");
        _branchFromDevelopStep.Execute(commitHash, pauseForCommit, noPush, startReleasePhase);
      }
      else if (_gitClient.IsOnBranch("master"))
      {
        _log.Debug("On branch 'master', calling branch from master");
        _branchFromMasterStep.Execute(commitHash, startReleasePhase, pauseForCommit, noPush);
      }
      else
      {
        const string message = "You have to be on either a 'hotfix/*' or 'release/*' or 'develop' or 'master' branch to release a version";
        throw new InvalidOperationException(message);
      }
    }
  }
}
