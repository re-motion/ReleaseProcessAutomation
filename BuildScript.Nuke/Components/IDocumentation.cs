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
using JetBrains.Annotations;
using Nuke.Common;
using Remotion.BuildScript.Components.Tasks;

namespace Remotion.BuildScript.Components;

public interface IDocumentation : IBaseBuild
{
  [Parameter("Skip generating documentation - true / false")]
  public bool SkipDocumentation => TryGetValue<bool?>(() => SkipDocumentation) ?? false;

  [PublicAPI]
  public Target GenerateDocumentation => _ => _
      .DependsOn<ICompile>(x => x.CompileReleaseBuild)
      .DependsOn<ICleanup>(x => x.CleanFolders)
      .Description("Generates documentation with sandcastle")
      .OnlyWhenStatic(() => !SkipDocumentation)
      .Executes(() =>
      {
        var documentOutputFile = DocumentationTask.CreateSandcastleProjectFile(
            ConfigurationData.ReleaseProjectFiles,
            ConfigurationData.AssemblyMetadata,
            Directories,
            ConfigurationData.SemanticVersion);
        DocumentationTask.CreateDocumentationNugetPackage(
            ConfigurationData.ReleaseProjectFiles,
            documentOutputFile,
            ConfigurationData.AssemblyMetadata,
            ConfigurationData.SemanticVersion,
            Directories);
      });
}
