using System;
using System.Collections.Generic;

namespace Core
{
  public class LicenseHeaderInput
  {
    public LicenseHeaderInput (
        string documentPath,
        IDictionary<string, string[]> headers,
        IEnumerable<DocumentHeaderProperty> additionalProperties = null)
    {
      DocumentPath = documentPath;
      Headers = headers;
      AdditionalProperties = additionalProperties;
    }

    public string DocumentPath { get; }

    public IDictionary<string, string[]> Headers { get; }

    public IEnumerable<DocumentHeaderProperty> AdditionalProperties { get; }
  }
}