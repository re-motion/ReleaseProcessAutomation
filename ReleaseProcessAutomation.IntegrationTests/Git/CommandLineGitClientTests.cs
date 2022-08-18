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
using ReleaseProcessAutomation.Configuration;
using ReleaseProcessAutomation.Git;
using ReleaseProcessAutomation.SemanticVersioning;

namespace ReleaseProcessAutomation.IntegrationTests.Git;

[TestFixture]
internal class CommandLineGitClientTests : GitBackedTests
{
  private Configuration.Data.Config _config;
  private const string c_configFileName = "ReleaseProcessScript.Test.Config";
  
  public override void Setup () 
  {
    base.Setup();
    var path = Path.Join(PreviousWorkingDirectory, c_configFileName);
    _config = new ConfigReader().LoadConfig(path);
  }

  [Test]
  public void GetCurrentBranchName_WithOneBranch_ReturnsBranchName ()
  {
    var client = new CommandLineGitClient();

    var branchName = client.GetCurrentBranchName();

    Assert.That(branchName, Is.EqualTo("master"));
  }

  [Test]
  public void GetCurrentBranchName_WithTwoBranches_ReturnsBranchName ()
  {
    var client = new CommandLineGitClient();
    ExecuteGitCommand("branch develop");

    var branchName = client.GetCurrentBranchName();

    Assert.That(branchName, Is.EqualTo("master"));
  }

  [Test]
  public void GetCurrentBranchName_OutsideGitRepo_ReturnsNull ()
  {
    var client = new CommandLineGitClient();
    Environment.CurrentDirectory = Path.GetTempPath();

    var branchName = client.GetCurrentBranchName();
    Assert.That(branchName, Is.Null);
  }

  [Test]
  public void IsMinGitVersion_WithNewerVersion_ReturnsTrue ()
  {
    var client = new CommandLineGitClient();

    var newer = client.IsMinGitVersion();

    Assert.That(newer, Is.True);
  }

  [Test]
  public void IsOnBranch_WithSeveralBranches_ReturnsTrue ()
  {
    var client = new CommandLineGitClient();
    ExecuteGitCommand("checkout -b develop");
    ExecuteGitCommand("checkout -b release/v1.0.1");
    ExecuteGitCommand("checkout develop");

    var newer = client.IsOnBranch("develop");

    Assert.That(newer, Is.True);
  }

  [Test]
  public void DoesBranchExist_WithOneBranch_ReturnsTrue ()
  {
    var client = new CommandLineGitClient();

    var outcome = client.DoesBranchExist("master");

    Assert.That(outcome, Is.True);
  }

  [Test]
  public void DoesBranchExist_WithOnlyMaster_ReturnsFalse ()
  {
    var client = new CommandLineGitClient();

    var outcome = client.DoesBranchExist("develop");

    Assert.That(outcome, Is.False);
  }

  [Test]
  public void DoesRemoteBranchExists_WithExistingBranch_ReturnsTrue ()
  {
    var client = new CommandLineGitClient();
    ExecuteGitCommand("checkout -b newBranch");
    AddCommit();
    ExecuteGitCommand("push -u origin newBranch");

    var exists = client.DoesRemoteBranchExist("origin", "newBranch");

    Assert.That(exists, Is.True);
  }

  [Test]
  public void DoesRemoteBranchExists_WithoutExistingBranch_ReturnsFalse ()
  {
    var client = new CommandLineGitClient();
    ExecuteGitCommand("checkout -b newBranch");
    AddCommit();
    ExecuteGitCommand("push -u origin newBranch");

    var exists = client.DoesRemoteBranchExist("origin", "oldBranch");

    Assert.That(exists, Is.False);
  }

  [Test]
  public void GetRemoteOfBranch_WithOriginAsRemote_ReturnsOrigin ()
  {
    var client = new CommandLineGitClient();
    ExecuteGitCommand("push -u origin master");
    var output = client.GetRemoteOfBranch("master");

    Assert.That(output, Is.EqualTo("origin"));
  }

