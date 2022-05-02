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
using NUnit.Framework;
using ReleaseProcessAutomation.Extensions;
using ReleaseProcessAutomation.SemanticVersioning;

namespace ReleaseProcessAutomation.Tests.SemanticVersioning;

[TestFixture]
public class SemanticVersionTests
{
  [SetUp]
  public void Setup ()
  {
  }

  [Test]
  public void GetNextPossibleVersionsDevelop_WithPreReleaseAlpha_ReturnsSixVersions ()
  {
    var parser = new SemanticVersionParser();
    var baseVersion = parser.ParseVersion("1.3.5-alpha.1");

    var possibleVersions = baseVersion.GetNextPossibleVersionsDevelop();

    Assert.That(possibleVersions, Does.Contain(parser.ParseVersion("1.3.5")));
    Assert.That(possibleVersions, Does.Contain(parser.ParseVersion("2.0.0")));
    Assert.That(possibleVersions, Does.Contain(parser.ParseVersion("2.0.0-alpha.1")));
    Assert.That(possibleVersions, Does.Contain(parser.ParseVersion("2.0.0-beta.1")));
    Assert.That(possibleVersions, Does.Contain(parser.ParseVersion("1.3.5-alpha.2")));
    Assert.That(possibleVersions, Does.Contain(parser.ParseVersion("1.3.5-beta.1")));
  }

  [Test]
  public void GetNextPossibleVersionsHotfix_WithNonPreRelease_ReturnsVersions()
  {
    var parser = new SemanticVersionParser();
    var expectedVersions = new SemanticVersion[]
                           {
                               parser.ParseVersion("1.3.6-alpha.1"),
                               parser.ParseVersion("1.3.6-beta.1"),
                               parser.ParseVersion("1.3.6")
                           };
    var baseVersion = parser.ParseVersion("1.3.5");

    var possibleVersions = baseVersion.GetNextPossibleVersionsHotfix();

    Assert.That(possibleVersions, Is.EqualTo(expectedVersions));
  }

  [Test]
  public void GetNextPossibleVersionsHotfix_WithPreReleaseAlpha_ReturnsThreeVersions()
  {
    var parser = new SemanticVersionParser();
    var expectedVersions = new SemanticVersion[]
                           {
                               parser.ParseVersion("1.3.5-alpha.2"),
                               parser.ParseVersion("1.3.5-beta.1"),
                               parser.ParseVersion("1.3.6")
                           };
    var baseVersion = parser.ParseVersion("1.3.5-alpha.1");

    var possibleVersions = baseVersion.GetNextPossibleVersionsHotfix();

    Assert.That(possibleVersions, Is.EqualTo(expectedVersions));
  }

  [Test]
  public void GetNextPossibleVersionsHotfix_WithPreReleaseBeta_ReturnsTwoVersions()
  {
    var parser = new SemanticVersionParser();
    var expectedVersions = new SemanticVersion[]
                           {
                               parser.ParseVersion("1.3.5-beta.2"),
                               parser.ParseVersion("1.3.6")
                           };
    var baseVersion = parser.ParseVersion("1.3.5-beta.1");

    var possibleVersions = baseVersion.GetNextPossibleVersionsHotfix();

    Assert.That(possibleVersions, Is.EqualTo(expectedVersions));
  }

  [Test]
  public void GetNextPossibleVersionsHotfix_WithPreReleaseRC_ReturnsTwoVersions()
  {
    var parser = new SemanticVersionParser();
    var expectedVersions = new SemanticVersion[]
                           {
                               parser.ParseVersion("1.3.5-rc.2"),
                               parser.ParseVersion("1.3.6")
                           };
    var baseVersion = parser.ParseVersion("1.3.5-rc.1");

    var possibleVersions = baseVersion.GetNextPossibleVersionsHotfix();

    Assert.That(possibleVersions, Is.EqualTo(expectedVersions));
  }

  [Test]
  public void GetCurrentPossibleVersionsHotfix_FullRelease_ReturnsVersions()
  {
    var parser = new SemanticVersionParser();
    var expectedVersions = new SemanticVersion[]
                           {
                               parser.ParseVersion("1.3.5-alpha.1"),
                               parser.ParseVersion("1.3.5-beta.1"),
                               parser.ParseVersion("1.3.5")
                           };
    var baseVersion = parser.ParseVersion("1.3.5");

    var possibleVersions = baseVersion.GetCurrentPossibleVersionsHotfix();

    Assert.That(possibleVersions, Is.EqualTo(expectedVersions));
  }

