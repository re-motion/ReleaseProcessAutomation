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

using NUnit.Framework;

namespace Remotion.ReleaseProcessAutomation.IntegrationTests;

[TestFixture]
internal class ReleaseFromReleaseTests : IntegrationTestBase
{
  [Test]
  public void ReleaseWithRC_FromSupportHotfixRelease_CreatesHotfixBranch ()
  {
    var correctLogs =
        @"*  (origin/hotfix/v1.3.6, hotfix/v1.3.6)Update metadata to version '1.3.6'.
          *    (HEAD -> support/v1.3, tag: v1.3.5, origin/support/v1.3)Merge branch 'release/v1.3.5' into support/v1.3
          |\  
          | *  (origin/release/v1.3.5, release/v1.3.5)Update metadata to version '1.3.5'.
          | *  (hotfix/v1.3.5)feature4
          | * feature3
          | * feature2
          |/  
          *  (tag: v1.0.0, master)feature
          * ConfigAndBuildProject
          *  (origin/master)Initial CommitAll
          ";
    ExecuteGitCommand("commit -m feature --allow-empty");
    ExecuteGitCommand("tag v1.0.0");
    ExecuteGitCommand("checkout -b support/v1.3");
    ExecuteGitCommand("checkout -b hotfix/v1.3.5");

    ExecuteGitCommand("commit -m feature2 --allow-empty");
    ExecuteGitCommand("commit -m feature3 --allow-empty");
    ExecuteGitCommand("commit -m feature4 --allow-empty");

    ExecuteGitCommand("checkout -b release/v1.3.5");

    //Get release version from user
    TestConsole.Input.PushTextWithEnter("1.3.5");
    //Get next release version from user for jira
    TestConsole.Input.PushTextWithEnter("1.3.6");
    //Confirms to create new support branch
    TestConsole.Input.PushTextWithEnter("n");

    var act = RunProgram(new[] { "Release-Version" });

    Assert.That(act, Is.EqualTo(0));
    AssertValidLogs(correctLogs);
  }

  [Test]
  public void ReleaseWithRC_FromSupportHotfixReleaseWithUserRequestingNewSupportBranch_CreatesSupportAndHotfixBranch ()
  {
    var correctLogs1 =
        @"*  (origin/hotfix/v1.4.0, hotfix/v1.4.0)Update metadata to version '1.4.0'.
          | *  (origin/hotfix/v1.3.6, hotfix/v1.3.6)Update metadata to version '1.3.6'.
          |/  
          *    (HEAD -> support/v1.3, tag: v1.3.5, origin/support/v1.4, origin/support/v1.3, support/v1.4) Merge branch 'release/v1.3.5' into support/v1.3
          |\  
          | *  (origin/release/v1.3.5, release/v1.3.5)Update metadata to version '1.3.5'.
          | *  (hotfix/v1.3.5)feature4
          | * feature3
          | * feature2
          |/  
          *  (tag: v1.0.0, master)feature
          * ConfigAndBuildProject
          *  (origin/master)Initial CommitAll
          ";

    var correctLogs2 =
        @"*  (origin/hotfix/v1.3.6, hotfix/v1.3.6)Update metadata to version '1.3.6'.
          | *  (origin/hotfix/v1.4.0, hotfix/v1.4.0)Update metadata to version '1.4.0'.
          |/  
          *    (HEAD -> support/v1.3, tag: v1.3.5, origin/support/v1.4, origin/support/v1.3, support/v1.4) Merge branch 'release/v1.3.5' into support/v1.3
          |\  
          | *  (origin/release/v1.3.5, release/v1.3.5)Update metadata to version '1.3.5'.
          | *  (hotfix/v1.3.5)feature4
          | * feature3
          | * feature2
          |/  
          *  (tag: v1.0.0, master)feature
          * ConfigAndBuildProject
          *  (origin/master)Initial CommitAll
          ";

    ExecuteGitCommand("commit -m feature --allow-empty");
    ExecuteGitCommand("tag v1.0.0");
    ExecuteGitCommand("checkout -b support/v1.3");
    ExecuteGitCommand("checkout -b hotfix/v1.3.5");

    ExecuteGitCommand("commit -m feature2 --allow-empty");
    ExecuteGitCommand("commit -m feature3 --allow-empty");
    ExecuteGitCommand("commit -m feature4 --allow-empty");

    ExecuteGitCommand("checkout -b release/v1.3.5");

    //Get release version from user
    TestConsole.Input.PushTextWithEnter("1.3.5");
    //Get next release version from user for jira
    TestConsole.Input.PushTextWithEnter("1.3.6");
    //Confirms to create new support branch
    TestConsole.Input.PushTextWithEnter("y");

    var act = RunProgram(new[] { "Release-Version" });

    Assert.That(act, Is.EqualTo(0));
    AssertValidLogs(correctLogs1, correctLogs2);
  }