  [Test]
  public void DoesTagExist_WithOneTag_ReturnsTrue ()
  {
    ReleaseVersion("v1.0.0");
    var client = new CommandLineGitClient();

    var outcome = client.DoesTagExist("v1.0.0");

    Assert.That(outcome, Is.True);
  }

  [Test]
  public void DoesTagExist_WithoutTag_ReturnsFalse ()
  {
    var client = new CommandLineGitClient();

    var outcome = client.DoesTagExist("v1.0.0");

    Assert.That(outcome, Is.False);
  }

  [Test]
  public void GetAncestors_DevelopBehindReleaseBranch_ReturnsOne ()
  {
    AddCommit();
    ExecuteGitCommand("checkout -b develop");
    AddCommit();
    ExecuteGitCommand("checkout -b release/v1.0.1");
    AddCommit();
    var client = new CommandLineGitClient();

    var ancestor = client.GetAncestors("develop");

    Assert.That(ancestor, Does.Contain("develop"));
    Assert.That(ancestor, Does.Not.Contain("master"));
    Assert.That(ancestor, Does.Not.Contain("release/v1.0.1"));
  }

  [Test]
  public void GetAncestors_WithTwoExpectedAncestors_ReturnsBothMatchingNames ()
  {
    AddCommit();
    ExecuteGitCommand("checkout -b develop");
    AddCommit();
    ExecuteGitCommand("checkout -b release/v1.0.1");
    AddCommit();
    ExecuteGitCommand("checkout develop");
    AddCommit();
    ExecuteGitCommand("checkout release/v1.0.1");
    ExecuteGitCommand("checkout master");

    var client = new CommandLineGitClient();

    var ancestor = client.GetAncestors("develop", "release");

    Assert.That(ancestor, Does.Contain("develop"));
    Assert.That(ancestor, Does.Contain("release/v1.0.1"));
    Assert.That(ancestor, Does.Not.Contain("master"));
  }

  [Test]
  public void GetAncestors_ReleaseOnSameCommitAsDevelop_ReturnsMatchingName ()
  {
    AddCommit();
    ExecuteGitCommand("checkout -b develop");
    ExecuteGitCommand("checkout -b developer");
    ExecuteGitCommand("checkout -b release/v1.0.1");
    var client = new CommandLineGitClient();

    var ancestor = client.GetAncestors("develop");

    Assert.That(ancestor, Does.Contain("develop"));
    Assert.That(ancestor, Does.Contain("developer"));
    Assert.That(ancestor, Does.Not.Contain("release/v1.0.1"));
  }

  [Test]
  public void GetAncestors_SeveralBranchesFromMaster_ReturnsMatchingName ()
  {
    AddCommit();
    ExecuteGitCommand("checkout -b develop");
    AddCommit();
    ExecuteGitCommand("checkout master");
    ExecuteGitCommand("checkout -b developer");
    AddCommit();
    ExecuteGitCommand("checkout -b release/v1.0.1");
    ExecuteGitCommand("checkout -b developeria");
    AddCommit();
    ExecuteGitCommand("checkout -b release/v1.0.2");
    var client = new CommandLineGitClient();

    var ancestor = client.GetAncestors("develop");

    Assert.That(ancestor, Does.Contain("developer"));
    Assert.That(ancestor, Does.Contain("developeria"));
  }

  [Test]
  public void IsCommitHash_CorrectHash_ReturnsTrue ()
  {
    var commitHash = ExecuteGitCommandWithOutput("log --pretty=%h").Replace("\n", "");
    var client = new CommandLineGitClient();

    var check = client.IsCommitHash(commitHash);

    Assert.That(check, Is.True);
  }

  [Test]
  public void IsCommitHash_NonHash_ReturnsFalse ()
  {
    var commitHash = "DefinetlyNotACommitHash";
    var client = new CommandLineGitClient();

    var check = client.IsCommitHash(commitHash);

    Assert.That(check, Is.EqualTo(false));
  }

