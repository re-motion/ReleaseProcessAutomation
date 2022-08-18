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
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using NUnit.Framework;
using ReleaseProcessAutomation.Jira;
using ReleaseProcessAutomation.Jira.Utility;
using ReleaseProcessAutomation.SemanticVersioning;
using ReleaseProcessAutomation.Steps.SubSteps;
using Spectre.Console.Testing;

namespace ReleaseProcessAutomation.IntegrationTests;

public abstract class IntegrationTestSetup : GitBackedTests
{
  protected const string c_testConfigName = "ReleaseProcessScript.Test.config";
  private const string c_buildProject = ".BuildProject";
  private const string c_buildFileName = "TestBuild.build";

  protected TestConsole TestConsole;

  [SetUp]
  public virtual void Setup ()
  {
    TestConsole = new TestConsole();
    Program.Console = TestConsole;

    var pathToBuildProject = Path.Combine(PreviousWorkingDirectory, c_buildProject);
    var destBuildProject = Path.Combine(RepositoryPath, c_buildProject);
    File.Copy(pathToBuildProject, destBuildProject);

    var pathToConfig = Path.Combine(PreviousWorkingDirectory, c_testConfigName);
    var destConfigFolder = Path.Combine(RepositoryPath, "Build", "Customizations");
    var destConfigFile = Path.Combine(destConfigFolder, c_testConfigName);
    Directory.CreateDirectory(destConfigFolder);
    File.Copy(pathToConfig, destConfigFile);

    var pathToBuildFile = Path.Combine(PreviousWorkingDirectory, c_buildFileName);
    var destBuildFile = Path.Combine(RepositoryPath, c_buildFileName);
    File.Copy(pathToBuildFile, destBuildFile);

    ExecuteGitCommand("add --all --force");
    ExecuteGitCommand("commit -m ConfigAndBuildProject");
  }

  protected void AssertValidLogs (string expectedLogs)
  {
    expectedLogs = expectedLogs.Replace(" ", "").Replace("\r", "");

    var logs = ExecuteGitCommandWithOutput("log --all --graph --oneline --decorate --pretty=%d%s");
    logs = logs.Replace(" ", "");

    Assert.That(logs, Is.EqualTo(expectedLogs));
  }

  protected void AssertValidLogs (string expectedLogs1, string expectedLogs2)
  {
    expectedLogs1 = expectedLogs1.Replace(" ", "").Replace("\r", "");
    expectedLogs2 = expectedLogs2.Replace(" ", "").Replace("\r", "");

    var logs = ExecuteGitCommandWithOutput("log --all --graph --oneline --decorate --pretty=%d%s");
    logs = logs.Replace(" ", "");

    Assert.That(logs, Is.EqualTo(expectedLogs1).Or.EqualTo(expectedLogs2));
  }

  protected int RunProgram (string[] args)
  {
    var services = new ApplicationServiceCollectionFactory().CreateServiceCollection();
    var releaseVersionAndMoveIssuesMock = new Mock<IReleaseVersionAndMoveIssuesSubStep>();
    releaseVersionAndMoveIssuesMock
        .Setup(_ => _.Execute(It.IsAny<SemanticVersion>(), It.IsAny<SemanticVersion>(), It.IsAny<bool>(), It.IsAny<bool>()));
    var releaseVersionAndMoveIssuesDescriptor = new ServiceDescriptor(
        typeof(IReleaseVersionAndMoveIssuesSubStep),
        x => releaseVersionAndMoveIssuesMock.Object,
        ServiceLifetime.Singleton);

    var jiraRestClientProviderStub = new Mock<IJiraRestClientProvider>();
    var jiraRestClientProviderDescriptor = new ServiceDescriptor(
        typeof(IJiraRestClientProvider),
        x => jiraRestClientProviderStub.Object,
        ServiceLifetime.Singleton);

    services.Replace(releaseVersionAndMoveIssuesDescriptor);
    services.Replace(jiraRestClientProviderDescriptor);

    var app = new ApplicationCommandAppFactory().CreateConfiguredCommandApp(services);

    return app.Run(args);
  }
}