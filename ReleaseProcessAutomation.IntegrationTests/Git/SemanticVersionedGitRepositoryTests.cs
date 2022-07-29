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
using System.Linq;
using Moq;
using NUnit.Framework;
using ReleaseProcessAutomation.Git;
using ReleaseProcessAutomation.ReadInput;
using ReleaseProcessAutomation.SemanticVersioning;

namespace ReleaseProcessAutomation.IntegrationTests.Git;

[TestFixture]
internal class SemanticVersionedGitRepositoryTests : GitBackedTests
{
  [Test]
  public void GetVersionsSorted_With3Versions_ReturnsThemInCorrectOrder ()
  {
    var parser = new SemanticVersionParser();
    var gitClientMock = new Mock<IGitClient>();
    gitClientMock.Setup(_ => _.GetTags("HEAD", "")).Returns(
        new[]
        {
            "v1.0.1-alpha.3",
            "v2.0.0",
            "vHelp",
            "v1.0.1-beta.2"
        });
    var semVeredGitRepo = new SemanticVersionedGitRepository(gitClientMock.Object);

    var versions = semVeredGitRepo.GetVersionsSorted();

    Assert.That(
        versions,
        Is.EqualTo(
            new[]
            {
                parser.ParseVersion("2.0.0"),
                parser.ParseVersion("1.0.1-beta.2"),
                parser.ParseVersion("1.0.1-alpha.3")
            }.ToList()));
  }

  [Test]
  public void TryGetCurrentVersion_With3Versions_ReturnsFirstAndTrue ()
  {
    var gitClientMock = new Mock<IGitClient>();
    gitClientMock.Setup(_ => _.GetTags("HEAD", "")).Returns(
        new[]
        {
            "v1.0.1-alpha.3",
            "v2.0.0",
            "vHelp",
            "v1.0.1-beta.2"
        });
    var correctVersion = new SemanticVersion
                         {
                             Major = 2,
                             Minor = 0,
                             Patch = 0
                         };

    var semVeredGitRepo = new SemanticVersionedGitRepository(gitClientMock.Object);

    var versionExist = semVeredGitRepo.TryGetCurrentVersion(out var output);

    Assert.That(versionExist, Is.True);
    Assert.That(output, Is.EqualTo(correctVersion));
  }

  [Test]
  public void DoesCurrentVersionExist_WithoutVersion_ReturnsFalse ()
  {
    var gitClient = new CommandLineGitClient();
    var readerMock = new Mock<IInputReader>();
    var semVeredGitRepo = new SemanticVersionedGitRepository(gitClient);

    var versionExist = semVeredGitRepo.TryGetCurrentVersion(out var output);

    Assert.That(versionExist, Is.False);
  }

  [Test]
  public void GetMostRecentHotfixVersion_PreReleaseAlpha_ReturnsAlphaVersion ()
  {
    ExecuteGitCommand("checkout -b support/v2.28");
    ExecuteGitCommand("commit -m \"Commit on Support Branch\" --allow-empty");

    ExecuteGitCommand("checkout -b hotfix/v2.28.1");
    ExecuteGitCommand("commit -m \"Feature-A\" --allow-empty");

    ExecuteGitCommand("checkout -b prerelease/v2.28.1-alpha.1");
    ExecuteGitCommand("commit -m \"Update metadata to version '2.28.1-alpha.1'\" --allow-empty");
    ExecuteGitCommand("tag -a v2.28.1-alpha.1 -m v2.28.1-alpha.1");

    ExecuteGitCommand("checkout hotfix/v2.28.1");
    ExecuteGitCommand("merge \"prerelease/v2.28.1-alpha.1\" --no-ff");

    ExecuteGitCommand("checkout -b hotfix/v2.28.1");
    ExecuteGitCommand("commit -m \"Feature-B\" --allow-empty");

    ExecuteGitCommand("checkout -b prerelease/v2.28.1-alpha.2");
    ExecuteGitCommand("commit -m \"Update metadata to version '2.28.1-alpha.2'\" --allow-empty");
    ExecuteGitCommand("tag -a v2.28.1-alpha.2 -m v2.28.1-alpha.2");

    ExecuteGitCommand("checkout -b hotfix/v2.28.1");
    ExecuteGitCommand("merge \"prerelease/v2.28.1-alpha.2\" --no-ff");

    var mostRecentVersion = new SemanticVersion
                            {
                                Major = 2,
                                Minor = 28,
                                Patch = 1,
                                Pre = PreReleaseStage.alpha,
                                PreReleaseCounter = 2
                            };

    var gitClient = new CommandLineGitClient();
    var semVeredGitRepo = new SemanticVersionedGitRepository(gitClient);

    var version = semVeredGitRepo.GetMostRecentHotfixVersion();

    Assert.That(version, Is.EqualTo(mostRecentVersion));
  }

