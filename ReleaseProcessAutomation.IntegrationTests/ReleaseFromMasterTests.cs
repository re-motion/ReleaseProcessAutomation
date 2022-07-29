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

internal class ReleaseFromMasterTests : IntegrationTestSetup
{
  [Test]
  public void ReleasePatch_FromMaster_ReleaseNewPatch ()
  {
    var correctLogs =
        @"*  (origin/hotfix/v1.0.2, hotfix/v1.0.2)Update metadata to version '1.0.2'.
          *    (HEAD -> master, tag: v1.0.1, origin/master)Merge branch 'release/v1.0.1' into master
          |\  
          | *  (origin/release/v1.0.1, release/v1.0.1)Update metadata to version '1.0.1'.
          |/  
          * feature4
          * feature3
          * feature2
          *  (tag: v1.0.0)feature
          * ConfigAndBuildProject
          * Initial CommitAll
          ";

    ExecuteGitCommand("commit -m feature --allow-empty");
    ExecuteGitCommand("tag v1.0.0");

    ExecuteGitCommand("commit -m feature2 --allow-empty");
    ExecuteGitCommand("commit -m feature3 --allow-empty");
    ExecuteGitCommand("commit -m feature4 --allow-empty");

    //Get release version from user
    //Get next release version from user for jira
    TestConsole.Input.PushTextWithEnter("1.0.2");
    //Do not want to create support branch
    TestConsole.Input.PushTextWithEnter("n");

    var act = RunProgram(new[] { "Release-Version" });

    Assert.That(act, Is.EqualTo(0));
    AssertValidLogs(correctLogs);
  }
}