using Microsoft.Extensions.DependencyInjection;
using ReleaseProcessAutomation.Commands;
using Spectre.Console.Cli;

namespace ReleaseProcessAutomation;

public class ApplicationCommandAppFactory
{
  public CommandApp CreateConfiguredCommandApp (IServiceCollection services)
  {
    var app = new CommandApp(new TypeRegistrar(services));

    app.Configure(
        config =>
        {
          config.CaseSensitivity(CaseSensitivity.None);
          config.ConfigureConsole(Program.Console);
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