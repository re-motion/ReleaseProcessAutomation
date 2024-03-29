﻿// Copyright (c) rubicon IT GmbH, www.rubicon.eu
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
using System.Diagnostics;
using Serilog;
using Spectre.Console;

namespace Remotion.ReleaseProcessAutomation.MSBuild;

public interface IMSBuild
{
  void CallMSBuild (string msBuildPath, string arguments);
}

internal class MSBuild
    : IMSBuild
{
  private const int c_msBuildProcessTimeout = 30_000;

  private readonly IAnsiConsole _console;
  private readonly ILogger _log = Log.ForContext<MSBuild>();

  public MSBuild (IAnsiConsole console)
  {
    _console = console;
  }

  public void CallMSBuild (string msBuildPath, string arguments)
  {
    _log.Debug("Starting to call MSBuild from '{MSBuildPath}' with arguments '{Arguments}'.", msBuildPath, arguments);

    var msBuildStartInfo = new ProcessStartInfo(msBuildPath, arguments);
    msBuildStartInfo.UseShellExecute = false;
    msBuildStartInfo.RedirectStandardOutput = true;
    msBuildStartInfo.WorkingDirectory = Environment.CurrentDirectory;

    var msBuildProcess = Process.Start(msBuildStartInfo);

    // a deadlock condition can occur if the parent process calls p.waitForExit before p.StandardOutput.ReadToEnd
    // and the child process writes enough text to fill the redirected stream.
    // This is due to the parent process waiting indefinitely for the child process to exit,
    // while the child process waits for the parent to read from the full stream.
    var msBuildOutput = msBuildProcess!.StandardOutput.ReadToEnd();
    msBuildProcess.WaitForExit(c_msBuildProcessTimeout);

    if (msBuildProcess.ExitCode != 0)
    {
      var message = $"MSBuild '{arguments}' failed with Error: '{msBuildOutput}'.";
      throw new InvalidOperationException(message);
    }

    _console.WriteLine(msBuildOutput);

    var successMessage = $"Successfully called MSBuild with '{arguments}'.";
    _log.Information(successMessage);
    _console.WriteLine(successMessage);
  }
}