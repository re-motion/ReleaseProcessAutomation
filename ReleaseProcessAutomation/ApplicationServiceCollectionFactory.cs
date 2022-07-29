using System;
using Microsoft.Extensions.DependencyInjection;
using ReleaseProcessAutomation.Configuration;
using ReleaseProcessAutomation.Git;
using ReleaseProcessAutomation.Jira;
using ReleaseProcessAutomation.Jira.Authentication;
using ReleaseProcessAutomation.Jira.CredentialManagement;
using ReleaseProcessAutomation.Jira.ServiceFacadeImplementations;
using ReleaseProcessAutomation.Jira.ServiceFacadeInterfaces;
using ReleaseProcessAutomation.Jira.Utility;
using ReleaseProcessAutomation.MSBuild;
using ReleaseProcessAutomation.ReadInput;
using ReleaseProcessAutomation.Scripting;
using ReleaseProcessAutomation.Steps;
using ReleaseProcessAutomation.Steps.PipelineSteps;

namespace ReleaseProcessAutomation;

public class ApplicationServiceCollectionFactory
{
  public ServiceCollection CreateServiceCollection ()
  {
    var services = new ServiceCollection();
    services
        .AddTransient<IGitClient, CommandLineGitClient>()
        .AddTransient<IInputReader, InputReader>()
        .AddTransient<IMSBuild, MSBuild.MSBuild>()
        .AddTransient<IMSBuildCallAndCommit, MSBuildCallAndCommit>()
        .AddTransient<ISemanticVersionedGitRepository, SemanticVersionedGitRepository>()
        .AddTransient<IAncestorFinder, AncestorFinder>()

        //Jira things
        .AddTransient<IJiraCredentialManager, JiraCredentialManager>()
        .AddTransient<IJiraCredentialAPI, AdysTechCredentialApi>()
        .AddTransient<IJiraAuthenticator, JiraAuthenticator>()
        .AddTransient<IJiraFunctionality, JiraFunctionality>()
        .AddSingleton<IJiraRestClientProvider, JiraRestClientProvider>()
        .AddTransient<IJiraVersionCreator, JiraVersionCreator>()
        .AddTransient<IJiraVersionReleaser, JiraVersionReleaser>()
        .AddTransient<IJiraProjectVersionFinder, JiraProjectVersionFinder>()
        .AddTransient<IJiraProjectVersionService, JiraProjectVersionService>()
        .AddTransient<IJiraProjectVersionRepairer, JiraProjectVersionRepairer>()
        .AddTransient<IJiraIssueService, JiraIssueService>()

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
}