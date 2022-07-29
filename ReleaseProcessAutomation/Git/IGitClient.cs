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

namespace ReleaseProcessAutomation.Git;

public interface IGitClient
{
  string? GetCurrentBranchName ();

  IReadOnlyCollection<string> GetAncestors (params string[] expectedAncestor);

  bool DoesBranchExist (string branchName);

  bool DoesTagExist (string tagName);

  string? GetRemoteOfBranch (string branchName);

  bool IsCommitHash (string? commitHash);

  bool IsOnBranch (string branchName);

  IReadOnlyCollection<string> GetTags (string from = "HEAD", string to = "");

  bool IsWorkingDirectoryClean ();

  string CheckoutCommitWithNewBranch (string? commitHash, string branchName);

  string Checkout (string toCheckout);

  string CheckoutNewBranch (string branchName);

  void MergeBranchWithoutCommit (string branchName);

  void MergeBranchToOnlyContainChangesFromMergedBranch (string baseBranchName);

  void CommitAll (string message);

  void AddAll ();

  void ResolveMergeConflicts ();

  void Reset (string fileName);

  void CheckoutDiscard (string fileName);

  void Tag (string tagName, string message);

  void PushToRepos (IReadOnlyCollection<string> remoteNames, string branchName, string? tagName = null);

  string? Fetch (string arguments);

  string? GetHash (string branch, string? remoteName = "");

  string? GetMostRecentCommonAncestorWithRemote (string branch, string branchOnRemote, string remote);
}