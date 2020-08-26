/* Copyright (c) rubicon IT GmbH
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"),
 * to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
 * and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
 * FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE. 
 */

using System;
using System.Collections.Generic;

namespace Core
{
  public class LicenseHeaderInput
  {
    public LicenseHeaderInput (
        string documentPath,
        IDictionary<string, string[]> headers,
        IEnumerable<AdditionalProperty> additionalProperties = null)
    {
      DocumentPath = documentPath;
      Headers = headers;
      AdditionalProperties = additionalProperties;
      IgnoreNonCommentText = false;
    }

    public LicenseHeaderInput (
        string documentPath,
        IDictionary<string, string[]> headers,
        IEnumerable<AdditionalProperty> additionalProperties,
        bool ignoreNonCommentText)
    {
      DocumentPath = documentPath;
      Headers = headers;
      AdditionalProperties = additionalProperties;
      IgnoreNonCommentText = ignoreNonCommentText;
    }

    public string DocumentPath { get; }

    public IDictionary<string, string[]> Headers { get; }

    public IEnumerable<AdditionalProperty> AdditionalProperties { get; }

    /// <summary>
    /// Specifies whether license headers should be inserted into a file even if the license header definition contains non-comment text.
    /// </summary>
    public bool IgnoreNonCommentText { get; set; }
  }
}