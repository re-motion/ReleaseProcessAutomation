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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using ReleaseProcessScript.New.Extensions;
using ReleaseProcessScript.New.ReadInput;
using ReleaseProcessScript.New.SemanticVersioning;
using Spectre.Console;
using Spectre.Console.Testing;

namespace ReleaseProcessScript.New.Tests.ReadInput
{
  [TestFixture]
  internal class InputReaderTests
  {
    [Test]
    public void ReadVersionChoice_WithInteractiveConsole_ReturnsFirstVersion ()
    {
      var nextVersions = new SemanticVersion().GetNextPossibleVersionsDevelop(true);
      var testConsole = new TestConsole();
      testConsole.Interactive();
      testConsole.Input.PushKey(ConsoleKey.Enter);
      var inputReader = new InputReader(testConsole);

      var act = inputReader.ReadVersionChoice("", nextVersions);

      Assert.That(act, Is.EqualTo(nextVersions.First()));
    }

    [Test]
    public void ReadVersionChoice_WithoutInteractiveConsole_ReturnsIndexedVersion()
    {
      var nextVersions = new SemanticVersion().GetNextPossibleVersionsDevelop(true);
      var testConsole = new TestConsole();
      testConsole.Input.PushTextWithEnter("2");
      var inputReader = new InputReader(testConsole);



      var act = inputReader.ReadVersionChoice("", nextVersions);

      var output = testConsole.Output;
      Assert.That(act, Is.EqualTo(new SemanticVersion{Major = 1}));
    }

    [Test]
    public void ReadVersionChoice_WithoutInteractiveConsole_ReturnsSpecifiedVersion()
    {
      var nextVersions = new SemanticVersion().GetNextPossibleVersionsDevelop(true);
      var testConsole = new TestConsole();
      testConsole.Input.PushTextWithEnter("1.0.0");
      var inputReader = new InputReader(testConsole);



      var act = inputReader.ReadVersionChoice("", nextVersions);

      var output = testConsole.Output;
      Assert.That(act, Is.EqualTo(new SemanticVersion { Major = 1 }));
    }

    [Test]
    public void ReadStringChoice_WithInteractiveConsole_ReturnsThirdString ()
    {
      var strings = new string[] { "foo" , "bar" , "faz", "foobar" };

      var testConsole = new TestConsole();
      testConsole.Interactive();
      testConsole.Input.PushKey(ConsoleKey.DownArrow);
      testConsole.Input.PushKey(ConsoleKey.DownArrow);
      testConsole.Input.PushKey(ConsoleKey.Enter);
      var inputReader = new InputReader(testConsole);

      var act = inputReader.ReadStringChoice("", strings);

      Assert.That(act, Is.EqualTo(strings[2]));
    }

    [Test]
    public void ReadStringChoice_WithoutInteractiveConsole_ReturnsSpecifiedString()
    {
      var strings = new string[] { "foo", "bar", "faz", "foobar" };

      var testConsole = new TestConsole();
      testConsole.Input.PushTextWithEnter("bar");
      var inputReader = new InputReader(testConsole);

      var act = inputReader.ReadStringChoice("", strings);

      Assert.That(act, Is.EqualTo(strings[1]));
    }

    [Test]
    public void ReadStringChoice_WithoutInteractiveConsole_ReturnsIndexedString()
    {
      var strings = new string[] { "foo", "bar", "faz", "foobar" };

      var testConsole = new TestConsole();
      testConsole.Input.PushTextWithEnter("2");
      var inputReader = new InputReader(testConsole);

      var act = inputReader.ReadStringChoice("", strings);

      Assert.That(act, Is.EqualTo(strings[1]));
    }

    [Test]
    public void ReadStringChoice_WithoutWrongInput_ThrowsNoMoreInputAvailable()
    {
      var strings = new string[] { "foo", "bar", "faz", "foobar" };

      var testConsole = new TestConsole();
      testConsole.Input.PushTextWithEnter("-1");
      var inputReader = new InputReader(testConsole);

      Assert.That(() => inputReader.ReadStringChoice("", strings),
          Throws.InstanceOf<InvalidOperationException>()
              .With.Message.EqualTo("No input available."));
    }
  }
}
