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
using NUnit.Framework;
using Spectre.Console.Testing;

namespace ReleaseProcessAutomation.IntegrationTests;

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

    var correctLogs1 =
        @"*  (HEAD -> develop, origin/develop)Update metadata to version '1.1.0'.
          | *    (tag: v1.0.0, origin/master, master)Merge branch 'release/v1.0.0'
          | |\  
          | | *  (origin/release/v1.0.0, release/v1.0.0)Update metadata to version '1.0.0'.
          | |/  
          |/|   
          * | feature4
          * | feature3
          * | feature2
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

    var act = RunProgram(new[] { "Release-Version" });

    Assert.That(act, Is.EqualTo(0));
    AssertValidLogs(correctLogs, correctLogs1);
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

    var correctLogs1 =
        @"*  (HEAD -> develop, origin/develop)Update metadata to version '2.1.0'.
          | *    (tag: v2.0.0, origin/master, master)Merge branch 'release/v2.0.0'
          | |\  
          | | *  (origin/release/v2.0.0, release/v2.0.0)Update metadata to version '2.0.0'.
          | |/  
          |/|   
          * | feature4
          * | feature3
          * | feature2
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

    var act = RunProgram(new[] { "Release-Version", "-n=false", "-p=false" });

    Assert.That(act, Is.EqualTo(0));
    AssertValidLogs(correctLogs, correctLogs1);
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

    var correctLogs1 =
        @"*  (HEAD -> develop)Update metadata to version '2.1.0'.
          | *    (tag: v2.0.0, master)Merge branch 'release/v2.0.0'
          | |\  
          | | *  (release/v2.0.0)Update metadata to version '2.0.0'.
          | |/  
          |/|   
          * | feature4
          * | feature3
          * | feature2
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

    var act = RunProgram(new[] { "Release-Version", "-n" });

    Assert.That(act, Is.EqualTo(0));
    AssertValidLogs(correctLogs, correctLogs1);
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

    var act = RunProgram(new[] { "Release-Version", "-n=false", "-p=true" });

    Assert.That(act, Is.EqualTo(0));
    AssertValidLogs(correctLogs1, correctLogs2);
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
    //Move the issues from 1.0.0 to 1.0.0-alpha.1
    TestConsole.Input.PushTextWithEnter("y");

    var act = RunProgram(new[] { "Release-Version" });

    Assert.That(act, Is.EqualTo(0));
    AssertValidLogs(correctLogs);
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
    //Move the issues from 1.0.0 to 1.0.0-alpha.1
    TestConsole.Input.PushTextWithEnter("y");

    var act1 = RunProgram(new[] { "Release-Version", "-p" });
    ExecuteGitCommand("commit -m \"Commit on prerelease branch\" --allow-empty");
    var act2 = RunProgram(new[] { "Close-Version" });

    Assert.That(act1, Is.EqualTo(0));
    Assert.That(act2, Is.EqualTo(0));
    AssertValidLogs(correctLogs);
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

    var correctLogs1 =
        @"*  (HEAD -> develop, origin/develop)Update metadata to version '1.3.0'.
          | *  (tag: v1.2.0, origin/master, master)Merge branch 'release/v1.2.0'
          |/| 
          | *  (origin/release/v1.2.0, release/v1.2.0)Commit on release branch
          | * Update metadata to version '1.2.0'.
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

    var act1 = RunProgram(new[] { "Release-Version", "-p" });
    ExecuteGitCommand("commit -m \"Commit on release branch\" --allow-empty");
    var act2 = RunProgram(new[] { "Close-Version" });

    Assert.That(act1, Is.EqualTo(0));
    Assert.That(act2, Is.EqualTo(0));
    AssertValidLogs(correctLogs, correctLogs1);
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

    var act1 = RunProgram(new[] { "Release-Version", $"-c {releaseCommit}" });

    ExecuteGitCommand("commit -m \"Commit after finishing on new branch\" --allow-empty");
    Assert.That(act1, Is.EqualTo(0));
    AssertValidLogs(correctLogs);
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

    var act1 = RunProgram(new[] { "Release-Version", "-p" });
    ExecuteGitCommand("commit -m \"Commit on release branch\" --allow-empty");
    var act2 = RunProgram(new[] { "Close-Version", "-a develop" });

    Assert.That(act1, Is.EqualTo(0));
    Assert.That(act2, Is.EqualTo(0));
    AssertValidLogs(correctLogs);
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

    var act1 = RunProgram(new[] { "Release-Version", "-p" });
    ExecuteGitCommand("commit -m \"Commit on release branch\" --allow-empty");
    var logs = ExecuteGitCommandWithOutput("log --all --graph --oneline --decorate --pretty=%d%s");
    logs = logs.Replace(" ", "");
    var act2 = RunProgram(new[] { "Close-Version", "--noPush" });

    Assert.That(act1, Is.EqualTo(0));
    Assert.That(act2, Is.EqualTo(0));
    AssertValidLogs(correctLogs);
  }

  [Test]
  public void ReleaseNewBranch_FromDevelop ()
  {
    var correctLogs =
        @"*  (origin/develop, develop)Update metadata to version '1.3.0'.
          *  (HEAD -> release/v1.2.0, origin/release/v1.2.0, master)ConfigAndBuildProject
          *  (origin/master)Initial CommitAll
          ";

    ExecuteGitCommand("checkout -b develop");
    ExecuteGitCommand("commit -m Commit on develop --allow-empty");

    //Get release version from user
    TestConsole.Input.PushTextWithEnter("1.2.0");
    //Get next release version from user for jira
    TestConsole.Input.PushTextWithEnter("1.3.0");

    var act1 = RunProgram(new[] { "New-Release-Branch" });

    Assert.That(TestConsole.Output, Does.Contain("Called UpdateAssemblyInfosForRelease!"));
    Assert.That(TestConsole.Output, Does.Not.Contain("Called UpdateAssemblyInfosForDevelopment!"));
    Assert.That(act1, Is.EqualTo(0));
    AssertValidLogs(correctLogs);
  }

  [Test]
  public void FixesMergeConflicts ()
  {
    var correctLogs =
        @"*    (tag: v2.0.0, origin/master, master)Merge branch 'release/v2.0.0'
          |\  
          | *  (origin/release/v2.0.0, release/v2.0.0)Update metadata to version '2.0.0'.
          | * added files to second release
          * |    (tag: v1.0.0)Merge branch 'release/v1.0.0'
          |\ \  
          | * |  (origin/release/v1.0.0, release/v1.0.0)Update metadata to version '1.0.0'.
          | * | added files to first release
          |/ /  
          | | *  (HEAD -> develop, origin/develop)Update metadata to version '3.0.0'.
          | |/  
          | * Update metadata to version '2.0.0'.
          |/  
          * ConfigAndBuildProject
          * Initial CommitAll
          ";

    ExecuteGitCommand("checkout -b develop");
    ExecuteGitCommand("commit -m Commit on develop --allow-empty");

    //Get release version from user
    TestConsole.Input.PushTextWithEnter("1.0.0");
    //Get next release version from user for jira
    TestConsole.Input.PushTextWithEnter("2.0.0");

    var act1 = RunProgram(new[] { "New-Release-Branch" });

    CreateAndAddFilesWithText("something added");
    ExecuteGitCommand("commit -a -m \"added files to first release\"");

    //Get release version from user
    TestConsole.Input.PushTextWithEnter("1.0.0");
    //Get next release version from user for jira
    TestConsole.Input.PushTextWithEnter("2.0.0");
    //Do not create a new support branch
    TestConsole.Input.PushTextWithEnter("n");

    var act2 = RunProgram(new[] { "Release-Version" });

    ExecuteGitCommand("checkout develop");

    //Get release version from user
    TestConsole.Input.PushTextWithEnter("2.0.0");
    //Get next release version from user for jira
    TestConsole.Input.PushTextWithEnter("3.0.0");

    var act3 = RunProgram(new[] { "New-Release-Branch" });

    CreateAndAddFilesWithText("anotherText");
    ExecuteGitCommand("commit -a -m \"added files to second release\"");

    //Get release version from user
    TestConsole.Input.PushTextWithEnter("2.0.0");
    //Get next release version from user for jira
    TestConsole.Input.PushTextWithEnter("3.0.0");
    //Do not create a new support branch
    TestConsole.Input.PushTextWithEnter("n");

    var act4 = RunProgram(new[] { "Release-Version" });

    AssertValidLogs(correctLogs);
    Assert.That(act1, Is.EqualTo(0));
    Assert.That(act2, Is.EqualTo(0));
    Assert.That(act3, Is.EqualTo(0));
    Assert.That(act4, Is.EqualTo(0));
  }

  [Test]
  public void ReleaseVersion_WithMajorReleaseTagOnMasterAndAfterFetching_SuggestsAlphaVersions ()
  {
    ExecuteGitCommand("tag v2.0.0 -m v2.0.0");
    ExecuteGitCommand("checkout -b develop");
    ExecuteGitCommand("commit -m \"Commit on develop\" --allow-empty");
    var resetCommit = ExecuteGitCommandWithOutput("log -1 --pretty=%H");
    ExecuteGitCommand("commit -m \"Commit on develop\" --allow-empty");
    ExecuteGitCommand($"push {RemoteName}");
    ExecuteGitCommand($"reset --hard {resetCommit}");
    ExecuteGitCommand($"fetch {RemoteName}");

    //Get release version from user
    TestConsole.Input.PushTextWithEnter("3.0.0-alpha.1");
    //Get next release version from user for jira
    TestConsole.Input.PushTextWithEnter("3.0.0");

    var act1 = RunProgram(new[] { "Release-Version" });

    Assert.That(act1, Is.EqualTo(0));
    Assert.That(TestConsole.Output, Does.Contain("3.0.0-alpha.1"));
    Assert.That(TestConsole.Output, Does.Contain("2.1.0-alpha.1"));
  }

  private void CreateAndAddFilesWithText (string text)
  {
    var firstFilePath = Path.Combine(Environment.CurrentDirectory, "file1.txt");
    var firstFile = File.CreateText(firstFilePath);
    firstFile.WriteLine(text);
    firstFile.Close();

    var secondFilePath = Path.Combine(Environment.CurrentDirectory, "file2.txt");
    var secondFile = File.CreateText(secondFilePath);
    secondFile.WriteLine(text);
    secondFile.Close();

    ExecuteGitCommand("add file1.txt");
    ExecuteGitCommand("add file2.txt");
  }
}