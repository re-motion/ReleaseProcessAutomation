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
using Spectre.Console;
using Spectre.Console.Testing;

namespace ReleaseProcessAutomation.Tests.IntegrationTests;

[TestFixture]
internal class ReleaseFromDevelopTests : IntegrationTestSetup
{
  [Test]
  public void ReleaseOnMaster_FromDevelop_ReleasesNewMinor ()
  {
    var correctLogs =
        @"*    (tag: v1.0.0, origin/master, master)Merge branch 'release/v1.0.0'
          |\  
          | *  (origin/release/v1.0.0, release/v1.0.0)Update metadata to version '1.0.0'.
          | | *  (HEAD -> develop, origin/develop)Update metadata to version '1.1.0'.
          | |/  
          | * feature4
          | * feature3
          | * feature2
          |/  
          * feature
          * ConfigAndBuildProject
          * Initial CommitAll
          ";

    ExecuteGitCommand("commit -m feature --allow-empty");
    ExecuteGitCommand("checkout -b develop");

    ExecuteGitCommand("commit -m feature2 --allow-empty");
    ExecuteGitCommand("commit -m feature3 --allow-empty");
    ExecuteGitCommand("commit -m feature4 --allow-empty");

    //Get release version from user
    TestConsole.Input.PushTextWithEnter("1.0.0");
    //Get next release version from user for jira
    TestConsole.Input.PushTextWithEnter("1.1.0");
    //Do not want to create support branch
    TestConsole.Input.PushTextWithEnter("n");

    var act = Program.Main(new[] { "Release-Version" });

    AssertValidLogs(correctLogs);
    Assert.That(act, Is.EqualTo(0));
  }

  [Test]
  public void ReleaseOnMaster_FromDevelop_WithPreviousRelease ()
  {
    var correctLogs =
        @"*    (tag: v2.0.0, origin/master, master)Merge branch 'release/v2.0.0'
          |\  
          | *  (origin/release/v2.0.0, release/v2.0.0)Update metadata to version '2.0.0'.
          | | *  (HEAD -> develop, origin/develop)Update metadata to version '2.1.0'.
          | |/  
          | * feature4
          | * feature3
          | * feature2
          |/  
          *  (tag: v1.28.3)feature
          * ConfigAndBuildProject
          * Initial CommitAll
          ";

    ExecuteGitCommand("commit -m feature --allow-empty");
    ExecuteGitCommand("tag v1.28.3");
    ExecuteGitCommand("checkout -b develop");

    ExecuteGitCommand("commit -m feature2 --allow-empty");
    ExecuteGitCommand("commit -m feature3 --allow-empty");
    ExecuteGitCommand("commit -m feature4 --allow-empty");

    //Get release version from user
    TestConsole.Input.PushTextWithEnter("2.0.0");
    //Get next release version from user for jira
    TestConsole.Input.PushTextWithEnter("2.1.0");
    //Do not want to create support branch
    TestConsole.Input.PushTextWithEnter("n");

    var act = Program.Main(new[] { "Release-Version", "-n=false", "-p=false" });

    AssertValidLogs(correctLogs);
    Assert.That(act, Is.EqualTo(0));
  }

  [Test]
  public void ReleaseOnMaster_FromDevelop_WithoutPush ()
  {
    var correctLogs =
        @"*    (tag: v2.0.0, master)Merge branch 'release/v2.0.0'
          |\  
          | *  (release/v2.0.0)Update metadata to version '2.0.0'.
          | | *  (HEAD -> develop)Update metadata to version '2.1.0'.
          | |/  
          | * feature4
          | * feature3
          | * feature2
          |/  
          *  (tag: v1.28.3)feature
          * ConfigAndBuildProject
          *  (origin/master)Initial CommitAll
          ";
    correctLogs = correctLogs.Replace(" ", "").Replace("\r", "");

    ExecuteGitCommand("commit -m feature --allow-empty");
    ExecuteGitCommand("tag v1.28.3");
    ExecuteGitCommand("checkout -b develop");

    ExecuteGitCommand("commit -m feature2 --allow-empty");
    ExecuteGitCommand("commit -m feature3 --allow-empty");
    ExecuteGitCommand("commit -m feature4 --allow-empty");

    //Get release version from user
    TestConsole.Input.PushTextWithEnter("2.0.0");
    //Get next release version from user for jira
    TestConsole.Input.PushTextWithEnter("2.1.0");
    //Do not want to create support branch
    TestConsole.Input.PushTextWithEnter("n");

    var act = Program.Main(new[] { "Release-Version", "-n" });

    AssertValidLogs(correctLogs);
    Assert.That(act, Is.EqualTo(0));
  }

