namespace Core
{
  public class ReplacerError
  {
    public string DocumentPath { get; }

    public ErrorType Type { get; }

    public string Description { get; }

    public ReplacerError(string documentPath, ErrorType type, string description)
    {
      DocumentPath = documentPath;
      Type = type;
      Description = description;
    }
  }
}
