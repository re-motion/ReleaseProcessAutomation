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
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace ReleaseProcessScript.New;

[ExcludeFromCodeCoverage]
public class TypeRegistrar : ITypeRegistrar
{
  private readonly IServiceCollection _serviceCollection;

  public TypeRegistrar (IServiceCollection serviceCollection)
  {
    _serviceCollection = serviceCollection;
  }

  public void Register (Type service, Type implementation)
  {
    _serviceCollection.AddSingleton(service, implementation);
  }

  public void RegisterInstance (Type service, object implementation)
  {
    _serviceCollection.AddSingleton(service, implementation);
  }

  public void RegisterLazy (Type service, Func<object> factory)
  {
    _serviceCollection.AddSingleton(service, _ => factory());
  }

  public ITypeResolver Build ()
  {
    return new TypeResolver(_serviceCollection.BuildServiceProvider());
  }
}
