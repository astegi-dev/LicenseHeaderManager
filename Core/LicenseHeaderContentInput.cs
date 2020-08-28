using System.Collections.Generic;

namespace Core
{
  public class LicenseHeaderContentInput : LicenseHeaderInput
  {
    public LicenseHeaderContentInput (
        string documentContent,
        string documentPath,
        IDictionary<string, string[]> headers,
        IEnumerable<AdditionalProperty> additionalProperties = null)
        : base(headers, additionalProperties, documentPath)
    {
      DocumentContent = documentContent;
    }

    public LicenseHeaderContentInput (
        string documentContent,
        string documentPath,
        IDictionary<string, string[]> headers,
        IEnumerable<AdditionalProperty> additionalProperties,
        bool ignoreNonCommentText)
        : base(headers, additionalProperties, ignoreNonCommentText, documentPath)
    {
      DocumentContent = documentContent;
    }

    public string DocumentContent { get; }

    public override LicenseHeaderInputMode InputMode => LicenseHeaderInputMode.Content;
  }
}
