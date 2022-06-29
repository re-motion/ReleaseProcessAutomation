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
using ReleaseProcessAutomation.SemanticVersioning;
using Serilog;

namespace ReleaseProcessAutomation.Extensions;

public static class SemanticVersionExtensions
{
  private static readonly ILogger s_log = Log.ForContext(typeof(SemanticVersionExtensions));

  public static IReadOnlyCollection<SemanticVersion> GetNextPossibleVersionsDevelop (
      this SemanticVersion semanticVersion,
      bool withoutPreRelease = false)
  {
    s_log.Debug("Getting next possible develop version of '{SemanticVersion}'.", semanticVersion);

    if (semanticVersion.Pre != null)
    {
      if (withoutPreRelease)
        return new[] { GetCurrentFullVersion(semanticVersion), GetNextMajorRelease(semanticVersion) };

      if (semanticVersion.Pre == PreReleaseStage.alpha)
        return new[]
               {
                   GetNextPreReleaseVersion(semanticVersion),
                   GetNextPreReleaseStage(semanticVersion),
                   GetCurrentFullVersion(semanticVersion),
                   GetNextMajorWithAlpha(semanticVersion),
                   GetNextMajorWithBeta(semanticVersion),
                   GetNextMajorRelease(semanticVersion)
               };

      if (semanticVersion.Pre == PreReleaseStage.beta)
        return new[]
               {
                   GetNextPreReleaseVersion(semanticVersion),
                   GetCurrentFullVersion(semanticVersion),
                   GetNextMajorWithAlpha(semanticVersion),
                   GetNextMajorWithBeta(semanticVersion),
                   GetNextMajorRelease(semanticVersion)
               };

      if (semanticVersion.Pre == PreReleaseStage.rc)
        return new[]
               {
                   GetCurrentFullVersion(semanticVersion),
                   GetNextMajorWithAlpha(semanticVersion),
                   GetNextMajorWithBeta(semanticVersion),
                   GetNextMajorRelease(semanticVersion)
               };
    }

    if (withoutPreRelease)
      return new[]
             {
                 GetNextMinorRelease(semanticVersion),
                 GetNextMajorRelease(semanticVersion)
             };
    return new[]
           {
               GetNextMinorWithAlpha(semanticVersion),
               GetNextMinorWithBeta(semanticVersion),
               GetNextMinorRelease(semanticVersion),
               GetNextMajorWithAlpha(semanticVersion),
               GetNextMajorWithBeta(semanticVersion),
               GetNextMajorRelease(semanticVersion)
           };
  }

  public static IReadOnlyCollection<SemanticVersion> GetNextPossibleVersionsHotfix (this SemanticVersion semanticVersion)
  {
    s_log.Debug("Getting next possible hotfix version of '{SemanticVersion}'.", semanticVersion);

    var patchVersion = GetNextPatchVersion(semanticVersion);

    if (semanticVersion.Pre != null)
    {
      if (semanticVersion.Pre == PreReleaseStage.alpha)
        return new[]
               {
                   GetNextAlpha(semanticVersion),
                   GetNextBeta(semanticVersion),
                   GetNextPatchVersion(semanticVersion)
               };

      if (semanticVersion.Pre == PreReleaseStage.beta)
        return new[]
               {
                   GetNextPreReleaseVersion(semanticVersion),
                   GetNextPatchVersion(semanticVersion)
               };

      if (semanticVersion.Pre == PreReleaseStage.rc)
        return new[]
               {
                   GetNextRc(semanticVersion),
                   GetNextPatchVersion(semanticVersion)
               };

      const string message = "Could not get next possible next versions for hotfix.";
      throw new InvalidOperationException(message);
    }

    return new[]
           {
               GetNextAlpha(patchVersion),
               GetNextBeta(patchVersion),
               patchVersion
           };
  }

  public static IReadOnlyCollection<SemanticVersion> GetCurrentPossibleVersionsHotfix (this SemanticVersion semanticVersion)
  {
    s_log.Debug("Getting current possible develop version of '{SemanticVersion}'.", semanticVersion);

    if (semanticVersion.Pre == PreReleaseStage.alpha)
      return new[]
             {
                 GetNextAlpha(semanticVersion), GetNextBeta(semanticVersion), GetCurrentFullVersion(semanticVersion)
             };
    if (semanticVersion.Pre == PreReleaseStage.beta)
      return new[]
             {
                 GetNextBeta(semanticVersion), GetCurrentFullVersion(semanticVersion)
             };
    if (semanticVersion.Pre == PreReleaseStage.rc)
      return new[]
             {
                 GetNextPreReleaseVersion(semanticVersion), GetCurrentFullVersion(semanticVersion)
             };

    return new[]
           {
               GetNextAlpha(semanticVersion), GetNextBeta(semanticVersion), GetCurrentFullVersion(semanticVersion)
           };
  }

  public static SemanticVersion GetNextMinor (this SemanticVersion semanticVersion)
  {
    var nextVersion = new SemanticVersion
                      {
                          Major = semanticVersion.Major,
                          Minor = semanticVersion.Minor + 1,
                          Patch = 0,
                      };
    return nextVersion;
  }

  public static SemanticVersion GetNextRc (this SemanticVersion semanticVersion)
  {
    var nextVersion = new SemanticVersion
                      {
                          Major = semanticVersion.Major,
                          Minor = semanticVersion.Minor,
                          Patch = semanticVersion.Patch,
                          Pre = semanticVersion.Pre,
                          PreReleaseCounter = semanticVersion.PreReleaseCounter
                      };

    if (semanticVersion.Pre == PreReleaseStage.rc)
    {
      nextVersion.PreReleaseCounter++;
      return nextVersion;
    }

    nextVersion.Pre = PreReleaseStage.rc;
    nextVersion.PreReleaseCounter = 1;
    return nextVersion;
  }

