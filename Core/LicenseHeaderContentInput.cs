using System.Collections.Generic;

namespace Core
{
  public class LicenseHeaderContentInput : LicenseHeaderInput
  {
    public LicenseHeaderContentInput (
        string documentContent,
        string extension,
        string documentPath,
        IDictionary<string, string[]> headers,
        IEnumerable<AdditionalProperty> additionalProperties = null)
        : base(headers, additionalProperties, documentPath)
    {
      DocumentContent = documentContent;
      Extension = extension;
    }

    public LicenseHeaderContentInput (
        string documentContent,
        string extension,
        string documentPath,
        IDictionary<string, string[]> headers,
        IEnumerable<AdditionalProperty> additionalProperties,
        bool ignoreNonCommentText)
        : base(headers, additionalProperties, ignoreNonCommentText, documentPath)
    {
      DocumentContent = documentContent;
      Extension = extension;
    }

    public string DocumentContent { get; }

    public override string Extension { get; }

    public override LicenseHeaderInputMode InputMode => LicenseHeaderInputMode.Content;
  }
}
