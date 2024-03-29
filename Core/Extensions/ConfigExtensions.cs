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
using System.Collections.Generic;
using System.Linq;
using Remotion.ReleaseProcessAutomation.Configuration.Data;
using Serilog;

namespace Remotion.ReleaseProcessAutomation.Extensions;

public static class ConfigExtensions
{
  private static readonly ILogger s_log = Log.ForContext(typeof(ConfigExtensions));

  public static IReadOnlyCollection<string> GetIgnoredFiles (this Config config, IgnoreListType ignoreListType)
  {
    return ignoreListType switch
    {
        IgnoreListType.DevelopStableMergeIgnoreList => config.DevelopStableMergeIgnoreList.FileName?.Where(n => n is { Length: > 0 })?.ToArray() ?? Array.Empty<string>(),
        IgnoreListType.PreReleaseMergeIgnoreList => config.PreReleaseMergeIgnoreList.FileName?.Where(n => n is { Length: > 0 })?.ToArray() ?? Array.Empty<string>(),
        _ => Array.Empty<string>()
    };
  }

  public static MSBuildSteps GetMSBuildSteps (this Config config, MSBuildMode msBuildMode)
  {
    if (msBuildMode == MSBuildMode.PrepareNextVersion)
      return config.PrepareNextVersionMSBuildSteps;

    if (msBuildMode == MSBuildMode.DevelopmentForNextRelease)
      return config.DevelopmentForNextReleaseMSBuildSteps;

    const string message = "Invalid parameter in InvokeMSBuildAndCommit. No MSBuild steps were completed. Please check if MSBuildMode parameter is equivalent with the value in the config.";
    throw new ArgumentException(message);
  }
}