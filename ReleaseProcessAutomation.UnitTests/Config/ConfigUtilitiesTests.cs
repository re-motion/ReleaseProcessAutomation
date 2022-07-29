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
using Moq;
using NUnit.Framework;
using ReleaseProcessAutomation.Configuration.Data;
using ReleaseProcessAutomation.MSBuild;
using ReleaseProcessAutomation.SemanticVersioning;
using Spectre.Console;

namespace ReleaseProcessAutomation.UnitTests.Config;

[TestFixture]
internal class ConfigUtilitiesTests
{
  [Test]
  public void GetMSBuildCallString_WithoutMSBuildCalls_ReturnsEmpty ()
  {
    var parser = new SemanticVersionParser();
    var step = new Step();
    step.MSBuildCallArguments = new MSBuildArguments();
    step.MSBuildCallArguments.Arguments = new string[] { };

    var consoleMock = new Mock<IAnsiConsole>();

    var msBuildCallString = MSBuildUtilities.GetMSBuildCallString(
        step,
        parser.ParseVersion("1.0.0"),
        consoleMock.Object);

    Assert.That(msBuildCallString, Is.Null);
  }

  [Test]
  public void GetMSBuildCallString_SeveralArguments_ReturnsVersionedArguments ()
  {
    var parser = new SemanticVersionParser();
    var step = new Step();
    step.MSBuildCallArguments = new MSBuildArguments();
    step.MSBuildCallArguments.Arguments = new[] { "Argument1", "version{version}" };

    var consoleMock = new Mock<IAnsiConsole>();

    var msBuildCallString = MSBuildUtilities.GetMSBuildCallString(
        step,
        parser.ParseVersion("1.0.0"),
        consoleMock.Object);

    Assert.That(msBuildCallString, Is.EqualTo("Argument1 version1.0.0 "));
  }
}