  [Test]
  public void ReleaseOnMaster_FromDevelop_CommitButNoMergeAndTag ()
  {
    var correctLogs1 =
        @"*  (HEAD -> release/v2.0.0)Update metadata to version '2.0.0'.
          | *  (develop)Update metadata to version '2.1.0'.
          |/  
          * feature4
          * feature3
          * feature2
          *  (tag: v1.28.3, master)feature
          * ConfigAndBuildProject
          *  (origin/master)Initial CommitAll
          ";
    var correctLogs2 =
        @"*  (develop)Update metadata to version '2.1.0'.
          | *  (HEAD -> release/v2.0.0)Update metadata to version '2.0.0'.
          |/  
          * feature4
          * feature3
          * feature2
          *  (tag: v1.28.3, master)feature
          * ConfigAndBuildProject
          *  (origin/master)Initial CommitAll
          ";

    ExecuteGitCommand("commit -m feature --allow-empty");
    ExecuteGitCommand("tag v1.28.3");
    ExecuteGitCommand("checkout -b develop");

    ExecuteGitCommand("commit -m feature2 --allow-empty");
    ExecuteGitCommand("commit -m feature3 --allow-empty");
    ExecuteGitCommand("commit -m feature4 --allow-empty");

    TestConsole.Interactive();
    //Get release version from user
    TestConsole.Input.PushKey(ConsoleKey.DownArrow);
    TestConsole.Input.PushKey(ConsoleKey.DownArrow);
    TestConsole.Input.PushKey(ConsoleKey.DownArrow);
    TestConsole.Input.PushKey(ConsoleKey.DownArrow);
    TestConsole.Input.PushKey(ConsoleKey.DownArrow);
    TestConsole.Input.PushKey(ConsoleKey.Enter);

    //Get next release version from user for jira
    TestConsole.Input.PushKey(ConsoleKey.DownArrow);
    TestConsole.Input.PushKey(ConsoleKey.DownArrow);
    TestConsole.Input.PushKey(ConsoleKey.Enter);

    var act = Program.Main(new[] { "Release-Version", "-n=false", "-p=true" });

    AssertValidLogs(correctLogs1, correctLogs2);
    Assert.That(act, Is.EqualTo(0));
  }

  [Test]
  public void ReleaseAlphaBeta_FromDevelop_ReleasePreRelease ()
  {
    var correctLogs =
        @"*    (HEAD -> develop, origin/develop)Merge branch 'prerelease/v1.0.0-alpha.1' into develop
          |\  
          | *  (tag: v1.0.0-alpha.1, origin/prerelease/v1.0.0-alpha.1, prerelease/v1.0.0-alpha.1)Update metadata to version '1.0.0-alpha.1'.
          |/  
          * feature4
          * feature3
          * feature2
          *  (master)feature
          * ConfigAndBuildProject
          *  (origin/master)Initial CommitAll
          ";

    ExecuteGitCommand("commit -m feature --allow-empty");
    ExecuteGitCommand("checkout -b develop");

    ExecuteGitCommand("commit -m feature2 --allow-empty");
    ExecuteGitCommand("commit -m feature3 --allow-empty");
    ExecuteGitCommand("commit -m feature4 --allow-empty");

    //Get release version from user
    TestConsole.Input.PushTextWithEnter("1.0.0-alpha.1");
    //Get next release version from user for jira
    TestConsole.Input.PushTextWithEnter("1.0.0-alpha.2");

    var act = Program.Main(new[] { "Release-Version" });

    AssertValidLogs(correctLogs);
    Assert.That(act, Is.EqualTo(0));
  }

