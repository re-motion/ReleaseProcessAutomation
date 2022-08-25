using System;

namespace ReleaseProcessAutomation.ReadInput;

public class UserDoesNotWantToContinueException : Exception
{
  public UserDoesNotWantToContinueException (string message)
      : base(message)
  {
  }
}