  [Test]
  public void ReleaseRC_FromSupportHotfixRelease_ReleasesNewPreReleaseIntoRelease ()
  {
    var correctLogs =
        @"*    (HEAD -> release/v1.3.5, origin/release/v1.3.5)Merge branch 'prerelease/v1.3.5-rc.1' into release/v1.3.5
          |\  
          | * (origin/prerelease/v1.3.5-rc.1, prerelease/v1.3.5-rc.1)Reset metadata for merge.
          | *  (tag: v1.3.5-rc.1)Update metadata to version '1.3.5-rc.1'.
          |/  
          *  (hotfix/v1.3.5)feature4
          * feature3
          * feature2
          *  (tag: v1.0.0, support/v1.3, master)feature
          * ConfigAndBuildProject
          *  (origin/master)Initial CommitAll
          ";

    ExecuteGitCommand("commit -m feature --allow-empty");
    ExecuteGitCommand("tag v1.0.0");

    ExecuteGitCommand("checkout -b support/v1.3");
    ExecuteGitCommand("checkout -b hotfix/v1.3.5");

    ExecuteGitCommand("commit -m feature2 --allow-empty");
    ExecuteGitCommand("commit -m feature3 --allow-empty");
    ExecuteGitCommand("commit -m feature4 --allow-empty");

    ExecuteGitCommand("checkout -b release/v1.3.5");

    //Get release version from user
    TestConsole.Input.PushTextWithEnter("1.3.5-rc.1");
    //Get next release version from user for jira
    TestConsole.Input.PushTextWithEnter("1.3.5");

    var act = RunProgram(new[] { "Release-Version" });

    Assert.That(act, Is.EqualTo(0));
    AssertValidLogs(correctLogs);
  }

  [Test]
  public void ReleaseRC_FromDevelopRelease_ReleasesNewRCToRelease ()
  {
    var correctLogs =
        @"*    (HEAD -> release/v1.3.5, origin/release/v1.3.5)Merge branch 'prerelease/v1.3.5-rc.1' into release/v1.3.5
          |\  
          | * (origin/prerelease/v1.3.5-rc.1, prerelease/v1.3.5-rc.1)Reset metadata for merge.
          | *  (tag: v1.3.5-rc.1)Update metadata to version '1.3.5-rc.1'.
          |/  
          *  (develop)feature4
          * feature3
          * feature2
          *  (tag: v1.0.0, master)feature
          * ConfigAndBuildProject
          *  (origin/master)Initial CommitAll
          ";

    ExecuteGitCommand("commit -m feature --allow-empty");
    ExecuteGitCommand("tag v1.0.0");
    ExecuteGitCommand("checkout -b develop");

    ExecuteGitCommand("commit -m feature2 --allow-empty");
    ExecuteGitCommand("commit -m feature3 --allow-empty");
    ExecuteGitCommand("commit -m feature4 --allow-empty");

    ExecuteGitCommand("checkout -b release/v1.3.5");

    //Get release version from user
    TestConsole.Input.PushTextWithEnter("1.3.5-rc.1");
    //Get next release version from user for jira
    TestConsole.Input.PushTextWithEnter("1.3.5");

    var act = RunProgram(new[] { "Release-Version" });

    Assert.That(act, Is.EqualTo(0));
    AssertValidLogs(correctLogs);
  }

