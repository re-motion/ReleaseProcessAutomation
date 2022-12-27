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
using Remotion.ReleaseProcessAutomation.SemanticVersioning;
using Spectre.Console;

namespace Remotion.ReleaseProcessAutomation.ReadInput;

public class InputReader
    : IInputReader
{
  private const string c_moreChoicesText = "[grey](Move up and down to choose version)[/]";

  private readonly IAnsiConsole _console;
  private readonly Style _textPromptStyle;
  private readonly Style _selectionPromptHighlightStyle;
  //standard console colors, #10 & #9 respectively
  private readonly string _yesColor = "lime";
  private readonly string _noColor = "red";

  public InputReader (IAnsiConsole console)
  {
    _console = console;
    var style = new Style(foreground: ConsoleColor.Green);
    _textPromptStyle = style;
    _selectionPromptHighlightStyle = style;
  }

  public string ReadHiddenString (string prompt)
  {
    return _console.Prompt(
        new TextPrompt<string>(prompt)
            .Secret()
    );
  }

  public bool ReadConfirmation (bool defaultValue = true)
  {
    var input = _console.Prompt(
        new TextPrompt<string>($"Confirm? [[[{_yesColor}]y[/]/[{_noColor}]n[/]]] ({(defaultValue ? $"[{_yesColor}]y[/]" : $"[{_noColor}]n[/]")}):" )
            .Validate(input => input is "y" or "Y" or "n" or "N" or "" ? ValidationResult.Success() : ValidationResult.Error($"The input '{input}' is not a valid option."))
            .AllowEmpty());
    return input switch
      {
        "y" or "Y" => true,
        "n" or "N" => false,
        _ => defaultValue
      };
  }

  public string ReadString (string prompt)
  {
    return _console.Ask<string>(prompt);
  }

  public SemanticVersion ReadSemanticVersion (string prompt)
  {
    var parser = new SemanticVersionParser();
    var input = _console.Prompt(
        new TextPrompt<string>(prompt)
            .PromptStyle(_textPromptStyle)
            .ValidationErrorMessage("That's not a valid version.")
            .Validate(version => parser.TryParseVersion(version, out _) ? ValidationResult.Success() : ValidationResult.Error()));
    return parser.ParseVersion(input);
  }

  public SemanticVersion ReadVersionChoice (string prompt, IReadOnlyCollection<SemanticVersion> possibleVersions)
  {
    if (_console.Profile.Capabilities.Interactive)
      return _console.Prompt(
          new SelectionPrompt<SemanticVersion>()
              .HighlightStyle(_selectionPromptHighlightStyle)
              .Title(prompt)
              .MoreChoicesText(c_moreChoicesText)
              .AddChoices(possibleVersions)
      );

    var orderedPossibleVersions = possibleVersions.ToArray();
    var versionsAsStrings = orderedPossibleVersions.Select(_ => _.ToString()).ToArray();
    var textPrompt = BuildNonInteractiveStringChoicePrompt(prompt, versionsAsStrings);
    var parser = new SemanticVersionParser();
    var chosenVersion = _console.Prompt(textPrompt);

    if (int.TryParse(chosenVersion, out var indexedVersion))
      return orderedPossibleVersions[indexedVersion - 1];

    return parser.ParseVersion(chosenVersion);
  }

  /// <summary>
  ///   Should never be called with possible answers that are just integers, as it messes with the way the values are
  ///   validated
  /// </summary>
  /// <param name="prompt">The prompt to be displayed</param>
  /// <param name="possibleAnswers">Must never just be integers between 0 and the number of possible answers</param>
  public string ReadStringChoice (string prompt, IReadOnlyCollection<string> possibleAnswers)
  {
    if (_console.Profile.Capabilities.Interactive)
      return _console.Prompt(
          new SelectionPrompt<string>()
              .HighlightStyle(_selectionPromptHighlightStyle)
              .Title(prompt)
              .MoreChoicesText(c_moreChoicesText)
              .AddChoices(possibleAnswers)
      );

    var orderedAnswers = possibleAnswers.ToArray();
    var textPrompt = BuildNonInteractiveStringChoicePrompt(prompt, orderedAnswers);
    var chosenAnswer = _console.Prompt(textPrompt);

    if (int.TryParse(chosenAnswer, out var index))
      return orderedAnswers[index - 1];

    return chosenAnswer;
  }

  private TextPrompt<string> BuildNonInteractiveStringChoicePrompt (string prompt, IReadOnlyList<string> versions)
  {
    var promptBuilder = new StringBuilder(prompt);
    promptBuilder.AppendLine();

    foreach (var (v, oneBasedIndex) in versions.Select((v, i) => (v, i + 1)))
      promptBuilder.Append("(").Append(oneBasedIndex).Append(") ").Append(v).AppendLine("");

    promptBuilder.Append("Your version: ");

    return new TextPrompt<string>(promptBuilder.ToString())
        .PromptStyle(_textPromptStyle)
        .ShowChoices(false)
        .AddChoices(versions)
        .AddChoices(versions.Select((_, i) => (i + 1).ToString()).ToArray())
        .Validate(
            v =>
            {
              if (int.TryParse(v, out var indexedVersion))
              {
                if (indexedVersion > 0 && indexedVersion <= versions.Count)
                  return ValidationResult.Success();
                return ValidationResult.Error(
                    $"The input '{v}' is not a valid option, please enter a valid version or select a valid option.");
              }

              if (versions.Contains(v))
                return ValidationResult.Success();
              return ValidationResult.Error(
                  $"The input '{v}' is not a valid option, please enter a valid version or select a valid option.");
            });
  }
}