  [Test]
  public void IsOnBranch_OnMasterBranchCheckMaster_ReturnsTrue ()
  {
    var client = new CommandLineGitClient();

    var check = client.IsOnBranch("master");

    Assert.That(check, Is.True);
  }

  [Test]
  public void IsOnBranch_OnMasterBranchCheckDevelop_ReturnsFalse ()
  {
    ExecuteGitCommand("checkout -b develop");
    ExecuteGitCommand("checkout master");
    var client = new CommandLineGitClient();

    var check = client.IsOnBranch("develop");

    Assert.That(check, Is.False);
  }

  [Test]
  public void GetTags_WithManyTags_ReturnsAllTags ()
  {
    var parser = new SemanticVersionParser();
    var client = new CommandLineGitClient();
    ExecuteGitCommand("tag v1.0.0");
    ExecuteGitCommand("tag v1.0.2");
    ExecuteGitCommand("tag v1.1.0");
    ExecuteGitCommand("tag vhelp");
    ExecuteGitCommand("tag v1.0.0-alpha.2");
    ExecuteGitCommand("tag v1.0.0-beta.1");

    var validVersions = client.GetTags();

    Assert.That(validVersions, Does.Contain("v1.0.0"));
    Assert.That(validVersions, Does.Contain("v1.0.2"));
    Assert.That(validVersions, Does.Contain("v1.1.0"));
    Assert.That(validVersions, Does.Contain("v1.0.0-alpha.2"));
    Assert.That(validVersions, Does.Contain("v1.0.0-beta.1"));
    Assert.That(validVersions, Does.Contain("vhelp"));
  }

  [Test]
  public void IsWorkingDirectoryClean_DirtyDirectory_ReturnsFalse ()
  {
    AddRandomFile(RepositoryPath);
    var client = new CommandLineGitClient();

    var check = client.IsWorkingDirectoryClean();

    Assert.That(check, Is.False);
  }

  [Test]
  public void IsWorkingDirectoryClean_CleanDirectory_ReturnsTrue ()
  {
    var client = new CommandLineGitClient();

    var check = client.IsWorkingDirectoryClean();

    Assert.That(check, Is.True);
  }

  [Test]
  public void CheckoutCommitWithNewBranch_WithIncorrectCommitHash_ThrowsException ()
  {
    var client = new CommandLineGitClient();

    Assert.That(
        () => client.CheckoutCommitWithNewBranch("notACommitHash", ""),
        Throws.InstanceOf<Exception>()
            .With.Message.StartsWith("Could not checkout commit 'notACommitHash'"));
  }

  [Test]
  public void CheckoutCommitWithNewBranch_WithoutErrors_CreatesNewBranch ()
  {
    var correctLogs1 = @"*  (HEAD -> newBranch)Commit on new branch
                        | *  (master)First commit
                        |/
                        *  (origin/master)Initial CommitAll
";
    var correctLogs2 = @"*  (master)First commit
                        | *  (HEAD -> newBranch)Commit on new branch
                        |/
                        *  (origin/master)Initial CommitAll
";

    var client = new CommandLineGitClient();
    var commitHash = ExecuteGitCommandWithOutput("log --pretty=%h").Split("\n")[0];
    ExecuteGitCommand("commit -m \"First commit\" --allow-empty");

    var act = client.CheckoutCommitWithNewBranch(commitHash, "newBranch");

    ExecuteGitCommand("commit -m \"Commit on new branch\" --allow-empty");

    var logs = ExecuteGitCommandWithOutput("log --all --graph --oneline --decorate --pretty=%d%s");
    correctLogs1 = correctLogs1.Replace(" ", "").Replace("\r", "");
    correctLogs2 = correctLogs2.Replace(" ", "").Replace("\r", "");

    logs = logs.Replace(" ", "");

    Assert.That(logs, Is.EqualTo(correctLogs1).Or.EqualTo(correctLogs2));
  }

