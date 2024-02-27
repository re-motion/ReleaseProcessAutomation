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

using Moq;
using NUnit.Framework;
using Remotion.ReleaseProcessAutomation.Extensions;
using Remotion.ReleaseProcessAutomation.ReadInput;
using Remotion.ReleaseProcessAutomation.SemanticVersioning;

namespace Remotion.ReleaseProcessAutomation.UnitTests.ReadInput
{
  [TestFixture]
  public class InputReaderExtensionsTest
  {
    [Test]
    public void ReadVersionChoiceForFollowingRelease()
    {
      var nextVersions = new SemanticVersion().GetNextPossibleVersionsDevelop(true);
      var inputReaderMock = new Mock<IInputReader>();

      inputReaderMock.Object.ReadVersionChoiceForFollowingRelease(nextVersions);

      inputReaderMock.Verify(_ => _.ReadVersionChoice("Please choose the version for the following release (open JIRA issues get moved there):", nextVersions));
    }

    [Test]
    public void ReadVersionChoiceForCurrentRelease()
    {
      var nextVersions = new SemanticVersion().GetNextPossibleVersionsDevelop(true);
      var inputReaderMock = new Mock<IInputReader>();

      inputReaderMock.Object.ReadVersionChoiceForCurrentRelease(nextVersions);

      inputReaderMock.Verify(_ => _.ReadVersionChoice("Please choose the version of the current release:", nextVersions));
    }
  }
}