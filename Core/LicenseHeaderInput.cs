using System.Collections.Generic;
using System.IO;

namespace Core
{
  public abstract class LicenseHeaderInput
  {
    protected LicenseHeaderInput (IDictionary<string, string[]> headers, IEnumerable<AdditionalProperty> additionalProperties, bool ignoreNonCommentText, string documentPath)
    {
      Headers = headers;
      AdditionalProperties = additionalProperties;
      IgnoreNonCommentText = ignoreNonCommentText;
      DocumentPath = documentPath;
      Extension = Path.GetExtension (DocumentPath);
    }

    protected LicenseHeaderInput (IDictionary<string, string[]> headers, IEnumerable<AdditionalProperty> additionalProperties, string documentPath)
    {
      Headers = headers;
      AdditionalProperties = additionalProperties;
      IgnoreNonCommentText = false;
      DocumentPath = documentPath;
      Extension = Path.GetExtension (DocumentPath);
    }

    public IDictionary<string, string[]> Headers { get; }

    public IEnumerable<AdditionalProperty> AdditionalProperties { get; }

    /// <summary>
    /// Specifies whether license headers should be inserted into a file even if the license header definition contains non-comment text.
    /// </summary>
    public bool IgnoreNonCommentText { get; set; }

    public string DocumentPath { get; }

    public string Extension { get; }

    public abstract LicenseHeaderInputMode InputMode { get; }
  }
}
