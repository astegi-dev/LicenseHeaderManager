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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Rhino.Mocks;
using Rhino.Mocks.Interfaces;

namespace Core.Tests
{
  [TestFixture]
  public class LicenseHeaderReplacerTest
  {
    [Test]
    public void GetLanguageFromExtension_LanguagesAreEmpty_ReturnsNull()
    {
      var replacer = new LicenseHeaderReplacer(Enumerable.Empty<Language>(), Enumerable.Empty<string>());

      var language = replacer.GetLanguageFromExtension(".cs");

      Assert.That (language, Is.Null);
    }

    [Test]
    public void GetLanguageFromExtension_LanguagesAreNull_DoesNotThrowException()
    {
      var replacer = new LicenseHeaderReplacer(null, Enumerable.Empty<string>());

      Assert.That (() => replacer.GetLanguageFromExtension(".cs"), Throws.Nothing);
    }

    [Test]
    public void IsValidPathInput_FileExistsValidDocument_ReturnsTrue()
    {
      var filePath = @"C:\Windows\explorer.exe";
      var replacer = new ReplacerStub();
      replacer.InitializeStub (filePath, true);

      var isValid = replacer.IsValidPathInput(filePath);
      Assert.That(isValid, Is.True);
    }

    [Test]
    public void IsValidPathInput_FileExistsInvalidDocument_ReturnsTrue()
    {
      var filePath = @"C:\Windows\explorer.exe";
      var replacer = new ReplacerStub();
      replacer.InitializeStub(filePath, false);

      var isValid = replacer.IsValidPathInput(filePath);
      Assert.That(isValid, Is.False);
    }

    [Test]
    public async Task RemoveOrReplaceHeader_PathIsNull_ReturnsReplacerResultWithFileNotFound()
    {
      var replacer = new LicenseHeaderReplacer(Enumerable.Empty<Language>(), Enumerable.Empty<string>());
      var actual = await replacer.RemoveOrReplaceHeader(new LicenseHeaderPathInput(null, null));
      Assert.That (actual.Error.Type, Is.EqualTo (ReplacerErrorType.FileNotFound));
    }

    [Test]
    public async Task RemoveOrReplaceHeader_DocumentIsLicenseHeaderFile_ReturnsReplacerResultWithLicenseHeaderDocument()
    {
      var replacer = new LicenseHeaderReplacer(Enumerable.Empty<Language>(), Enumerable.Empty<string>());
      var path = "";
      var actual = await replacer.RemoveOrReplaceHeader(new LicenseHeaderPathInput(path, null));
      Assert.That(actual.Error.Type, Is.EqualTo(ReplacerErrorType.FileNotFound));
    }
  }

  internal class ReplacerStub : LicenseHeaderReplacer
  {
    private string _path;
    private bool _validDocument;

    public void InitializeStub(string path, bool validDocument)
    {
      _path = path;
      _validDocument = validDocument;
    }

    internal override CreateDocumentResult TryCreateDocument(LicenseHeaderInput licenseHeaderInput, out Document document)
    {
      document = null;
      if (licenseHeaderInput.DocumentPath == _path && _validDocument)
        return CreateDocumentResult.DocumentCreated;
      return CreateDocumentResult.NoHeaderFound;
    }
  }
}