  public static SemanticVersion GetNextPatchVersion (this SemanticVersion semanticVersion)
  {
    var nextPatchVersion = new SemanticVersion
                           {
                               Major = semanticVersion.Major,
                               Minor = semanticVersion.Minor,
                               Patch = semanticVersion.Patch + 1
                           };
    return nextPatchVersion;
  }

  public static SemanticVersion GetCurrentFullVersion (this SemanticVersion semanticVersion)
  {
    var nextPatchRelease = new SemanticVersion
                           {
                               Major = semanticVersion.Major,
                               Minor = semanticVersion.Minor,
                               Patch = semanticVersion.Patch
                           };
    return nextPatchRelease;
  }

  private static SemanticVersion GetNextAlpha (SemanticVersion semanticVersion, bool changeToAlpha1 = false)
  {
    var nextVersion = new SemanticVersion
                      {
                          Major = semanticVersion.Major,
                          Minor = semanticVersion.Minor,
                          Patch = semanticVersion.Patch,
                          Pre = semanticVersion.Pre,
                          PreReleaseCounter = semanticVersion.PreReleaseCounter
                      };

    if (semanticVersion.Pre == PreReleaseStage.alpha && !changeToAlpha1)
    {
      nextVersion.PreReleaseCounter++;
      return nextVersion;
    }

    nextVersion.Pre = PreReleaseStage.alpha;
    nextVersion.PreReleaseCounter = 1;
    return nextVersion;
  }

  private static SemanticVersion GetNextBeta (SemanticVersion semanticVersion, bool changeToBeta1 = false)
  {
    var nextVersion = new SemanticVersion
                      {
                          Major = semanticVersion.Major,
                          Minor = semanticVersion.Minor,
                          Patch = semanticVersion.Patch,
                          Pre = semanticVersion.Pre,
                          PreReleaseCounter = semanticVersion.PreReleaseCounter
                      };

    if (semanticVersion.Pre == PreReleaseStage.beta && !changeToBeta1)
    {
      nextVersion.PreReleaseCounter++;
      return nextVersion;
    }

    nextVersion.Pre = PreReleaseStage.beta;
    nextVersion.PreReleaseCounter = 1;
    return nextVersion;
  }

  private static SemanticVersion GetNextMajorRelease (SemanticVersion semanticVersion)
  {
    var nextMajorRelease = new SemanticVersion
                           {
                               Major = semanticVersion.Major + 1,
                               Minor = 0,
                               Patch = 0
                           };
    return nextMajorRelease;
  }

  private static SemanticVersion GetNextMajorWithAlpha (SemanticVersion semanticVersion)
  {
    var nextVersion = GetNextMajorRelease(semanticVersion);
    nextVersion.Pre = PreReleaseStage.alpha;
    nextVersion.PreReleaseCounter = 1;
    return nextVersion;
  }

  private static SemanticVersion GetNextMajorWithBeta (SemanticVersion semanticVersion)
  {
    var nextVersion = GetNextMajorRelease(semanticVersion);
    nextVersion.Pre = PreReleaseStage.beta;
    nextVersion.PreReleaseCounter = 1;
    return nextVersion;
  }

  private static SemanticVersion GetNextMinorRelease (SemanticVersion semanticVersion)
  {
    var nextMinorRelease = new SemanticVersion
                           {
                               Major = semanticVersion.Major,
                               Minor = semanticVersion.Minor + 1,
                               Patch = 0
                           };
    return nextMinorRelease;
  }

  private static SemanticVersion GetNextMinorWithAlpha (SemanticVersion semanticVersion)
  {
    var nextMinorRelease = new SemanticVersion
                           {
                               Major = semanticVersion.Major,
                               Minor = semanticVersion.Minor + 1,
                               Patch = 0,
                               Pre = PreReleaseStage.alpha,
                               PreReleaseCounter = 1
                           };
    return nextMinorRelease;
  }

  private static SemanticVersion GetNextMinorWithBeta (SemanticVersion semanticVersion)
  {
    var nextMinorRelease = new SemanticVersion
                           {
                               Major = semanticVersion.Major,
                               Minor = semanticVersion.Minor + 1,
                               Patch = 0,
                               Pre = PreReleaseStage.beta,
                               PreReleaseCounter = 1
                           };
    return nextMinorRelease;
  }

  private static SemanticVersion GetNextPreReleaseVersion (SemanticVersion semanticVersion)
  {
    var nextPreRelease = new SemanticVersion
                         {
                             Major = semanticVersion.Major,
                             Minor = semanticVersion.Minor,
                             Patch = semanticVersion.Patch,
                             Pre = semanticVersion.Pre,
                             PreReleaseCounter = semanticVersion.PreReleaseCounter + 1
                         };
    return nextPreRelease;
  }

  private static SemanticVersion GetNextPreReleaseStage (SemanticVersion semanticVersion)
  {
    var nextPreRelease = new SemanticVersion
                         {
                             Major = semanticVersion.Major,
                             Minor = semanticVersion.Minor,
                             Patch = semanticVersion.Patch,
                             Pre = null != semanticVersion.Pre ? semanticVersion.Pre + 1 : PreReleaseStage.alpha,
                             PreReleaseCounter = 1
                         };
    return nextPreRelease;
  }
}