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
using ReleaseProcessAutomation.SemanticVersioning;

namespace ReleaseProcessAutomation.UnitTests.SemanticVersioning;

[TestFixture]
internal class SemanticVersionParserTests
{
  [Test]
  public void ParseVersion_WithWrongFormat_ThrowsException ()
  {
    var parser = new SemanticVersionParser();

    var invalidFormat = "help";

    Assert.That(
        () => parser.ParseVersion(invalidFormat),
        Throws.InstanceOf<ArgumentException>()
            .With.Message.EqualTo("Version has an invalid format. Expected equivalent to '1.2.3' or '1.2.3-alpha.4'"));
  }

  [Test]
  public void ParseVersion_WithCorrectFormat_ReturnsProperSemanticVersion ()
  {
    var parser = new SemanticVersionParser();

    var validFormat = "1.0.0";
    var correctSemVer = new SemanticVersion
                        {
                            Major = 1
                        };

    var parsedVersion = parser.ParseVersion(validFormat);

    Assert.That(parsedVersion, Is.EqualTo(correctSemVer));
  }

  [Test]
  public void ParseVersionFromBranchName_WithCorrectBranchName_ReturnsProperVersion ()
  {
    var parser = new SemanticVersionParser();

    var validFormat = "help/v1.0.0";
    var correctSemVer = new SemanticVersion();
    correctSemVer.Major = 1;

    var parsedVersion = parser.ParseVersionFromBranchName(validFormat);

    Assert.That(parsedVersion, Is.EqualTo(correctSemVer));
  }

  [Test]
  public void ParseVersionFromBranchName_WithWrongFormat_ThrowsException ()
  {
    var parser = new SemanticVersionParser();

    var invalidFormat = "help";

    Assert.That(
        () => parser.ParseVersionFromBranchName(invalidFormat),
        Throws.InstanceOf<InvalidOperationException>()
            .With.Message.EqualTo(
                "Could not parse version from branch name 'help' because it is not in a valid format. Expected equivalent to 'release/v1.2.3'"));
  }

  [Test]
  public void TryParseVersion_WithWrongFormat_ReturnsUnchangedVersion ()
  {
    var parser = new SemanticVersionParser();
    var invalidFormat = "help";

    var success = parser.TryParseVersion(invalidFormat, out var nullVersion);

    Assert.That(nullVersion, Is.Null);
  }

  [Test]
  public void ParseVersionOrNull_WithCorrectFormat_ReturnsProperSemanticVersion ()
  {
    var parser = new SemanticVersionParser();

    var validFormat = "1.0.0";
    var correctSemVer = new SemanticVersion
                        {
                            Major = 1
                        };

    var success = parser.TryParseVersion(validFormat, out var semanticVersion);

    Assert.That(semanticVersion, Is.EqualTo(correctSemVer));
  }

  [Test]
  public void IsSemver_WithWrongFormat_ReturnsNull ()
  {
    var parser = new SemanticVersionParser();
    var invalidFormat = "help";

    var isSemver = parser.IsSemver(invalidFormat);

    Assert.That(isSemver, Is.False);
  }

  [Test]
  public void IsSemver_WithCorrectFormat_ReturnsTrue ()
  {
    var parser = new SemanticVersionParser();

    var validFormat = "1.0.0";

    var isSemver = parser.IsSemver(validFormat);

    Assert.That(isSemver, Is.True);
  }
}