  [Test]
  public void GetMostRecentHotfixVersion_PreReleaseBeta_ReturnsBetaVersion ()
  {
    ExecuteGitCommand("checkout -b support/v2.28");
    ExecuteGitCommand("commit -m \"Commit on Support Branch\" --allow-empty");

    ExecuteGitCommand("checkout -b hotfix/v2.28.1");
    ExecuteGitCommand("commit -m \"Feature-A\" --allow-empty");

    ExecuteGitCommand("checkout -b prerelease/v2.28.1-beta.1");
    ExecuteGitCommand("commit -m \"Update metadata to version '2.28.1-beta.1'\" --allow-empty");
    ExecuteGitCommand("tag -a v2.28.1-beta.1 -m v2.28.1-beta.1");

    ExecuteGitCommand("checkout hotfix/v2.28.1");
    ExecuteGitCommand("merge \"prerelease/v2.28.1-beta.1\" --no-ff");

    var mostRecentVersion = new SemanticVersion
                            {
                                Major = 2,
                                Minor = 28,
                                Patch = 1,
                                Pre = PreReleaseStage.beta,
                                PreReleaseCounter = 1
                            };

    var gitClient = new CommandLineGitClient();
    var semVeredGitRepo = new SemanticVersionedGitRepository(gitClient);

    var version = semVeredGitRepo.GetMostRecentHotfixVersion();

    Assert.That(version, Is.EqualTo(mostRecentVersion));
  }

  [Test]
  public void GetMostRecentHotfixVersion_NoTags_ReturnsCurrentVersion ()
  {
    ExecuteGitCommand("checkout -b support/v2.28");
    ExecuteGitCommand("commit -m \"Commit on Support Branch\" --allow-empty");

    ExecuteGitCommand("checkout -b hotfix/v2.28.1");
    ExecuteGitCommand("commit -m \"Feature-A\" --allow-empty");
    ExecuteGitCommand("commit -m \"Feature-B\" --allow-empty");

    var mostRecentVersion = new SemanticVersion
                            {
                                Major = 2,
                                Minor = 28,
                                Patch = 1
                            };

    var gitClient = new CommandLineGitClient();
    var semVeredGitRepo = new SemanticVersionedGitRepository(gitClient);

    var version = semVeredGitRepo.GetMostRecentHotfixVersion();

    Assert.That(version, Is.EqualTo(mostRecentVersion));
  }

  [Test]
  public void GetMostRecentHotfixVersion_PreReleaseAlphaWithInvalidTags_ReturnsAlphaVersion ()
  {
    ExecuteGitCommand("checkout -b support/v2.28");
    ExecuteGitCommand("commit -m \"Commit on Support Branch\" --allow-empty");

    ExecuteGitCommand("checkout -b hotfix/v2.28.1");
    ExecuteGitCommand("commit -m \"Feature-A\" --allow-empty");

    ExecuteGitCommand("checkout support/v2.28");
    ExecuteGitCommand("merge --no-ff hotfix/v2.28.1");
    ExecuteGitCommand("tag -a v2.28.1 -m v2.28.1");

    ExecuteGitCommand("checkout -b hotfix/v2.28.2");
    ExecuteGitCommand("commit -m \"Feature-B\" --allow-empty");

    ExecuteGitCommand("checkout support/v2.28");
    ExecuteGitCommand("merge --no-ff hotfix/v2.28.2");
    ExecuteGitCommand("tag -a v2.28.2 -m v2.28.2");

    ExecuteGitCommand("checkout -b hotfix/v2.28.3");
    ExecuteGitCommand("commit -m \"Feature-B\" --allow-empty");

    ExecuteGitCommand("checkout support/v2.28");
    ExecuteGitCommand("merge --no-ff hotfix/v2.28.3");
    ExecuteGitCommand("tag -a v2.28.3 -m v2.28.3");

    ExecuteGitCommand("checkout -b hotfix/v2.28.4");
    ExecuteGitCommand("commit -m \"Feature-C\" --allow-empty");

    var mostRecentVersion = new SemanticVersion
                            {
                                Major = 2,
                                Minor = 28,
                                Patch = 4
                            };

    var gitClient = new CommandLineGitClient();
    var semVeredGitRepo = new SemanticVersionedGitRepository(gitClient);

    var version = semVeredGitRepo.GetMostRecentHotfixVersion();

    Assert.That(version, Is.EqualTo(mostRecentVersion));
  }