  [Test]
  public void Checkout_WithIncorrectName_ThrowsException ()
  {
    var client = new CommandLineGitClient();

    Assert.That(
        () => client.Checkout("notABranch"),
        Throws.InstanceOf<Exception>()
            .With.Message.StartsWith("Could not checkout 'notABranch' from branch master."));
  }

  [Test]
  public void Checkout_ToBranchWithoutErrors_SwitchesBranch ()
  {
    var client = new CommandLineGitClient();

    ExecuteGitCommand("checkout -b newBranch");
    ExecuteGitCommand("checkout master");

    client.Checkout("newBranch");

    var check = ExecuteGitCommandWithOutput("status");
    Assert.That(check, Does.Contain("On branch newBranch"));
  }

  [Test]
  public void CheckoutNewBranch_WithoutErrors_CreatesAndSwitchesBranch ()
  {
    var client = new CommandLineGitClient();

    ExecuteGitCommand("checkout master");

    client.CheckoutNewBranch("newBranch");

    var check = ExecuteGitCommandWithOutput("status");
    Assert.That(check, Does.Contain("On branch newBranch"));
  }

  [Test]
  public void MergeBranchWithoutCommit_WithoutErrorsAndManualCommitAfterwards_MergesBranch ()
  {
    var correctLogs =
        @"*    (HEAD -> master)MergeCommit
          |\
          | *  (newBranch)Empty commit on new Branch
          |/
          *  (origin/master)Initial CommitAll
         ";

    var client = new CommandLineGitClient();

    ExecuteGitCommand("checkout -b newBranch");
    ExecuteGitCommand("commit -m \"Empty commit on new Branch\" --allow-empty");
    ExecuteGitCommand("checkout master");

    client.MergeBranchWithoutCommit("newBranch");

    ExecuteGitCommand("commit -am MergeCommit --allow-empty");
    AssertValidLogs(correctLogs);
  }

  [Test]
  public void MergeBranchWithoutCommit_WithConflictsAndNoAutomaticResolution_ThrowsException ()
  {
    ExecuteGitCommand("checkout -b a");
    var filePath = Path.Combine(Environment.CurrentDirectory, "file.txt");
    var fileWriter = File.CreateText(filePath);
    fileWriter.WriteLine("Branch a changes");
    fileWriter.Close();
    ExecuteGitCommand("add file.txt");
    ExecuteGitCommand("commit -a -m FileAdded");

    ExecuteGitCommand("checkout master");
    ExecuteGitCommand("checkout -b b");

    fileWriter = File.CreateText(filePath);
    fileWriter.WriteLine("Branch b changes");
    fileWriter.Close();
    ExecuteGitCommand("add file.txt");
    ExecuteGitCommand("commit -a -m FileAdded");

    var client = new CommandLineGitClient();

    Assert.That(() => client.MergeBranchWithoutCommit("a"), Throws.InstanceOf<Exception>().With.Message.StartsWith("Could not merge branch"));
  }

  [Test]
  public void MergeBranchToOnlyContainChangesFromMergedBranch_WithConflictsAndAutomaticResolution_FileContainsContentsFromOtherBranch ()
  {
    var correctLogs =
        @"*    (HEAD -> b)Merge branch 'a' into b
          |\  
          | *  (a)FileAdded
          * | FileAdded
          |/  
          *  (origin/master, master)Initial CommitAll
          ";

    ExecuteGitCommand("checkout -b a");
    var filePath = Path.Combine(Environment.CurrentDirectory, "file.txt");
    var fileWriter = File.CreateText(filePath);
    var originalContent = "Branch a changes";
    fileWriter.WriteLine(originalContent);
    fileWriter.Close();
    ExecuteGitCommand("add file.txt");
    ExecuteGitCommand("commit -a -m FileAdded");

    ExecuteGitCommand("checkout master");
    ExecuteGitCommand("checkout -b b");

    fileWriter = File.CreateText(filePath);
    fileWriter.WriteLine("Branch b changes");
    fileWriter.Close();
    ExecuteGitCommand("add file.txt");
    ExecuteGitCommand("commit -a -m FileAdded");

    var client = new CommandLineGitClient();
    client.MergeBranchToOnlyContainChangesFromMergedBranch("a");

    var fileContents = File.ReadAllText(filePath).ReplaceLineEndings("");

    Assert.That(fileContents, Is.EqualTo(originalContent));
    AssertValidLogs(correctLogs);
  }

