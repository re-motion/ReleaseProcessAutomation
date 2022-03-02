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
using ReleaseProcessScript.New.Configuration.Data;
using ReleaseProcessScript.New.Git;
using ReleaseProcessScript.New.ReadInput;
using ReleaseProcessScript.New.SemanticVersioning;
using Serilog;
using Spectre.Console;

namespace ReleaseProcessScript.New.Steps.PipelineSteps;

/// <summary>
///   Should only be called by ContinueMasterReleaseStep.
///   Pushes the changes made by previous steps.
/// </summary>
public interface IPushMasterReleaseStep
{
  void Execute (SemanticVersion nextVersion);
}

/// <inheritdoc cref="IPushMasterReleaseStep" />
public class PushMasterReleaseStep
    : ReleaseProcessStepBase, IPushMasterReleaseStep
{
  private readonly ILogger _log = Log.ForContext<PushMasterReleaseStep>();

  public PushMasterReleaseStep (
      IGitClient gitClient,
      Config config,
      IInputReader inputReader,
      IAnsiConsole console)
      : base(gitClient, config, inputReader, console)
  {
  }

  public void Execute (SemanticVersion nextVersion)
  {
    var branchName = $"release/v{nextVersion}";
    var tagName = $"v{nextVersion}";

    if (!GitClient.DoesBranchExist(branchName))
    {
      var message = $"The Branch {branchName} does not exist. Please create a release branch first.";
      _log.Error(message);
      throw new InvalidOperationException(message);
    }

    EnsureBranchUpToDate(branchName);
    var remoteNames = Config.RemoteRepositories.RemoteNames;
    GitClient.PushToRepos(remoteNames, branchName);

    EnsureBranchUpToDate("master");
    EnsureBranchUpToDate("develop");
    
    GitClient.PushToRepos(remoteNames, "master", tagName);
    GitClient.PushToRepos(remoteNames, "develop");
  }
}
