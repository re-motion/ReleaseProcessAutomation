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
using NUnit.Framework;

namespace ReleaseProcessAutomation.Tests.IntegrationTests;

[TestFixture]
internal class ReleaseFromHotfixTests : IntegrationTestSetup
{
  [Test]
  public void ReleaseAlphaBeta_FromHotfix_MergeNewPreReleaseIntoHotfix ()
  {
    var correctLogs =
        @"*    (HEAD -> hotfix/v1.1.1-alpha.1, origin/hotfix/v1.1.1-alpha.1)Merge branch 'prerelease/v1.1.1-alpha.2' into hotfix/v1.1.1-alpha.1
          |\  
          | *  (tag: v1.1.1-alpha.2, origin/prerelease/v1.1.1-alpha.2, prerelease/v1.1.1-alpha.2)Update metadata to version '1.1.1-alpha.2'.
          |/  
          *  (support/v1.1, master)ConfigAndBuildProject
          *  (origin/master)Initial CommitAll
          ";
    
    ExecuteGitCommand("checkout -b support/v1.1");
    ExecuteGitCommand("checkout -b hotfix/v1.1.1-alpha.1");
    ExecuteGitCommand("commit -m Commit on hotfix --allow-empty");
    
    //Get release version from user
    TestConsole.Input.PushTextWithEnter("1.1.1-alpha.2");
    //Get next release version from user for jira
    TestConsole.Input.PushTextWithEnter("1.1.2");
    

    var act = Program.Main(new[] { "Release-Version"});
    
    AssertValidLogs(correctLogs);
    Assert.That(act, Is.EqualTo(0));
  }
  
  
  [Test]
  public void ReleaseAlphaBeta_FromHotfix_WithAdditionalCommitAndContinue ()
  {
    var correctLogs =
        @"*    (HEAD -> hotfix/v1.1.1-alpha.1)Merge branch 'prerelease/v1.1.1-beta.1' into hotfix/v1.1.1-alpha.1
          |\  
          | *  (tag: v1.1.1-beta.1, origin/prerelease/v1.1.1-beta.1, prerelease/v1.1.1-beta.1)Commit on prerelease branch
          | * Update metadata to version '1.1.1-beta.1'.
          |/  
          *  (support/v1.1, master)ConfigAndBuildProject
          *  (origin/master)Initial CommitAll
          ";

    ExecuteGitCommand("checkout -b support/v1.1");
    ExecuteGitCommand("checkout -b hotfix/v1.1.1-alpha.1");

    //Get release version from user
    TestConsole.Input.PushTextWithEnter("1.1.1-beta.1");
    //Get next release version from user for jira
    TestConsole.Input.PushTextWithEnter("1.1.1-beta.2");
    
    var act1 = Program.Main(new[] { "Release-Version", "-p" });
    ExecuteGitCommand("commit -m \"Commit on prerelease branch\" --allow-empty");
    var act2 = Program.Main(new[] { "Close-Version"});
    
    AssertValidLogs(correctLogs);
    Assert.That(act1, Is.EqualTo(0));
    Assert.That(act2, Is.EqualTo(0));

  }

  [Test]
  public void ReleaseNewPatch_FromHotfix_ToSupport ()
  {
    var correctLogs =
        @"*  (hotfix/v1.1.2)Update metadata to version '1.1.2'.
          *    (HEAD -> support/v1.1, tag: v1.1.1, origin/support/v1.1)Merge branch 'release/v1.1.1' into support/v1.1
          |\  
          | *  (origin/release/v1.1.1, release/v1.1.1)Update metadata to version '1.1.1'.
          |/  
          *  (master, hotfix/v1.1.1)ConfigAndBuildProject
          *  (origin/master)Initial CommitAll
          ";

    correctLogs = correctLogs.Replace(" ", "").Replace("\r", "");
    ExecuteGitCommand("checkout -b support/v1.1");
    ExecuteGitCommand("checkout -b hotfix/v1.1.1");
    ExecuteGitCommand("commit -m Commit on hotfix --allow-empty");

    //Get release version from user
    TestConsole.Input.PushTextWithEnter("1.1.1");
    //Get next release version from user for jira
    TestConsole.Input.PushTextWithEnter("1.1.2");

    var act = Program.Main(new[] { "Release-Version" });

    AssertValidLogs(correctLogs);
    Assert.That(act, Is.EqualTo(0));
  }

