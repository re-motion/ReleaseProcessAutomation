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

namespace ReleaseProcessAutomation.ReadInput;

public interface IInputReader
{
  SemanticVersion ReadSemanticVersion (string prompt);

  SemanticVersion ReadVersionChoice (string prompt, IReadOnlyCollection<SemanticVersion> possibleVersions);

  bool ReadConfirmation (bool defaultValue = true);

  string ReadString (string prompt);

  string ReadStringChoice (string prompt, IReadOnlyCollection<string> foundAncestors);

  string ReadHiddenString (string prompt);
}