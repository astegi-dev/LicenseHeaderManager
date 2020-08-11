#region copyright

// Copyright (c) rubicon IT GmbH

// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
// FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE. 

#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Core
{
  public class Document
  {
    internal readonly CommentParser _commentParser;
    internal readonly string _documentFilePath;
    internal readonly DocumentHeader _header;
    internal readonly IEnumerable<string> _keywords;
    internal readonly Language _language;
    internal readonly string _lineEndingInDocument;
    private string _documentTextCache;

    public Document (
        string documentFilePath,
        Language language,
        string[] headerLines,
        IEnumerable<DocumentHeaderProperty> additionalProperties = null,
        IEnumerable<string> keywords = null)
    {
      _documentFilePath = documentFilePath;

      _lineEndingInDocument = NewLineManager.DetectMostFrequentLineEnd (GetText());


      var headerText = CreateHeaderText (headerLines);
      _header = new DocumentHeader (documentFilePath, headerText, new DocumentHeaderProperties (additionalProperties));
      _keywords = keywords;

      _language = language;

      _commentParser = new CommentParser (language.LineComment, language.BeginComment, language.EndComment, language.BeginRegion, language.EndRegion);
    }

    public bool ValidateHeader ()
    {
      if (_header.IsEmpty)
        return true;
      return LicenseHeader.Validate (_header.Text, _commentParser);
    }

    public void ReplaceHeaderIfNecessary ()
    {
      var skippedText = SkipText();
      if (!string.IsNullOrEmpty (skippedText))
        RemoveHeader (skippedText);

      var existingHeader = GetExistingHeader();

      if (!_header.IsEmpty)
      {
        if (existingHeader != _header.Text)
          ReplaceHeader (existingHeader, _header.Text);
      }
      else
      {
        RemoveHeader (existingHeader);
      }

      if (!string.IsNullOrEmpty (skippedText))
        AddHeader (skippedText);
    }

    private string CreateHeaderText (string[] headerLines)
    {
      if (headerLines == null)
        return null;

      var inputText = string.Join (_lineEndingInDocument, headerLines);
      inputText += _lineEndingInDocument;

      return inputText;
    }

    private string GetText ()
    {
      if (string.IsNullOrEmpty (_documentTextCache))
        RefreshText();

      return _documentTextCache;
    }

    private void RefreshText ()
    {
      _documentTextCache = File.ReadAllText (_documentFilePath);
    }

    private string GetExistingHeader ()
    {
      var header = _commentParser.Parse (GetText());

      if (_keywords == null || _keywords.Any (k => header.ToLower().Contains (k.ToLower())))
        return header;
      return string.Empty;
    }

    private string SkipText ()
    {
      if (string.IsNullOrEmpty (_language.SkipExpression))
        return null;
      var match = Regex.Match (GetText(), _language.SkipExpression, RegexOptions.IgnoreCase);
      if (match.Success && match.Index == 0)
        return match.Value;
      return null;
    }

    private void ReplaceHeader (string existingHeader, string newHeader)
    {
      RemoveHeader (existingHeader);
      AddHeader (LicenseHeaderPreparer.Prepare (newHeader, GetText(), _commentParser));
    }

    private void AddHeader (string header)
    {
      if (!string.IsNullOrEmpty (header))
      {
        var sb = new StringBuilder();
        var newContent = sb.Append (header).Append (GetText()).ToString();
        File.WriteAllText (_documentFilePath, newContent);
        RefreshText();
      }
    }

    private void RemoveHeader (string header)
    {
      if (!string.IsNullOrEmpty (header))
      {
        var newContent = GetText().Substring (header.Length);
        File.WriteAllText (_documentFilePath, newContent);
        RefreshText();
      }
    }
  }
}