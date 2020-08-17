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
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Core
{
  public class Document
  {
    private readonly IEnumerable<AdditionalProperty> _additionalProperties;
    private readonly CommentParser _commentParser;
    private readonly string _documentFilePath;
    private readonly string[] _headerLines;
    private readonly IEnumerable<string> _keywords;
    private readonly Language _language;
    private string _documentTextCache;
    private DocumentHeader _headerCache;
    private string _lineEndingInDocumentCache;

    public Document (
        string documentFilePath,
        Language language,
        string[] headerLines,
        IEnumerable<AdditionalProperty> additionalProperties = null,
        IEnumerable<string> keywords = null)
    {
      _documentFilePath = documentFilePath;
      _additionalProperties = additionalProperties;
      _keywords = keywords;
      _language = language;
      _headerLines = headerLines;
      _commentParser = new CommentParser (language.LineComment, language.BeginComment, language.EndComment, language.BeginRegion, language.EndRegion);
    }

    private async Task<DocumentHeader> GetHeader ()
    {
      if (_headerCache != default)
        return _headerCache;

      var headerText = await CreateHeaderText (_headerLines);
      _headerCache = new DocumentHeader (_documentFilePath, headerText, new DocumentHeaderProperties (_additionalProperties));
      return _headerCache;
    }

    private async Task<string> GetLineEndingInDocument ()
    {
      _lineEndingInDocumentCache ??= NewLineManager.DetectMostFrequentLineEnd (await GetText());
      return _lineEndingInDocumentCache;
    }

    public async Task<bool> ValidateHeader ()
    {
      return (await GetHeader()).IsEmpty || LicenseHeader.Validate ((await GetHeader()).Text, _commentParser);
    }

    public async Task ReplaceHeaderIfNecessary (CancellationToken cancellationToken)
    {
      var skippedText = await SkipText();
      if (!string.IsNullOrEmpty (skippedText))
      {
        cancellationToken.ThrowIfCancellationRequested();
        await RemoveHeader (skippedText);
      }

      var existingHeader = await GetExistingHeader();

      if (!(await GetHeader()).IsEmpty)
      {
        if (existingHeader != (await GetHeader()).Text)
        {
          cancellationToken.ThrowIfCancellationRequested();
          await ReplaceHeader (existingHeader, (await GetHeader()).Text);
        }
      }
      else
      {
        cancellationToken.ThrowIfCancellationRequested();
        await RemoveHeader (existingHeader);
      }

      if (!string.IsNullOrEmpty (skippedText))
      {
        cancellationToken.ThrowIfCancellationRequested();
        await AddHeader (skippedText);
      }
    }

    private async Task<string> CreateHeaderText (string[] headerLines)
    {
      if (headerLines == null)
        return null;

      var inputText = string.Join (await GetLineEndingInDocument(), headerLines);
      inputText += await GetLineEndingInDocument();

      return inputText;
    }

    private async Task<string> GetText ()
    {
      if (string.IsNullOrEmpty (_documentTextCache))
        await RefreshText();

      return _documentTextCache;
    }

    private async Task RefreshText ()
    {
      using var reader = new StreamReader (_documentFilePath, Encoding.UTF8);
      _documentTextCache = await reader.ReadToEndAsync();
    }

    private async Task<string> GetExistingHeader ()
    {
      var header = _commentParser.Parse (await GetText());

      if (_keywords == null || _keywords.Any (k => header.ToLower().Contains (k.ToLower())))
        return header;
      return string.Empty;
    }

    private async Task<string> SkipText ()
    {
      if (string.IsNullOrEmpty (_language.SkipExpression))
        return null;
      var match = Regex.Match (await GetText(), _language.SkipExpression, RegexOptions.IgnoreCase);
      if (match.Success && match.Index == 0)
        return match.Value;
      return null;
    }

    private async Task ReplaceHeader (string existingHeader, string newHeader)
    {
      await RemoveHeader (existingHeader);
      await AddHeader (LicenseHeaderPreparer.Prepare (newHeader, await GetText(), _commentParser));
    }

    private async Task AddHeader (string header)
    {
      if (!string.IsNullOrEmpty (header))
      {
        var sb = new StringBuilder();
        var newContent = sb.Append (header).Append (await GetText()).ToString();
        using (var writer = new StreamWriter (_documentFilePath, false, Encoding.UTF8))
        {
          await writer.WriteAsync (newContent);
        }

        await RefreshText();
      }
    }

    private async Task RemoveHeader (string header)
    {
      if (!string.IsNullOrEmpty (header))
      {
        var newContent = (await GetText()).Substring (header.Length);
        using (var writer = new StreamWriter (_documentFilePath, false, Encoding.UTF8))
        {
          await writer.WriteAsync (newContent);
        }

        await RefreshText();
      }
    }
  }
}