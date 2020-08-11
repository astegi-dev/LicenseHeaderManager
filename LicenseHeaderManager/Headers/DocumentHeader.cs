// first line LicenseHeaderManager,LicenseHeaderManager.Headers
// second line copyright123

// first line
// second line copyright456

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using EnvDTE;

namespace LicenseHeaderManager.Headers
{
  internal class DocumentHeader
  {
    private readonly TextDocument _document;
    private readonly IEnumerable<DocumentHeaderProperty> _properties;

    public DocumentHeader (TextDocument document, string text, IEnumerable<DocumentHeaderProperty> properties)
    {
      if (document == null) throw new ArgumentNullException ("document");
      if (properties == null) throw new ArgumentNullException ("properties");

      _document = document;
      _properties = properties;

      FileInfo = CreateFileInfo();
      Text = CreateText (text);
    }

    public bool IsEmpty => Text == null;

    public FileInfo FileInfo { get; }

    public string Text { get; }

    private FileInfo CreateFileInfo ()
    {
      var pathToDocument = _document.Parent.FullName;

      if (File.Exists (pathToDocument))
        return new FileInfo (pathToDocument);
      return null;
    }

    private string CreateText (string inputText)
    {
      if (inputText == null)
        return null;

      var finalText = inputText;

      foreach (var property in _properties)
        if (property.CanCreateValue (this))
        {
          var regex = new Regex (property.Token, RegexOptions.IgnoreCase);
          finalText = regex.Replace (finalText, property.CreateValue (this));
        }

      return finalText;
    }
  }
}