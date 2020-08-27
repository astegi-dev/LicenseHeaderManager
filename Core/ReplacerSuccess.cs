using System;

namespace Core
{
  public class ReplacerSuccess
  {
    public string FilePath { get; }
    public string NewContent { get; }

    public ReplacerSuccess (string filePath, string newContent)
    {
      FilePath = filePath;
      NewContent = newContent;
    }
  }
}
