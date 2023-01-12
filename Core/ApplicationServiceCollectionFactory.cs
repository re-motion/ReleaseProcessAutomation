using System;
using Microsoft.Extensions.DependencyInjection;
using Remotion.ReleaseProcessAutomation.Configuration;
using Remotion.ReleaseProcessAutomation.Git;
using Remotion.ReleaseProcessAutomation.Jira;
using Remotion.ReleaseProcessAutomation.Jira.Authentication;
using Remotion.ReleaseProcessAutomation.Jira.CredentialManagement;
using Remotion.ReleaseProcessAutomation.Jira.ServiceFacadeImplementations;
using Remotion.ReleaseProcessAutomation.Jira.ServiceFacadeInterfaces;
using Remotion.ReleaseProcessAutomation.Jira.Utility;
using Remotion.ReleaseProcessAutomation.MSBuild;
using Remotion.ReleaseProcessAutomation.ReadInput;
using Remotion.ReleaseProcessAutomation.Scripting;
using Remotion.ReleaseProcessAutomation.Steps;
using Remotion.ReleaseProcessAutomation.Steps.PipelineSteps;
using Remotion.ReleaseProcessAutomation.Steps.SubSteps;

namespace Remotion.ReleaseProcessAutomation;

public class ApplicationServiceCollectionFactory
{
  public ServiceCollection CreateServiceCollection ()
  {
    var services = new ServiceCollection();
    services
        .AddTransient<IGitClient, CommandLineGitClient>()
        .AddTransient<IGitBranchOperations, GitBranchOperations>()
        .AddTransient<IInputReader, InputReader>()
        .AddTransient<IMSBuild, MSBuild.MSBuild>()
        .AddTransient<IMSBuildCallAndCommit, MSBuildCallAndCommit>()
        .AddTransient<ISemanticVersionedGitRepository, SemanticVersionedGitRepository>()
        .AddTransient<IAncestorFinder, AncestorFinder>()

        //Jira things
        .AddTransient<IJiraCredentialManager, JiraCredentialManager>()
        .AddTransient<IJiraCredentialAPI, AdysTechCredentialApi>()
        .AddTransient<IJiraAuthenticator, JiraAuthenticator>()
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
        .AddTransient<IReleaseWithRCStep, ReleaseWithRCStep>()

        //Continuation of actual release behaviour
        .AddTransient<IContinueReleaseOnMasterStep, ContinueReleaseOnMasterStep>()
        .AddTransient<IContinueReleasePatchStep, ContinueReleasePatchStep>()
        .AddTransient<IContinueAlphaBetaStep, ContinueAlphaBetaStep>()

        //Push behaviour
        .AddTransient<IPushMasterReleaseStep, PushMasterReleaseStep>()
        .AddTransient<IPushPreReleaseStep, PushPreReleaseStep>()
        .AddTransient<IPushPatchReleaseStep, PushPatchReleaseStep>()
        .AddTransient<IPushNewReleaseBranchStep, PushNewReleaseBranchStep>()

        //Sub steps
        .AddTransient<IReleaseVersionAndMoveIssuesSubStep, ReleaseVersionAndMoveIssuesSubStep>()
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