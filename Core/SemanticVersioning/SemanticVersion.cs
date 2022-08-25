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

namespace ReleaseProcessAutomation.SemanticVersioning;

public class SemanticVersion : IComparable<SemanticVersion>
{
  public int Major { get; set; }
  public int Minor { get; set; }
  public int Patch { get; set; }
  public PreReleaseStage? Pre { get; set; }

  public int? PreReleaseCounter { get; set; }

  public override bool Equals (object? obj)
  {
    var other = obj as SemanticVersion;

    if (other == null) return false;

    return Major == other.Major &&
           Minor == other.Minor &&
           Patch == other.Patch &&
           Pre == other.Pre &&
           PreReleaseCounter == other.PreReleaseCounter;
  }

  [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
  public override int GetHashCode ()
  {
    unchecked
    {
      var hashCode = Major;
      hashCode = (hashCode * 397) ^ Minor;
      hashCode = (hashCode * 397) ^ Patch;
      hashCode = (hashCode * 397) ^ Pre.GetHashCode();
      hashCode = (hashCode * 397) ^ PreReleaseCounter.GetHashCode();
      return hashCode;
    }
  }

  public static explicit operator SemanticVersion (string input)
  {
    return new SemanticVersionParser().ParseVersion(input);
  }

  public override string ToString ()
  {
    if (Pre == null)
      return $"{Major}.{Minor}.{Patch}";

    return $"{Major}.{Minor}.{Patch}-{Pre}.{PreReleaseCounter}";
  }

  public int CompareTo (SemanticVersion? other)
  {
    if (other == null) return 1;

    if (Major > other.Major) return 1;
    if (Major < other.Major) return -1;

    if (Minor > other.Minor) return 1;
    if (Minor < other.Minor) return -1;

    if (Patch > other.Patch) return 1;
    if (Patch < other.Patch) return -1;

    if (Pre != null && other.Pre == null) return -1;
    if (Pre == null && other.Pre != null) return 1;

    if (Pre > other.Pre) return 1;
    if (Pre < other.Pre) return -1;

    if (PreReleaseCounter > other.PreReleaseCounter) return 1;
    if (PreReleaseCounter < other.PreReleaseCounter) return -1;

    return 0;
  }
}