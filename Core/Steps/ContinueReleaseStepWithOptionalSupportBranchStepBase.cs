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

using Remotion.ReleaseProcessAutomation.Configuration.Data;
using Remotion.ReleaseProcessAutomation.Git;
using Remotion.ReleaseProcessAutomation.Jira;
using Remotion.ReleaseProcessAutomation.ReadInput;
using Remotion.ReleaseProcessAutomation.Scripting;
using Remotion.ReleaseProcessAutomation.SemanticVersioning;
using Spectre.Console;

namespace Remotion.ReleaseProcessAutomation.Steps;

public class ContinueReleaseStepWithOptionalSupportBranchStepBase
    : ReleaseProcessStepBase
{
  private readonly IMSBuildCallAndCommit _msBuildCallAndCommit;
  private readonly IJiraVersionCreator _jiraVersionCreator;

  protected ContinueReleaseStepWithOptionalSupportBranchStepBase (IGitClient gitClient, Config config, IInputReader inputReader, IAnsiConsole console, IMSBuildCallAndCommit msBuildCallAndCommit, IJiraVersionCreator jiraVersionCreator)
      : base(gitClient, config, inputReader, console)
  {
    _msBuildCallAndCommit = msBuildCallAndCommit;
    _jiraVersionCreator = jiraVersionCreator;
  }

  protected void CreateSupportBranchWithHotfixForRelease (SemanticVersion nextHotfixVersion, bool noPush)
  {
    Console.WriteLine("Do you wish to create a new support branch?");
    if (!InputReader.ReadConfirmation())
      return;

    var supportBranchName = $"support/v{nextHotfixVersion.Major}.{nextHotfixVersion.Minor}";
    var hotfixBranchName = $"hotfix/v{nextHotfixVersion}";
    GitClient.CheckoutNewBranch(supportBranchName);
    GitClient.CheckoutNewBranch(hotfixBranchName);

    _msBuildCallAndCommit.CallMSBuildStepsAndCommit(MSBuildMode.DevelopmentForNextRelease, nextHotfixVersion);
    _jiraVersionCreator.CreateNewVersionWithVersionNumber(nextHotfixVersion.ToString());

    if (noPush)
      return;

    var remoteNames = Config.RemoteRepositories.RemoteNames;
    GitClient.PushToRepos(remoteNames, supportBranchName);
    GitClient.PushToRepos(remoteNames, hotfixBranchName);
  }
}