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
using ReleaseProcessAutomation.Extensions;
using ReleaseProcessAutomation.Git;
using ReleaseProcessAutomation.ReadInput;
using ReleaseProcessAutomation.Scripting;
using ReleaseProcessAutomation.SemanticVersioning;
using Spectre.Console;

namespace ReleaseProcessAutomation.Steps;

public class ContinueReleaseStepWithOptionalSupportBranchStepBase
    : ReleaseProcessStepBase
{
  private readonly IMSBuildCallAndCommit _msBuildCallAndCommit;

  protected ContinueReleaseStepWithOptionalSupportBranchStepBase (IGitClient gitClient, Config config, IInputReader inputReader, IAnsiConsole console, IMSBuildCallAndCommit msBuildCallAndCommit)
      : base(gitClient, config, inputReader, console)
  {
    _msBuildCallAndCommit = msBuildCallAndCommit;
  }

  protected void CreateSupportBranchWithHotfixForRelease (SemanticVersion nextHotfixVersion)
  {
    Console.WriteLine("Do you wish to create a new support branch?");
    if (!InputReader.ReadConfirmation())
      return;

    GitClient.CheckoutNewBranch($"support/v{nextHotfixVersion.Major}.{nextHotfixVersion.Minor}");
    GitClient.CheckoutNewBranch($"hotfix/v{nextHotfixVersion}");
    _msBuildCallAndCommit.CallMSBuildStepsAndCommit(MSBuildMode.DevelopmentForNextRelease, nextHotfixVersion);
  }
}