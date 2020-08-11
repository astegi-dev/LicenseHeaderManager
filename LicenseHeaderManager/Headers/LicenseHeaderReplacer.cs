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
using System.Runtime.InteropServices;
using EnvDTE;
using LicenseHeaderManager.Interfaces;
using LicenseHeaderManager.Utils;
using Window = EnvDTE.Window;

namespace LicenseHeaderManager.Headers
{
  public class LicenseHeaderReplacer
  {
    /// <summary>
    /// Used to keep track of the user selection when he is trying to insert invalid headers into all files,
    /// so that the warning is only displayed once per file extension.
    /// </summary>
    private readonly IDictionary<string, bool> _extensionsWithInvalidHeaders = new Dictionary<string, bool>();

    private readonly ILicenseHeaderExtension _licenseHeaderExtension;

    public LicenseHeaderReplacer (ILicenseHeaderExtension licenseHeaderExtension)
    {
      _licenseHeaderExtension = licenseHeaderExtension;
    }

    public void ResetExtensionsWithInvalidHeaders ()
    {
      _extensionsWithInvalidHeaders.Clear();
    }

    /// <summary>
    /// Tries to open a given project item as a Document which can be used to add or remove headers.
    /// </summary>
    /// <param name="item">The project item.</param>
    /// <param name="document">The document which was created or null if an error occured (see return value).</param>
    /// <param name="headers">A dictionary of headers using the file extension as key and the header as value or null if headers should only be removed.</param>
    /// <returns>A value indicating the result of the operation. Document will be null unless DocumentCreated is returned.</returns>
    public CreateDocumentResult TryCreateDocument (
        ProjectItem item,
        out Document document,
        out bool wasOpen,
        IDictionary<string, string[]> headers = null)
    {
      document = null;
      wasOpen = true;

      if (!ProjectItemInspection.IsPhysicalFile (item))
        return CreateDocumentResult.NoPhysicalFile;

      if (ProjectItemInspection.IsLicenseHeader (item))
        return CreateDocumentResult.LicenseHeaderDocument;

      if (ProjectItemInspection.IsLink (item))
        return CreateDocumentResult.LinkedFile;

      var language = _licenseHeaderExtension.LanguagesPage.Languages
          .Where (x => x.Extensions.Any (y => item.Name.EndsWith (y, StringComparison.OrdinalIgnoreCase)))
          .FirstOrDefault();

      if (language == null)
        return CreateDocumentResult.LanguageNotFound;

      Window window = null;

      //try to open the document as a text document
      try
      {
        if (!item.IsOpen[Constants.vsViewKindTextView])
        {
          window = item.Open (Constants.vsViewKindTextView);
          wasOpen = false;
        }
      }
      catch (COMException)
      {
        return CreateDocumentResult.NoTextDocument;
      }
      catch (IOException)
      {
        return CreateDocumentResult.NoPhysicalFile;
      }

      var itemDocument = item.Document;
      if (item.Document == null)
      {
        return CreateDocumentResult.NoPhysicalFile;
      }


      var textDocument = itemDocument.Object ("TextDocument") as TextDocument;
      if (textDocument == null)
      {
        return CreateDocumentResult.NoTextDocument;
      }

      string[] header = null;
      if (headers != null)
      {
        var extension = headers.Keys
            .OrderByDescending (x => x.Length)
            .Where (x => item.Name.EndsWith (x, StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault();

        if (extension == null)
        {
          return CreateDocumentResult.NoHeaderFound;
        }

        header = headers[extension];

        if (header.All (string.IsNullOrEmpty))
        {
          return CreateDocumentResult.EmptyHeader;
        }
      }

      var optionsPage = _licenseHeaderExtension.OptionsPage;

      document = new Document (
          textDocument,
          language,
          header,
          item,
          optionsPage.UseRequiredKeywords
              ? optionsPage.RequiredKeywords.Split (new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select (k => k.Trim())
              : null);

      return CreateDocumentResult.DocumentCreated;
    }
  }
}