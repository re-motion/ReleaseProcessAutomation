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
using Microsoft.Extensions.DependencyInjection;
using ReleaseProcessAutomation.Commands;
using ReleaseProcessAutomation.Configuration;
using ReleaseProcessAutomation.Git;
using ReleaseProcessAutomation.MSBuild;
using ReleaseProcessAutomation.ReadInput;
using ReleaseProcessAutomation.Scripting;
using ReleaseProcessAutomation.Steps;
using ReleaseProcessAutomation.Steps.PipelineSteps;
using Serilog;
using Serilog.Core;
using Spectre.Console;
using Spectre.Console.Cli;

namespace ReleaseProcessAutomation;

[ExcludeFromCodeCoverage]
public static class Program
{
  public static IAnsiConsole Console { get; set; } = AnsiConsole.Console;

  public static int Main (string[] args)
  {
    ConfigureLogger();
    
    var services = ConfigureServices();
    var app = ConfigureCommandApp(services);

   
    
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

  private static ServiceCollection ConfigureServices ()
  {
    var services = new ServiceCollection();
    services
        .AddTransient<IGitClient, CommandLineGitClient>()
        .AddTransient<IInputReader, InputReader>()
        .AddTransient<IMSBuild, MSBuild.MSBuild>()
        .AddTransient<IMSBuildCallAndCommit, MSBuildCallAndCommit>()
        .AddTransient<ISemanticVersionedGitRepository, SemanticVersionedGitRepository>()
        .AddTransient<IAncestorFinder, AncestorFinder>()

        //Different invoked methods
        .AddTransient<IStartReleaseStep, StartReleaseStep>()
        .AddTransient<IContinueRelease, ContinueReleaseStep>()

        //Initial branching for release version
        .AddTransient<IBranchFromDevelopStep, BranchFromDevelopStep>()
        .AddTransient<IBranchFromHotfixStep, BranchFromHotfixStep>()
        .AddTransient<IBranchFromMasterStep, BranchFromMasterStep>()
        .AddTransient<IBranchFromReleaseStep, BranchFromReleaseStep>()

        //Initial Branching for continue version
        .AddTransient<IBranchFromPreReleaseForContinueVersionStep, BranchFromPreReleaseForContinueVersionStep>()
        .AddTransient<IBranchFromReleaseForContinueVersionStep, BranchFromReleaseForContinueVersionStep>()

        //Actual release behaviour
        .AddTransient<IReleasePatchStep, ReleasePatchStep>()
        .AddTransient<IReleaseOnMasterStep, ReleaseOnMasterStep>()
        .AddTransient<IReleaseAlphaBetaStep, ReleaseAlphaBetaStep>()
        .AddTransient<IReleaseRCStep, ReleaseRCStep>()
        .AddTransient<IReleaseWithRcStep, ReleaseWithRcStep>()

        //Continuation of actual release behaviour
        .AddTransient<IContinueReleaseOnMasterStep, ContinueReleaseOnMasterStep>()
        .AddTransient<IContinueReleasePatchStep, ContinueReleasePatchStep>()
        .AddTransient<IContinueAlphaBetaStep, ContinueAlphaBetaStep>()

        //Push behaviour
        .AddTransient<IPushMasterReleaseStep, PushMasterReleaseStep>()
        .AddTransient<IPushPreReleaseStep, PushPreReleaseStep>()
        .AddTransient<IPushPatchReleaseStep, PushPatchReleaseStep>()
        .AddSingleton(
            _ =>
            {
              var configReader = new ConfigReader();
              var pathToConfig = configReader.GetConfigPathFromBuildProject(Environment.CurrentDirectory);
              return configReader.LoadConfig(pathToConfig);
            });
    return services;
  }

  private static CommandApp ConfigureCommandApp (IServiceCollection services)
  {
    var app = new CommandApp(new TypeRegistrar(services));

    app.Configure(
        config =>
        {
          config.CaseSensitivity(CaseSensitivity.None);
          config.ConfigureConsole(Console);
          config.PropagateExceptions();
          config.SetApplicationName("Release Process Script");

          //Calls StartReleaseStep
          config.AddCommand<ReleaseVersionCommand>("Release-Version")
              .WithDescription("Releases a new Version");

          //Calls ContinueReleaseStep
          config.AddCommand<CloseVersionCommand>("Close-Version")
              .WithDescription("Complete the Version process");

          //Calls StartReleaseStep with StartReleasePhase set to true
          config.AddCommand<ReleaseBranchCommand>("New-Release-Branch")
              .WithDescription("Creates a new release Branch");

          //Calls PushToRepos from the given GitClient
          config.AddCommand<PushRemoteCommand>("Push-Remote-Repositories")
              .WithAlias("Push-Remote-Repos")
              .WithDescription("Push given branch to the remote repositories defined in releaseProcessScript.config");
        });
    return app;
  }
}
