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

using System.Linq;
using Remotion.ReleaseProcessAutomation.Configuration.Data;
using Remotion.ReleaseProcessAutomation.SemanticVersioning;
using Serilog;
using Spectre.Console;

namespace Remotion.ReleaseProcessAutomation.MSBuild;

public static class MSBuildUtilities
{
  private static readonly ILogger s_log = Log.ForContext(typeof(MSBuildUtilities));

  public static string? GetMSBuildCallString (Step step, SemanticVersion version, IAnsiConsole console)
  {
    s_log.Debug("Building MS build call string from step '{Step}'.", step);

    if (step.MSBuildCallArguments.Arguments.Length == 0)
    {
      s_log.Warning("Did not find any MSBuildCallArguments.");
      return null;
    }

    var replacedArguments = step.MSBuildCallArguments.Arguments.Select(arg => arg.Replace("{version}", $"{version}").Replace("{Version}", $"{version}"));
    return string.Join(" ", replacedArguments);
  }
}