using System;

namespace Remotion.ReleaseProcessAutomation.Git;

public class GitException: Exception
{
  public GitException (string message)
      : base(message)
  {
  }
}