  [Test]
  public void ReleasePreRelease_FromDevelop_RoReleaseWithCommitOnPreRelease ()
  {
    var correctLogs =
        @"*    (HEAD -> develop)Merge branch 'prerelease/v1.2.0-alpha.1' into develop
          |\  
          | *  (tag: v1.2.0-alpha.1, origin/prerelease/v1.2.0-alpha.1, prerelease/v1.2.0-alpha.1)Commit on prerelease branch
          | * Update metadata to version '1.2.0-alpha.1'.
          |/  
          *  (master)ConfigAndBuildProject
          *  (origin/master)Initial CommitAll
          ";

    ExecuteGitCommand("checkout -b develop");
    ExecuteGitCommand("commit -m Commit on develop --allow-empty");

    //Get release version from user
    TestConsole.Input.PushTextWithEnter("1.2.0-alpha.1");
    //Get next release version from user for jira
    TestConsole.Input.PushTextWithEnter("1.2.0");

    var act1 = Program.Main(new[] { "Release-Version", "-p" });
    ExecuteGitCommand("commit -m \"Commit on prerelease branch\" --allow-empty");
    var act2 = Program.Main(new[] { "Close-Version" });

    AssertValidLogs(correctLogs);
    Assert.That(act1, Is.EqualTo(0));
    Assert.That(act2, Is.EqualTo(0));
  }

  [Test]
  public void ReleaseRelease_FromDevelop_ToMasterWithCommitOnRelease ()
  {
    var correctLogs =
        @"*    (tag: v1.2.0, origin/master, master)Merge branch 'release/v1.2.0'
          |\  
          | *  (origin/release/v1.2.0, release/v1.2.0)Commit on release branch
          | * Update metadata to version '1.2.0'.
          |/  
          | *  (HEAD -> develop, origin/develop)Update metadata to version '1.3.0'.
          |/  
          * ConfigAndBuildProject
          * Initial CommitAll
          ";
    ExecuteGitCommand("checkout -b develop");
    ExecuteGitCommand("commit -m Commit on develop --allow-empty");

    //Get release version from user
    TestConsole.Input.PushTextWithEnter("1.2.0");
    //Get next release version from user for jira
    TestConsole.Input.PushTextWithEnter("1.3.0");
    //Do not want to create support branch
    TestConsole.Input.PushTextWithEnter("n");

    var act1 = Program.Main(new[] { "Release-Version", "-p" });
    ExecuteGitCommand("commit -m \"Commit on release branch\" --allow-empty");
    var act2 = Program.Main(new[] { "Close-Version" });

    AssertValidLogs(correctLogs);
    Assert.That(act1, Is.EqualTo(0));
    Assert.That(act2, Is.EqualTo(0));
  }

  [Test]
  public void ReleaseRelease_FromDevelop_ToMasterWithCommitHash ()
  {
    var correctLogs =
        @"*  (HEAD -> develop)Commit after finishing on new branch
          *  (origin/develop)Update metadata to version '1.3.0'.
          * Commit after branch
          | *    (tag: v1.2.0, origin/master, master)Merge branch 'release/v1.2.0'
          | |\  
          | | *  (origin/release/v1.2.0, release/v1.2.0)Update metadata to version '1.2.0'.
          | |/  
          |/|   
          * | Commit for release
          |/  
          * ConfigAndBuildProject
          * Initial CommitAll
          ";

    ExecuteGitCommand("checkout -b develop");
    ExecuteGitCommand("commit -m \"Commit for release\" --allow-empty");
    var releaseCommit = ExecuteGitCommandWithOutput("log -1 --pretty=%H");
    ExecuteGitCommand("commit -m \"Commit after branch\" --allow-empty");

    //Get release version from user
    TestConsole.Input.PushTextWithEnter("1.2.0");
    //Get next release version from user for jira
    TestConsole.Input.PushTextWithEnter("1.3.0");
    //Do not want to create support branch
    TestConsole.Input.PushTextWithEnter("n");

    var act1 = Program.Main(new[] { "Release-Version", $"-c {releaseCommit}" });

    ExecuteGitCommand("commit -m \"Commit after finishing on new branch\" --allow-empty");
    AssertValidLogs(correctLogs);
    Assert.That(act1, Is.EqualTo(0));
  }

