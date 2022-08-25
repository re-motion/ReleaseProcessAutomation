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
using ReleaseProcessAutomation.Configuration.Data;
using ReleaseProcessAutomation.Git;
using ReleaseProcessAutomation.ReadInput;
using ReleaseProcessAutomation.SemanticVersioning;
using Serilog;
using Spectre.Console;

namespace ReleaseProcessAutomation.Steps.PipelineSteps;

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
    : IPushMasterReleaseStep
{
  private readonly IGitClient _gitClient;
  private readonly Config _config;
  private readonly IGitBranchOperations _gitBranchOperations;

  public PushMasterReleaseStep (
      IGitClient gitClient,
      Config config,
      IGitBranchOperations gitBranchOperations)
  {
    _gitClient = gitClient;
    _config = config;
    _gitBranchOperations = gitBranchOperations;
  }

  public void Execute (SemanticVersion nextVersion)
  {
    var branchName = $"release/v{nextVersion}";
    var tagName = $"v{nextVersion}";

    if (!_gitClient.DoesBranchExist(branchName))
    {
      var message = $"The Branch '{branchName}' does not exist. Please create a release branch by using 'new-release-branch' first.";
      throw new InvalidOperationException(message);
    }

    _gitBranchOperations.EnsureBranchUpToDate(branchName);
    var remoteNames = _config.RemoteRepositories.RemoteNames;
    _gitClient.PushToRepos(remoteNames, branchName);

    _gitBranchOperations.EnsureBranchUpToDate("master");
    _gitBranchOperations.EnsureBranchUpToDate("develop");

    _gitClient.PushToRepos(remoteNames, "master", tagName);
    _gitClient.PushToRepos(remoteNames, "develop");
  }
}