  [Test]
  public void MergeBranchToOnlyContainChangesFromMergedBranch_WithFileAddedInTargetBranch_RemovesFile ()
  {
    var correctLogs =
        @"*    (HEAD -> b)Merge branch 'a' into b
          |\  
          * | AddedFile
          |/  
          *  (origin/master, master, a)Initial CommitAll
          ";

    ExecuteGitCommand("checkout -b a");

    ExecuteGitCommand("checkout master");
    ExecuteGitCommand("checkout -b b");

    var filePath = Path.Combine(Environment.CurrentDirectory, "file.txt");
    var fileWriter = File.CreateText(filePath);
    fileWriter.WriteLine("Branch b added File");
    fileWriter.Close();

    ExecuteGitCommand("add file.txt");
    ExecuteGitCommand("commit -a -m AddedFile");

    var client = new CommandLineGitClient();
    client.MergeBranchToOnlyContainChangesFromMergedBranch("a");

    Assert.That(() => File.Exists(filePath), Is.False);
    AssertValidLogs(correctLogs);
  }

  [Test]
  public void MergeBranchToOnlyContainChangesFromMergedBranch_FileRemovedInTargetBranch_RestoresFile ()
  {
    var correctLogs =
        @"*    (HEAD -> b)Merge branch 'a' into b
          |\  
          | *  (a)FileChanged
          * | Deleted File
          |/  
          *  (master)FileAdded
          *  (origin/master)Initial CommitAll
          ";

    var filePath = Path.Combine(Environment.CurrentDirectory, "file.txt");
    var fileWriter = File.CreateText(filePath);
    fileWriter.WriteLine("File added on master");
    fileWriter.Close();

    ExecuteGitCommand("add file.txt");
    ExecuteGitCommand("commit -a -m FileAdded");

    ExecuteGitCommand("checkout -b a");

    fileWriter = File.CreateText(filePath);
    fileWriter.WriteLine("Added some stuff on a branch");
    fileWriter.Close();
    ExecuteGitCommand("commit -a -m FileChanged");

    ExecuteGitCommand("checkout master");
    ExecuteGitCommand("checkout -b b");

    File.Delete(filePath);
    ExecuteGitCommand("commit -a -m \"Deleted File\"");

    var client = new CommandLineGitClient();
    client.MergeBranchToOnlyContainChangesFromMergedBranch("a");

    Assert.That(() => File.Exists(filePath), Is.True);
    AssertValidLogs(correctLogs);
  }

  [Test]
  public void CommitAll_WithoutErrors_CommitsAllChanges ()
  {
    AddRandomFile(RepositoryPath);
    AddRandomFile(RepositoryPath);
    AddRandomFile(RepositoryPath);
    AddRandomFile(RepositoryPath);
    ExecuteGitCommand("add --all");

    var client = new CommandLineGitClient();

    client.CommitAll("Added several files");

    var status = ExecuteGitCommandWithOutput("status");
    Assert.That(status, Does.Contain("nothing to commit"));
  }

  [Test]
  public void AddAll_WithoutErrors_AddsAllNewChanges ()
  {
    AddRandomFile(RepositoryPath);
    AddRandomFile(RepositoryPath);
    AddRandomFile(RepositoryPath);
    AddRandomFile(RepositoryPath);

    var client = new CommandLineGitClient();

    client.AddAll();

    var status = ExecuteGitCommandWithOutput("status");
    Assert.That(status, Does.Contain("Changes to be committed"));
    Assert.That(status, Does.Not.Contain("Untracked files:"));
  }

