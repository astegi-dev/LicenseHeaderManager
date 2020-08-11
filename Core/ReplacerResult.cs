using System;

namespace Core
{
  public class ReplacerResult<TError>
  {
    public ReplacerResult ()
    {
      IsSuccess = true;
      Error = default;
    }

    public ReplacerResult (TError error)
    {
      Error = error;
      IsSuccess = false;
    }

    public bool IsSuccess { get; }

    public TError Error { get; }
  }
}