  [Test]
  public void ReleaseRelease_FromDevelopRelease_WithDevelopAheadOfRoot_ReleasesToMaster ()
  {
    var correctLogs =
        @"*    (tag: v2.0.0, origin/master, master)Merge branch 'release/v2.0.0'
          |\  
          | *  (origin/release/v2.0.0, release/v2.0.0)Update metadata to version '2.0.0'.
          | *  (HEAD -> develop, origin/develop)feature4
          | * feature3
          | * feature2
          |/  
          *  (tag: v1.3.0)feature
          * ConfigAndBuildProject
          * Initial CommitAll
          ";

    ExecuteGitCommand("commit -m feature --allow-empty");
    ExecuteGitCommand("tag v1.3.0");
    ExecuteGitCommand("checkout -b develop");

    ExecuteGitCommand("commit -m feature2 --allow-empty");
    ExecuteGitCommand("commit -m feature3 --allow-empty");
    ExecuteGitCommand("commit -m feature4 --allow-empty");

    ExecuteGitCommand("checkout -b release/v2.0.0");

    //Get release version from user
    TestConsole.Input.PushTextWithEnter("2.0.0");
    //Get next release version from user for jira
    TestConsole.Input.PushTextWithEnter("3.0.0");
    //Do not want to create support branch
    TestConsole.Input.PushTextWithEnter("n");

    var act = RunProgram(new[] { "Release-Version" });

    Assert.That(act, Is.EqualTo(0));
    AssertValidLogs(correctLogs);
  }

  [Test]
  public void ReleaseWithRC_FromDevelopReleaseWithMinorNextVersion_ReleasesToMaster ()
  {
    var correctLogs =
        @"*    (tag: v2.0.0, origin/master, master)Merge branch 'release/v2.0.0'
          |\  
          | *  (origin/release/v2.0.0, release/v2.0.0)Update metadata to version '2.0.0'.
          |/  
          *  (HEAD -> develop, origin/develop)feature
          * ConfigAndBuildProject
          * Initial CommitAll
          ";

    ExecuteGitCommand("commit -m feature --allow-empty");
    ExecuteGitCommand("checkout -b develop");

    ExecuteGitCommand("checkout -b release/v2.0.0");

    //Get release version from user
    TestConsole.Input.PushTextWithEnter("2.0.0");
    //Get next release version from user for jira
    TestConsole.Input.PushTextWithEnter("2.1.0");
    //Do not want to create support branch
    TestConsole.Input.PushTextWithEnter("n");

    var act = RunProgram(new[] { "Release-Version" });

    Assert.That(act, Is.EqualTo(0));
    AssertValidLogs(correctLogs);
  }
  
  [Test]
  public void ReleaseWithRC_FromDevelopReleaseWithMajorNextVersion_ReleasesToMaster ()
  {
    var correctLogs =
        @"*    (tag: v2.0.0, origin/master, master)Merge branch 'release/v2.0.0'
          |\  
          | *  (origin/release/v2.0.0, release/v2.0.0)Update metadata to version '2.0.0'.
          |/  
          *  (HEAD -> develop, origin/develop)feature
          * ConfigAndBuildProject
          * Initial CommitAll
          ";

    ExecuteGitCommand("commit -m feature --allow-empty");
    ExecuteGitCommand("checkout -b develop");

    ExecuteGitCommand("checkout -b release/v2.0.0");

    //Get release version from user
    TestConsole.Input.PushTextWithEnter("2.0.0");
    //Get next release version from user for jira
    TestConsole.Input.PushTextWithEnter("3.0.0");
    //Do not want to create support branch
    TestConsole.Input.PushTextWithEnter("n");

    var act = RunProgram(new[] { "Release-Version" });

    Assert.That(act, Is.EqualTo(0));
    AssertValidLogs(correctLogs);
  }