  [Test]
  public void GetCurrentPossibleVersionsHotfix_WithPreReleaseAlpha_ReturnsVersions()
  {
    var parser = new SemanticVersionParser();
    var expectedVersions = new SemanticVersion[]
                           {
                               parser.ParseVersion("1.3.5-alpha.3"),
                               parser.ParseVersion("1.3.5-beta.1"),
                               parser.ParseVersion("1.3.5")
                           };
    var baseVersion = parser.ParseVersion("1.3.5-alpha.2");

    var possibleVersions = baseVersion.GetCurrentPossibleVersionsHotfix();

    Assert.That(possibleVersions, Is.EqualTo(expectedVersions));
  }

  [Test]
  public void GetCurrentPossibleVersionsHotfix_WithPreReleaseBeta_ReturnsVersions()

  {
    var parser = new SemanticVersionParser();
    var expectedVersions = new SemanticVersion[]
                           {
                               parser.ParseVersion("1.3.5-beta.3"),
                               parser.ParseVersion("1.3.5")
                           };
    var baseVersion = parser.ParseVersion("1.3.5-beta.2");

    var possibleVersions = baseVersion.GetCurrentPossibleVersionsHotfix();

    Assert.That(possibleVersions, Is.EqualTo(expectedVersions));
  }

  [Test]
  public void GetCurrentPossibleVersionsHotfix_WithPreReleaseRC_ReturnsVersions()
  {
    var parser = new SemanticVersionParser();
    var expectedVersions = new SemanticVersion[]
                           {
                               parser.ParseVersion("1.3.5-rc.2"),
                               parser.ParseVersion("1.3.5")
                           };
    var baseVersion = parser.ParseVersion("1.3.5-rc.1");

    var possibleVersions = baseVersion.GetCurrentPossibleVersionsHotfix();

    Assert.That(possibleVersions, Is.EqualTo(expectedVersions));
  }

  [Test]
  public void Equals_WithSame_ReturnsTrue ()
  {
    var sem1 = new SemanticVersion();
    sem1.Major = 1;
    var sem2 = new SemanticVersion();
    sem2.Major = 1;

    var equals = sem1.Equals(sem2);

    Assert.That(equals, Is.True);
  }

  [Test]
  public void Equals_WithDiff_ReturnsFalse ()
  {
    var sem1 = new SemanticVersion();
    sem1.Major = 1;
    var sem2 = new SemanticVersion();
    sem2.Major = 2;

    var equals = sem1.Equals(sem2);

    Assert.That(equals, Is.False);
  }

  [Test]
  public void CompareTo_WithSame_ReturnsZero ()
  {
    var sem1 = new SemanticVersion();
    sem1.Major = 1;
    var sem2 = new SemanticVersion();
    sem2.Major = 1;

    var compare = sem1.CompareTo(sem2);

    Assert.That(compare, Is.EqualTo(0));
  }

  [Test]
  public void CompareTo_WithPreReleaseDiff_ReturnsOne ()
  {
    var sem1 = new SemanticVersion();
    sem1.Major = 1;
    sem1.Pre = PreReleaseStage.rc;
    sem1.PreReleaseCounter = 3;
    var sem2 = new SemanticVersion();
    sem2.Major = 1;
    sem2.Pre = PreReleaseStage.beta;
    sem2.PreReleaseCounter = 1;

    var compare = sem1.CompareTo(sem2);

    Assert.That(compare, Is.EqualTo(1));
  }

  [Test]
  public void CompareTo_WithOnePreRelease_ReturnsOne ()
  {
    var sem1 = new SemanticVersion();
    sem1.Major = 1;
    var sem2 = new SemanticVersion();
    sem2.Major = 1;
    sem2.Pre = PreReleaseStage.beta;
    sem2.PreReleaseCounter = 1;

    var compare = sem1.CompareTo(sem2);

    Assert.That(compare, Is.EqualTo(1));
  }
}