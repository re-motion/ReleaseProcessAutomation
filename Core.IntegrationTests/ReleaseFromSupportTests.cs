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
using Remotion.ReleaseProcessAutomation;

namespace Remotion.ReleaseProcessAutomation.IntegrationTests;

[TestFixture]
internal class ReleaseFromSupportTests : IntegrationTestBase
{
  [Test]
  public void ReleaseVersion_FromSupport_ThrowsException ()
  {
    ExecuteGitCommand("checkout -b support/v1.1");

    Program.Console = TestConsole;

    Assert.That(() => RunProgram(new[] { "Release-Version" }), 
        Throws.InstanceOf<UserInteractionException>().
            With.Message.EqualTo("You have to be on either a 'hotfix/*' or 'release/*' or 'develop' or 'master' branch to release a version."));
  }
}