  [Test]
  public void ReleaseSecondRC_FromDevelopRelease_ToReleaseWithContinue ()
  {
    var correctLogs =
        @"*    (HEAD -> release/v1.2.0)Merge branch 'prerelease/v1.2.0-rc.2' into release/v1.2.0
          |\  
          | *  (origin/prerelease/v1.2.0-rc.2, prerelease/v1.2.0-rc.2)Reset metadata for merge.
          | *  (tag: v1.2.0-rc.2)Another commit on prerelease
          | * Update metadata to version '1.2.0-rc.2'.
          |/  
          *    Merge branch 'prerelease/v1.2.0-rc.1' into release/v1.2.0
          |\  
          | *  (tag: v1.2.0-rc.1, prerelease/v1.2.0-rc.1)Commit on prerelease branch
          |/  
          *  (master, develop)ConfigAndBuildProject
          *  (origin/master)Initial CommitAll
          ";

    ExecuteGitCommand("checkout -b develop");
    ExecuteGitCommand("checkout -b release/v1.2.0");
    ExecuteGitCommand("checkout -b prerelease/v1.2.0-rc.1");

    ExecuteGitCommand("commit -m \"Commit on prerelease branch\" --allow-empty");
    ExecuteGitCommand("tag -a v1.2.0-rc.1 -m v1.2.0-rc.1");
    ExecuteGitCommand("checkout release/v1.2.0");
    ExecuteGitCommand("merge prerelease/v1.2.0-rc.1 --no-ff");

    //Get release version from user
    TestConsole.Input.PushTextWithEnter("1.2.0-rc.2");
    //Get next release version from user for jira
    TestConsole.Input.PushTextWithEnter("1.2.0");
    //Get ancestor choice
    TestConsole.Input.PushTextWithEnter("release/v1.2.0");

    var act1 = RunProgram(new[] { "Release-Version", "-p" });
    ExecuteGitCommand("commit -m \"Another commit on prerelease\" --allow-empty");
    var act2 = RunProgram(new[] { "Close-Version" });

    Assert.That(act1, Is.EqualTo(0));
    Assert.That(act2, Is.EqualTo(0));
    AssertValidLogs(correctLogs);
  }

  [Test]
  public void ReleaseRC_FromDevelopRelease_WithCommitOnPrerelease ()
  {
    var correctLogs =
        @"*    (HEAD -> release/v1.2.0)Merge branch 'prerelease/v1.2.0-rc.1' into release/v1.2.0
          |\  
          | * (origin/prerelease/v1.2.0-rc.1, prerelease/v1.2.0-rc.1)Reset metadata for merge.
          | *  (tag: v1.2.0-rc.1)Commit on prerelease branch
          | * Update metadata to version '1.2.0-rc.1'.
          |/  
          *  (master, develop)ConfigAndBuildProject
          *  (origin/master)Initial CommitAll
          ";

    ExecuteGitCommand("checkout -b develop");
    ExecuteGitCommand("checkout -b release/v1.2.0");

    //Get release version from user
    TestConsole.Input.PushTextWithEnter("1.2.0-rc.1");
    //Get next release version from user for jira
    TestConsole.Input.PushTextWithEnter("1.2.0");
    //Get ancestor choice
    TestConsole.Input.PushTextWithEnter("release/v1.2.0");

    var act1 = RunProgram(new[] { "Release-Version", "-p" });
    ExecuteGitCommand("commit -m \"Commit on prerelease branch\" --allow-empty");
    var act2 = RunProgram(new[] { "Close-Version" });

    Assert.That(act1, Is.EqualTo(0));
    Assert.That(act2, Is.EqualTo(0));
    AssertValidLogs(correctLogs);
  }
}