  [Test]
  public void ReleaseNewBranch_FromHotfix ()
  {
    var correctLogs =
        @"*  (HEAD -> release/v1.2.1, master, hotfix/v1.2.1)ConfigAndBuildProject
          *  (origin/master)Initial CommitAll
          ";

    ExecuteGitCommand("checkout -b hotfix/v1.2.1");
    ExecuteGitCommand("commit -m Commit on hotfix --allow-empty");
    
    //Get release version from user
    TestConsole.Input.PushTextWithEnter("1.2.2");
    //Get next release version from user for jira
    TestConsole.Input.PushTextWithEnter("1.3.0");

    var act1 = Program.Main(new[] { "New-Release-Branch" });

    AssertValidLogs(correctLogs);   
    Assert.That(act1, Is.EqualTo(0));
  }

  [Test]
  public void ReleaseNewBranchWithGivenCommit_FromHotfix_ReleaseBranchOnGivenCommit ()
  {
    var correctLogs = 
        @"*  (HEAD -> release/v1.2.1) Commit afterwards
          | *  (hotfix/v1.2.1) Commit on hotfix
          |/
          *  Commit for release
          *  (master) ConfigAndBuildProject
          *  (origin/master) Initial CommitAll
          ";

    ExecuteGitCommand("checkout -b hotfix/v1.2.1");
    ExecuteGitCommand("commit -m \"Commit for release\" --allow-empty");
    var releaseCommit = ExecuteGitCommandWithOutput("log -1 --pretty=%H");
    ExecuteGitCommand("commit -m \"Commit on hotfix\" --allow-empty");
    
    //Get release version from user
    TestConsole.Input.PushTextWithEnter("1.2.2");
    
    var logs = ExecuteGitCommandWithOutput("log --all --graph --oneline --decorate --pretty=%d%s");

    var act1 = Program.Main(new[] { "New-Release-Branch" , $"-c {releaseCommit}"});
    
    ExecuteGitCommand("commit -m \"Commit afterwards\" --allow-empty");
    AssertValidLogs(correctLogs);   
    Assert.That(act1, Is.EqualTo(0));
  }
  
  [Test]
  public void ReleaseToSupport_WithPauseAndCommitAndCloseVersion_FinishesSuccessfully ()
  {
    var correctLogs = 
        @"*  (hotfix/v1.2.2)Update metadata to version '1.2.2'.
          *    (HEAD -> support/v1.2, tag: v1.2.1, origin/support/v1.2) Merge branch 'release/v1.2.1' into support/v1.2
          |\  
          | *  (origin/release/v1.2.1, release/v1.2.1)Update metadata to version '1.2.1'.
          | *  (hotfix/v1.2.1)Commit on hotfix
          |/  
          *  (tag: v1.2.0, master)ConfigAndBuildProject
          *  (origin/master)Initial CommitAll
          ";

    ExecuteGitCommand("checkout -b support/v1.2");
    ExecuteGitCommand("tag v1.2.0");
    ExecuteGitCommand("checkout -b hotfix/v1.2.1");
    ExecuteGitCommand("commit -m \"Commit on hotfix\" --allow-empty");
    
    //Get release version from user
    TestConsole.Input.PushTextWithEnter("1.2.1");
    //Get next release version from user for jira
    TestConsole.Input.PushTextWithEnter("1.2.2");
    
    var act1 = Program.Main(new[] { "Release-Version" , "-p"});
    
    //Get ancestor version from user
    TestConsole.Input.PushTextWithEnter("hotfix/v1.2.1");
    
    var act2 = Program.Main(new[] { "Close-Version" });
    
    AssertValidLogs(correctLogs);   
    Assert.That(act1, Is.EqualTo(0));
    Assert.That(act2, Is.EqualTo(0));
  }
}