  [Test]
  public void GetMostRecentHotfixVersion_FallsBackToVersionBasedOnBranchName_IrnogresMinorTagFromSupportBranchFork ()
  {
    ExecuteGitCommand("checkout -b support/v2.28");
    ExecuteGitCommand("commit -m \"Commit on Support Branch\" --allow-empty");
    ExecuteGitCommand("tag -a v2.28.0 -m v2.28.0");

    ExecuteGitCommand("checkout -b support/v2.29");
    ExecuteGitCommand("checkout -b hotfix/v2.29.0");
    ExecuteGitCommand("commit -m \"Feature-B\" --allow-empty");
    ExecuteGitCommand("commit -m \"Feature-C\" --allow-empty");
    ExecuteGitCommand("commit -m \"Feature-D\" --allow-empty");

    var mostRecentVersion = new SemanticVersion
                            {
                                Major = 2,
                                Minor = 29,
                                Patch = 0
                            };

    var gitClient = new CommandLineGitClient();
    var semVeredGitRepo = new SemanticVersionedGitRepository(gitClient);

    var version = semVeredGitRepo.GetMostRecentHotfixVersion();

    Assert.That(version, Is.EqualTo(mostRecentVersion));
  }

  [Test]
  public void GetMostRecentHotfixVersion_FallsBackToVersionBasedOnHotfixBranchName_IgnoresMinorTagFromSupportBranch ()
  {
    ExecuteGitCommand("checkout -b support/v2.28");
    ExecuteGitCommand("commit -m \"Commit on Support Branch\" --allow-empty");

    ExecuteGitCommand("checkout -b support/v2.29");
    ExecuteGitCommand("checkout -b hotfix/v2.29.0");
    ExecuteGitCommand("commit -m \"Feature-B\" --allow-empty");
    ExecuteGitCommand("commit -m \"Feature-C\" --allow-empty");
    ExecuteGitCommand("commit -m \"Feature-D\" --allow-empty");

    var mostRecentVersion = new SemanticVersion
                            {
                                Major = 2,
                                Minor = 29,
                                Patch = 0
                            };

    var gitClient = new CommandLineGitClient();
    var semVeredGitRepo = new SemanticVersionedGitRepository(gitClient);

    var version = semVeredGitRepo.GetMostRecentHotfixVersion();

    Assert.That(version, Is.EqualTo(mostRecentVersion));
  }

  [Test]
  public void GetMostRecentHotfixVersion_FallsBackToVersionBasedOnBranchName_IgnoresMinorTagFromSupportBranch ()
  {
    ExecuteGitCommand("checkout -b support/v2.29");
    ExecuteGitCommand("commit -m \"Commit on Support Branch\" --allow-empty");
    ExecuteGitCommand("tag -a v2.28.0 -m v2.28.0");

    ExecuteGitCommand("checkout -b hotfix/v2.29.0");
    ExecuteGitCommand("commit -m \"Feature-B\" --allow-empty");
    ExecuteGitCommand("commit -m \"Feature-C\" --allow-empty");
    ExecuteGitCommand("commit -m \"Feature-D\" --allow-empty");

    var mostRecentVersion = new SemanticVersion
                            {
                                Major = 2,
                                Minor = 29,
                                Patch = 0
                            };

    var gitClient = new CommandLineGitClient();
    var semVeredGitRepo = new SemanticVersionedGitRepository(gitClient);

    var version = semVeredGitRepo.GetMostRecentHotfixVersion();

    Assert.That(version, Is.EqualTo(mostRecentVersion));
  }
}