  [Test]
  public void Reset_FromAddingFile_FileIsNotInCommit ()
  {
    var path = Path.Join(RepositoryPath, "randomFile");
    var fs = File.Create(path);
    fs.Close();
    var client = new CommandLineGitClient();
    ExecuteGitCommand($"add {path}");

    client.Reset(path);
    var checkIfReset = ExecuteGitCommandWithOutput("status");

    Assert.That(checkIfReset, Does.Contain("nothing added to commit"));
  }

  [Test]
  public void CheckoutDiscard_CommitOneFileAndEdit_DoesNotContainEdit ()
  {
    var path = Path.Join(RepositoryPath, "randomFile");
    var fs = File.Create(path);
    fs.Close();
    var client = new CommandLineGitClient();
    ExecuteGitCommand($"add {path}");
    ExecuteGitCommand("commit -m randomFile");

    var file = "123text";
    File.WriteAllText(path, file);

    client.CheckoutDiscard(path);

    file = File.ReadAllText(path);
    Assert.That(file, Does.Not.Contain("123text"));
  }

  [Test]
  public void Tag_WithTagNameAndMessage_CreatesTagWithProperMessage ()
  {
    var client = new CommandLineGitClient();

    client.Tag("v1.0.0", "Create tag for version v1.0.0");

    var tags = ExecuteGitCommandWithOutput("tag -n");
    Assert.That(tags, Does.Contain("Create tag for version v1.0.0"));
  }

  [Test]
  public void Tag_TagWithoutMessage_CreatesTag ()
  {
    var client = new CommandLineGitClient();

    client.Tag("v1.0.0", null);

    var tags = ExecuteGitCommandWithOutput("tag -n");
    Assert.That(tags, Does.Contain("v1.0.0"));
  }

  [Test]
  public void Push_PushesLastCommit_BranchIsNotAhead ()
  {
    var client = new CommandLineGitClient();
    ExecuteGitCommand("commit --allow-empty -m comm");

    client.Push("");

    var tags = ExecuteGitCommandWithOutput("status");
    Assert.That(tags, Does.Not.Contain("Your branch is ahead of"));
  }

  [Test]
  public void Fetch_AddedFileInRemote_FetchesFile ()
  {
    var client = new CommandLineGitClient();
    var old = ExecuteGitCommandWithOutput("log --pretty=format:\"%h\"");
    AddCommit();
    AddCommit();
    ExecuteGitCommand("checkout master");
    ExecuteGitCommand($"push -u {RemoteName} master");

    var notBehind = ExecuteGitCommandWithOutput("show HEAD --pretty=%d");

    ExecuteGitCommand("branch --unset-upstream");
    ExecuteGitCommand($"branch -d -r {RemoteName}/master");

    client.Fetch($"{RemoteName}");

    var fetched = ExecuteGitCommandWithOutput("show HEAD --pretty=%d");

    Assert.That(fetched, Is.EqualTo(notBehind));
    Assert.That(fetched, Is.EqualTo(" (HEAD -> master, origin/master)\n"));
  }

  [Test]
  public void PushToRepos_WrongTag_DoesThrow ()
  {
    var gitClient = new CommandLineGitClient();
    var remoteNames = _config.RemoteRepositories.RemoteNames;

    Assert.That(
        () => gitClient.PushToRepos(remoteNames, "", "tag"),
        Throws.InstanceOf<InvalidOperationException>()
            .With.Message.EqualTo(
                "Tag with name 'tag' does not exist, must not have been created before calling pushToRepos, check previous steps."));
  }

  [Test]
  public void PushToRepos_NotOnBranch_DoesNotThrow ()
  {
    ExecuteGitCommand("checkout -b branch");
    ExecuteGitCommand("tag tag");
    ExecuteGitCommand("push origin");

    _config.RemoteRepositories.RemoteNames = new[]
                                             {
                                                 "origin"
                                             };

    var remoteNames = _config.RemoteRepositories.RemoteNames;

    var gitClient = new CommandLineGitClient();

    Assert.That(
        () => gitClient.PushToRepos(remoteNames, "branch", "tag"),
        Throws.Nothing);
  }
}