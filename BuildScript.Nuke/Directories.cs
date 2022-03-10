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
using Nuke.Common.IO;

public class Directories
{
  /// <summary>
  ///   Path to the project folder
  /// </summary>
  public AbsolutePath Solution { get; }

  /// <summary>
  ///   <see cref="Solution" />/BuildOutput/
  ///   Subfolder of the solution directory named "BuildOutput"
  ///   Is handed over to MSBuild during the compile step <see cref="BaseBuild.CompileReleaseBuild" />
  /// </summary>
  public AbsolutePath Output { get; }

  /// <summary>
  ///   <see cref="Output" />/temp/
  ///   Temporary subfolder of the solution directory
  ///   Is handed over to MSBuild during the compile step <see cref="Build.CompileReleaseBuild" />
  /// </summary>
  public AbsolutePath Temp { get; }

  /// <summary>
  ///   <see cref="Output" />/log/
  ///   Is handed over to MSBuild during the compile step <see cref="Build.CompileReleaseBuild" />
  /// </summary>
  public AbsolutePath Log { get; }

  /// <summary>
  ///   <see cref="Solution" />/remotion.snk
  ///   Is handed over to MSBuild during the compile step <see cref="BaseBuild.CompileReleaseBuild" />
  /// </summary>
  public AbsolutePath SolutionKeyFile { get; }

  /// <summary>
  ///   <see cref="Solution" />/BuildScript.Nuke/Customizations
  ///   Contains configuration files for the build
  ///   Which are loaded in the <see cref="Build.ImportPropertiesDefinition" />
  /// </summary>
  public AbsolutePath CustomizationPath { get; }

  public Directories (AbsolutePath root, AbsolutePath buildProjectDirectory)
  {
    Solution = root;
    Output = Solution / "BuildOutput/";
    Temp = Output / "temp/";
    Log = Output / "log/";
    SolutionKeyFile = Solution / "remotion.snk";
    CustomizationPath = buildProjectDirectory / "Customizations";
  }
}