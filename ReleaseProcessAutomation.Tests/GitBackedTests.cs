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
using System.Diagnostics;
using System.IO;
using NUnit.Framework;

namespace ReleaseProcessAutomation.Tests;

public class GitBackedTests
{
  protected string PreviousWorkingDirectory;
  protected string RepositoryPath;
  protected const string RemoteName = "origin";

  private string _remotePath;

  [SetUp]
  public void GitTestSetup()
  {
    PreviousWorkingDirectory = Environment.CurrentDirectory;
    var temp = Path.GetTempPath();

    var guid = Guid.NewGuid();
    var path = Path.Combine(temp, guid.ToString());
    _remotePath = Directory.CreateDirectory(path).FullName;

    Environment.CurrentDirectory = _remotePath;

    ExecuteGitCommand("--bare init");

    guid = Guid.NewGuid();

    path = Path.Combine(temp, guid.ToString());
    RepositoryPath = Directory.CreateDirectory(path).FullName;

    Environment.CurrentDirectory = RepositoryPath;

    ExecuteGitCommand("init");
    ExecuteGitCommand($"remote add {RemoteName} {_remotePath}");
    ExecuteGitCommand("commit -m \"Initial CommitAll\" --allow-empty");
    ExecuteGitCommand($"push {RemoteName} --all");
  }

  [TearDown]
  public void TearDown()
  {
    Environment.CurrentDirectory = PreviousWorkingDirectory;
    DeleteDirectory(RepositoryPath);
    DeleteDirectory(_remotePath);
  }

  protected static void ExecuteGitCommand(string argument)
  {
    using var command = Process.Start("git", argument);
    command.WaitForExit();
  }

  protected static string ExecuteGitCommandWithOutput(string argument)
  {
    var psi = new ProcessStartInfo("git", argument);
    psi.UseShellExecute = false;
    psi.RedirectStandardOutput = true;
    psi.RedirectStandardError = true;

    using var command = Process.Start(psi);
    command.WaitForExit();
    if (command.ExitCode != 0)
    {
      var error = command.StandardError.ReadToEnd();
    }
    var output = command.StandardOutput.ReadToEnd();

    return output;
  }

  private static void DeleteDirectory(string target_dir)
  {
    var files = Directory.GetFiles(target_dir);
    var dirs = Directory.GetDirectories(target_dir);

    foreach (var file in files)
    {
      File.SetAttributes(file, FileAttributes.Normal);
      File.Delete(file);
    }

    foreach (var dir in dirs)
      DeleteDirectory(dir);

    Directory.Delete(target_dir, false);
  }


  protected void AddCommit(string s = "")
  {
    var random = new Random().Next();
    ExecuteGitCommand($"commit --allow-empty -m {random}");
  }

  protected void AddRandomFile(string directoryPath)
  {
    var random = new Random().Next();

    var path = Path.Join(directoryPath, random.ToString());

    using var fs = File.Create(path);
    fs.Dispose();
  }

  protected void ReleaseVersion(string version)
  {
    var releaseBranchName = $"release/{version}";
    ExecuteGitCommand($"checkout -b {releaseBranchName}");
    AddCommit();
    ExecuteGitCommand($"commit --amend -m \"Release Version {version}\"");
    ExecuteGitCommand($"tag -a {version} -m {version}");
  }

  protected bool IsEqualRepo (string otherLogs)
  {
    var currentLogs = ExecuteGitCommandWithOutput("log--all--graph--oneline--decorate--pretty = '%d %s'");

    return currentLogs.Equals(otherLogs);
  }
}