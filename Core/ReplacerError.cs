using System;

namespace Core
{
  public class ReplacerError
  {
    public ReplacerError (string documentPath, ErrorType type, string description)
    {
      DocumentPath = documentPath;
      Type = type;
      Description = description;
    }

    public string DocumentPath { get; }

    public ErrorType Type { get; }

    public string Description { get; }
  }
}