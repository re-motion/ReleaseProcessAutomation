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
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Serilog;
using Spectre.Console;

namespace ReleaseProcessAutomation;

[ExcludeFromCodeCoverage]
public static class Program
{
  public static IAnsiConsole Console { get; set; } = AnsiConsole.Console;

  public static int Main (string[] args)
  {
    ConfigureLogger();

    var services = new ApplicationServiceCollectionFactory().CreateServiceCollection();
    var app = new ApplicationCommandAppFactory().CreateConfiguredCommandApp(services);

    try
    {
      app.Run(args);
    }
    catch (Exception e)
    {
      Console.WriteException(e, ExceptionFormats.ShortenEverything);
      var log = Log.ForContext(e.TargetSite!.DeclaringType);
      log.Error(e.Message);
      return -1;
    }
    finally
    {
      (Log.Logger as IDisposable)?.Dispose();
    }

    return 0;
  }

  private static void ConfigureLogger ()
  {
    var tempPath = Path.GetTempPath();
    var rpsTempLog = Path.Combine(tempPath, "ReleaseProcessScript", "logs", "rps.log");

    var log = new LoggerConfiguration()
        .MinimumLevel.Verbose()
        .Enrich.FromLogContext()
        .WriteTo.Debug(outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level}] ({SourceContext}) {Message}{NewLine}{Exception}")
        .WriteTo.File(rpsTempLog, outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level}] ({SourceContext}) {Message}{NewLine}{Exception}")
        .CreateLogger();

    Log.Logger = log;
  }
}