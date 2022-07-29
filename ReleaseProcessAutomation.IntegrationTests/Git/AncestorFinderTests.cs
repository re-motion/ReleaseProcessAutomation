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
using ReleaseProcessAutomation.Git;
using ReleaseProcessAutomation.ReadInput;
using Spectre.Console;
using Spectre.Console.Testing;

namespace ReleaseProcessAutomation.IntegrationTests.Git;

[TestFixture]
internal class AncestorFinderTests
{
  private IAnsiConsole _console;

  [SetUp]
  public void Setup ()
  {
    _console = new TestConsole();
  }

  [Test]
  public void GetAncestor_EmptyFound_CallsReader ()
  {
    var readerMock = new Mock<IInputReader>();
    var gitClientStub = new Mock<IGitClient>();
    readerMock.Setup(_ => _.ReadString(It.IsAny<string>())).Returns("noBranch").Verifiable();
    gitClientStub.Setup(_ => _.GetAncestors("")).Returns(Array.Empty<string>());

    var ancestorFinder = new AncestorFinder(gitClientStub.Object, readerMock.Object, _console);

    var output = ancestorFinder.GetAncestor("");

    Assert.That(output, Is.EqualTo("noBranch"));
    readerMock.Verify();
  }

  [Test]
  public void GetAncestor_OneFound_ReturnsFirst ()
  {
    var readerStub = new Mock<IInputReader>();
    var gitClientStub = new Mock<IGitClient>();
    gitClientStub.Setup(_ => _.GetAncestors("")).Returns(new[] { "one" });

    var ancestorFinder = new AncestorFinder(gitClientStub.Object, readerStub.Object, _console);

    var output = ancestorFinder.GetAncestor("");

    Assert.That(output, Is.EqualTo("one"));
  }

  [Test]
  public void GetAncestor_MultipleFound_CallsReader ()
  {
    var outputArray = new[] { "one", "two", "three" };

    var readerMock = new Mock<IInputReader>();
    var gitClientStub = new Mock<IGitClient>();
    readerMock.Setup(_ => _.ReadStringChoice(It.IsAny<string>(), outputArray)).Returns("two").Verifiable();
    gitClientStub.Setup(_ => _.GetAncestors("")).Returns(outputArray);

    var ancestorFinder = new AncestorFinder(gitClientStub.Object, readerMock.Object, _console);

    var output = ancestorFinder.GetAncestor("");

    Assert.That(output, Is.EqualTo("two"));
    readerMock.Verify();
  }
}