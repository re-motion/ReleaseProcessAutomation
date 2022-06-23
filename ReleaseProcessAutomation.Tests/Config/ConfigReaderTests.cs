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
using NUnit.Framework;
using ReleaseProcessAutomation.Configuration;
using ReleaseProcessAutomation.Git;

namespace ReleaseProcessAutomation.Tests.Config;

[TestFixture]
internal class ConfigReaderTests
{
  private const string c_configFileName = "ReleaseProcessScript.Test.Config";
  private const string c_buildProjectFileName = ".BuildProject";

  [Test]
  public void GetConfigPath_WithWrongPath_ThrowsException ()
  {
    var reader = new ConfigReader();
    var guid = Guid.NewGuid();

    Assert.That(
        () => reader.GetConfigPathFromBuildProject(
            guid.ToString()),
        Throws.InstanceOf<FileNotFoundException>()
            .With.Message.EqualTo(
                $"Could not get Config path from '.BuildProject' because the file '{guid}\\.BuildProject' does not exist"));
  }

  [Test]
  public void LoadConfig_WithWrongPath_ThrowsException ()
  {
    var reader = new ConfigReader();

    Assert.That(
        () => reader.LoadConfig(""),
        Throws.InstanceOf<FileNotFoundException>()
            .With.Message.EqualTo("Could not Load Config from '' because the file does not exist"));
  }

  [Test]
  public void LoadConfig_InCurrentWorkingDir_LoadsConfigToProperty ()
  {
    var reader = new ConfigReader();
    var configFilePath = Path.Combine(TestContext.CurrentContext.TestDirectory, c_configFileName);

    var config = reader.LoadConfig(configFilePath);

    Assert.That(config.Jira.UseNTLM, Is.False);
    Assert.That(config.Jira.JiraURL, Is.EqualTo("https://re-motion.atlassian.net"));
    Assert.That(config.ResourceStrings.Resources, Does.Contain("v{version}"));
    Assert.That(config.PrepareNextVersionMSBuildSteps.Steps[0].MSBuildCallArguments.Arguments[0], Is.EqualTo("TestBuild.build"));
  }

  [Test]
  public void GetConfigPathFromBuildProject_InCurrentWorkingDir_GetsProperName ()
  {
    var reader = new ConfigReader();

    var configPath = reader.GetConfigPathFromBuildProject(TestContext.CurrentContext.TestDirectory);

    Assert.That(configPath, Is.EqualTo("Build/Customizations/ReleaseProcessScript.Test.config"));
  }
}