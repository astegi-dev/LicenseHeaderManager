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

using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Core.Tests
{
  [TestFixture]
  public class LicenseHeaderInputTest
  {
    private List<string> _paths;

    [SetUp]
    public void Setup()
    {
      _paths = new List<string>();
    }

    [Test]
    public void LicenseHeaderInput_ValidInput_ReturnsValidProperties()
    {
      var headers = new Dictionary<string, string[]>();
      var additionalProperties = new List<AdditionalProperty>();
      const string extension = ".cs";
      var documentPath = CreateTestFile();

      LicenseHeaderInput licenseHeaderContentInput = new LicenseHeaderContentInput("", documentPath, headers, additionalProperties);

      var actualHeaders = licenseHeaderContentInput.Headers;
      var actualAdditionalProperties = licenseHeaderContentInput.AdditionalProperties;
      var actualIgnoreNonCommentText = licenseHeaderContentInput.IgnoreNonCommentText;
      var actualDocumentPath = licenseHeaderContentInput.DocumentPath;
      var actualExtension = licenseHeaderContentInput.Extension;
      var actualInputMode = licenseHeaderContentInput.InputMode;

      Assert.That(actualHeaders, Is.EqualTo(headers));
      Assert.That(actualAdditionalProperties, Is.EqualTo(additionalProperties));
      Assert.That(actualIgnoreNonCommentText, Is.False);
      Assert.That(actualDocumentPath, Is.EqualTo(documentPath));
      Assert.That(actualExtension, Is.EqualTo(extension));
      Assert.That(actualInputMode, Is.EqualTo(LicenseHeaderInputMode.Content));
    }

    [TearDown]
    public void TearDown()
    {
      foreach (var path in _paths)
        File.Delete(path);
    }

    private string CreateTestFile(string extension = null)
    {
      if (extension == null)
        extension = ".cs";

      var testFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + extension);
      _paths.Add(testFile);

      using (var fs = File.Create(testFile))
      {
        var content = Encoding.UTF8.GetBytes("");
        fs.Write(content, 0, content.Length);
      }
      return testFile;
    }
  }
}
