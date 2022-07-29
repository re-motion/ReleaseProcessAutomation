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

namespace ReleaseProcessAutomation.IntegrationTests;

internal class PushToReposTests : IntegrationTestSetup
{
  [Test]
  public void PushToRepos_DevelopAhead_PushesDevelop ()
  {
    var correctLogs =
        @"*  (HEAD -> develop, origin/develop, master)ConfigAndBuildProject
          *  (origin/master)Initial CommitAll
          ";

    ExecuteGitCommand("checkout -b develop");
    ExecuteGitCommand("commit -m Commit on develop1 --allow-empty");
    ExecuteGitCommand("commit -m Commit on develop2 --allow-empty");
    ExecuteGitCommand("commit -m Commit on develop3 --allow-empty");

    var act1 = RunProgram(new[] { "Push-Remote-Repos", "develop" });

    Assert.That(act1, Is.EqualTo(0));
    AssertValidLogs(correctLogs);
  }

  [Test]
  public void PushToRepos_DevelopAheadWithTag_PushesDevelopWithTag ()
  {
    var correctLogs =
        @"*(HEAD->develop,tag:v1.0.0,origin/develop,master)ConfigAndBuildProject
          *(origin/master)InitialCommitAll
          ";

    ExecuteGitCommand("checkout -b develop");
    ExecuteGitCommand("commit -m Commit on develop1 --allow-empty");
    ExecuteGitCommand("commit -m Commit on develop2 --allow-empty");
    ExecuteGitCommand("commit -m Commit on develop3 --allow-empty");
    ExecuteGitCommand("tag -a v1.0.0 -m v1.0.0");

    var act1 = RunProgram(new[] { "Push-Remote-Repos", "develop", "-t v1.0.0" });

    Assert.That(act1, Is.EqualTo(0));
    AssertValidLogs(correctLogs);
  }
}