  [Test]
  public void CloseVersionWithAncestor_FromDevelopRelease_ToMaster ()
  {
    var correctLogs =
        @"*    (tag: v1.2.0, origin/master, master)Merge branch 'release/v1.2.0'
          |\  
          | *  (origin/release/v1.2.0, release/v1.2.0)Commit on release branch
          | * Update metadata to version '1.2.0'.
          |/  
          | *  (HEAD -> develop, origin/develop)Update metadata to version '1.3.0'.
          |/  
          * ConfigAndBuildProject
          * Initial CommitAll
          ";
    ExecuteGitCommand("checkout -b develop");
    ExecuteGitCommand("commit -m Commit on develop --allow-empty");

    //Get release version from user
    TestConsole.Input.PushTextWithEnter("1.2.0");
    //Get next release version from user for jira
    TestConsole.Input.PushTextWithEnter("1.3.0");
    //Do not want to create support branch
    TestConsole.Input.PushTextWithEnter("n");

    var act1 = Program.Main(new[] { "Release-Version", "-p" });
    ExecuteGitCommand("commit -m \"Commit on release branch\" --allow-empty");
    var act2 = Program.Main(new[] { "Close-Version", "-a develop" });

    AssertValidLogs(correctLogs);
    Assert.That(act1, Is.EqualTo(0));
    Assert.That(act2, Is.EqualTo(0));
  }

  [Test]
  public void CloseVersionWithNoPush_FromDevelopRelease_ToMasterWithCommitOnRelease ()
  {
    var correctLogs =
        @"*    (tag: v1.2.0, master)Merge branch 'release/v1.2.0'
          |\  
          | *  (release/v1.2.0)Commit on release branch
          | * Update metadata to version '1.2.0'.
          |/  
          | *  (HEAD -> develop)Update metadata to version '1.3.0'.
          |/  
          * ConfigAndBuildProject
          *  (origin/master)Initial CommitAll
          ";

    ExecuteGitCommand("checkout -b develop");
    ExecuteGitCommand("commit -m Commit on develop --allow-empty");

    //Get release version from user
    TestConsole.Input.PushTextWithEnter("1.2.0");
    //Get next release version from user for jira
    TestConsole.Input.PushTextWithEnter("1.3.0");
    //Do not want to create support branch
    TestConsole.Input.PushTextWithEnter("n");

    var act1 = Program.Main(new[] { "Release-Version", "-p" });
    ExecuteGitCommand("commit -m \"Commit on release branch\" --allow-empty");
    var logs = ExecuteGitCommandWithOutput("log --all --graph --oneline --decorate --pretty=%d%s");
    logs = logs.Replace(" ", "");
    var act2 = Program.Main(new[] { "Close-Version", "--noPush" });

    AssertValidLogs(correctLogs);
    Assert.That(act1, Is.EqualTo(0));
    Assert.That(act2, Is.EqualTo(0));
  }

  [Test]
  public void ReleaseNewBranch_FromDevelop ()
  {
    var correctLogs =
        @"*  (develop)Update metadata to version '1.3.0'.
          *  (HEAD -> release/v1.2.0, master)ConfigAndBuildProject
          *  (origin/master)Initial CommitAll
          ";

    ExecuteGitCommand("checkout -b develop");
    ExecuteGitCommand("commit -m Commit on develop --allow-empty");

    //Get release version from user
    TestConsole.Input.PushTextWithEnter("1.2.0");
    //Get next release version from user for jira
    TestConsole.Input.PushTextWithEnter("1.3.0");

    var act1 = Program.Main(new[] { "New-Release-Branch" });

    AssertValidLogs(correctLogs);
    Assert.That(TestConsole.Output, Does.Contain("Called UpdateAssemblyInfosForRelease!"));
    Assert.That(TestConsole.Output, Does.Not.Contain("Called UpdateAssemblyInfosForDevelopment!"));
    Assert.That(act1, Is.EqualTo(0));
  }
}