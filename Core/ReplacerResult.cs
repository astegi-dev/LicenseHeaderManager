namespace Core
{
  public class ReplacerResult<TError>
  {
    public bool IsSuccess { get; }

    public TError Error { get; }

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
  }
}
