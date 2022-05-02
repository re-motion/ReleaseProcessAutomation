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
using System.ComponentModel;
using JetBrains.Annotations;
using Spectre.Console.Cli;

namespace ReleaseProcessAutomation.Commands;

[UsedImplicitly]
internal class CloseVersionSettings : CommandSettings
{
  [UsedImplicitly]
  [CommandOption("-a|--ancestor")]
  [Description("Optional Parameter that should contain the branch from which the release branch emerged from")]
  public string? Ancestor { get; set; }

  [UsedImplicitly]
  [CommandOption("-n|--noPush")]
  [Description(
      "Optional <switch> Parameter. If given, the scripts stops before pushing the changes to the remote repositories")]
  public bool DoNotPush { get; set; }
}
