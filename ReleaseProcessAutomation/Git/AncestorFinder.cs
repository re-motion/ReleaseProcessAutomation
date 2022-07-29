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
using ReleaseProcessAutomation.ReadInput;
using Serilog;
using Spectre.Console;

namespace ReleaseProcessAutomation.Git;

public interface IAncestorFinder
{
  string GetAncestor (params string[] expectedAncestors);
}

public class AncestorFinder
    : IAncestorFinder
{
  private readonly IAnsiConsole _console;
  private readonly IGitClient _gitClient;
  private readonly IInputReader _inputReader;
  private readonly ILogger _log = Log.ForContext<AncestorFinder>();

  public AncestorFinder (IGitClient gitClient, IInputReader inputReader, IAnsiConsole console)
  {
    _gitClient = gitClient;
    _inputReader = inputReader;
    _console = console;
  }

  public string GetAncestor (params string[] expectedAncestors)
  {
    _log.Debug("Trying to get ancestor from '{ExpectedAncestors}'", expectedAncestors);
    var foundAncestors = _gitClient.GetAncestors(expectedAncestors);

    if (foundAncestors.Count == 1)
      return foundAncestors.First();

    if (foundAncestors.Count == 0)
    {
      _log.Warning("Ancestors were expected but not found");
      _console.WriteLine("We expected some of the following ancestors but found none:");
      foreach (var ancestor in foundAncestors)
        _console.WriteLine(ancestor);

      var userInput = _inputReader.ReadString("Please enter the name of the ancestor branch:");
      _log.Debug("User input for the name of the ancestor branch was read: {UserInput}", userInput);
      return userInput;
    }

    _log.Warning("Multiple matching ancestors were found: {FoundAncestors}", foundAncestors);
    var input = _inputReader.ReadStringChoice("Please enter the name of the ancestor branch:", foundAncestors);
    _log.Debug("User input for the name of the ancestor branch was read: {UserInput}", input);
    return input;
  }
}