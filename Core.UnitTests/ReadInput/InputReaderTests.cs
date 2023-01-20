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
using NUnit.Framework;
using Remotion.ReleaseProcessAutomation.Extensions;
using Remotion.ReleaseProcessAutomation.ReadInput;
using Remotion.ReleaseProcessAutomation.SemanticVersioning;
using Spectre.Console.Testing;

namespace Remotion.ReleaseProcessAutomation.UnitTests.ReadInput;

[TestFixture]
internal class InputReaderTests
{
  [Test]
  [TestCase("y", true)]
  [TestCase("Y", true)]
  [TestCase("n", false)]
  [TestCase("N", false)]
  public void ReadConfirmation_WithInput_ReturnsOutput (string inputText, bool expectedOutput)
  {
    var testConsole = new TestConsole();
    testConsole.Input.PushTextWithEnter(inputText);
    var inputReader = new InputReader(testConsole);

    var result = inputReader.ReadConfirmation();

    Assert.That(result, Is.EqualTo(expectedOutput));
  }

  [Test]
  public void ReadConfirmation_WithFirstIllegalInput_WithSecondOKInput_AsksUserAgainAfterFirstInput ()
  {
    var testConsole = new TestConsole();
    testConsole.Input.PushTextWithEnter("notACorrectInput");
    testConsole.Input.PushTextWithEnter("y");
    var inputReader = new InputReader(testConsole);

    var result = inputReader.ReadConfirmation();

    var lines = testConsole.Output.Split("\n");
    Assert.That(lines[0], Is.EqualTo("Confirm? [y/n] (y): notACorrectInput"));
    Assert.That(lines[1], Is.EqualTo("The input 'notACorrectInput' is not a valid option."));
    Assert.That(lines[2], Is.EqualTo("Confirm? [y/n] (y): y"));
    Assert.That(result, Is.EqualTo(true));
  }

  [Test]
  [TestCase(true, "Confirm? [y/n] (y)")]
  [TestCase(false, "Confirm? [y/n] (n)")]
  public void ReadConfirmation_WithDifferingDefaultValues_ChangesYesNoAtEndOfMessageDependingOnDefaultValue (bool defaultValue, string consoleMessage)
  {
    var testConsole = new TestConsole();
    testConsole.Input.PushTextWithEnter("y");
    var inputReader = new InputReader(testConsole);

    var result = inputReader.ReadConfirmation(defaultValue);

    Assert.That(testConsole.Output, Does.Contain(consoleMessage));
  }

  [Test]
  [TestCase(true)]
  [TestCase(false)]
  public void ReadConfirmation_WithEnterInput_ReturnsDefault (bool defaultValue)
  {
    var testConsole = new TestConsole();
    testConsole.Input.PushKey(ConsoleKey.Enter);
    var inputReader = new InputReader(testConsole);

    var result = inputReader.ReadConfirmation(defaultValue);

    Assert.That(result, Is.EqualTo(defaultValue));
  }

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
  public void ReadVersionChoice_WithIndexedInput_ReturnsIndexedVersion ()
  {
    var nextVersions = new SemanticVersion().GetNextPossibleVersionsDevelop(true);
    var testConsole = new TestConsole();
    testConsole.Input.PushTextWithEnter("2");
    var inputReader = new InputReader(testConsole);

    var act = inputReader.ReadVersionChoice("", nextVersions);

    Assert.That(act, Is.EqualTo(new SemanticVersion { Major = 1 }));
  }

  [Test]
  public void ReadVersionChoice_WithNormalInput_ReturnsSpecifiedVersion ()
  {
    var nextVersions = new SemanticVersion().GetNextPossibleVersionsDevelop(true);
    var testConsole = new TestConsole();
    testConsole.Input.PushTextWithEnter("1.0.0");
    var inputReader = new InputReader(testConsole);

    var act = inputReader.ReadVersionChoice("", nextVersions);

    Assert.That(act, Is.EqualTo(new SemanticVersion { Major = 1 }));
  }

  [Test]
  public void ReadStringChoice_WithInteractiveConsole_ReturnsThirdString ()
  {
    var strings = new[] { "foo", "bar", "faz", "foobar" };

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
  public void ReadStringChoice_WithNormalInput_ReturnsSpecifiedString ()
  {
    var strings = new[] { "foo", "bar", "faz", "foobar" };

    var testConsole = new TestConsole();
    testConsole.Input.PushTextWithEnter("bar");
    var inputReader = new InputReader(testConsole);

    var act = inputReader.ReadStringChoice("", strings);

    Assert.That(act, Is.EqualTo(strings[1]));
  }

  [Test]
  public void ReadStringChoice_WithIndexedInput_ReturnsIndexedString ()
  {
    var strings = new[] { "foo", "bar", "faz", "foobar" };

    var testConsole = new TestConsole();
    testConsole.Input.PushTextWithEnter("2");
    var inputReader = new InputReader(testConsole);

    var act = inputReader.ReadStringChoice("", strings);

    Assert.That(act, Is.EqualTo(strings[1]));
  }

  [Test]
  public void ReadStringChoice_WithoutInput_ThrowsNoMoreInputAvailable ()
  {
    var strings = new[] { "foo", "bar", "faz", "foobar" };

    var testConsole = new TestConsole();
    testConsole.Input.PushTextWithEnter("-1");
    var inputReader = new InputReader(testConsole);

    Assert.That(
        () => inputReader.ReadStringChoice("", strings),
        Throws.InstanceOf<InvalidOperationException>()
            .With.Message.EqualTo("No input available."));
  }
}