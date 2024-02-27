using System;

namespace Remotion.ReleaseProcessAutomation;

public class UserInteractionException: Exception
{
  public UserInteractionException (string message)
      : base(message)
  {
  }
}