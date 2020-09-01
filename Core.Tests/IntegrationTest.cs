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
using System.Threading.Tasks;
using NUnit.Framework;

namespace Core.Tests
{
  [TestFixture]
  internal class IntegrationTest
  {
    [Test]
    public async Task Test ()
    {
      var languages = new List<Language>
                      {
                          new Language
                          {
                              Extensions = new[] { ".cs" }, LineComment = "//", BeginComment = "/*", EndComment = "*/", BeginRegion = "#region",
                              EndRegion = "#endregion"
                          },
                          new Language
                          {
                              Extensions = new[] { ".js", ".ts" }, LineComment = "//", BeginComment = "/*", EndComment = "*/",
                              SkipExpression = @"(/// *<reference.*/>( |\t)*(\n|\r\n|\r)?)*"
                          }
                      };

      var replacer = new LicenseHeaderReplacer (languages, new[] { "1" });

      var headers = new Dictionary<string, string[]> { { ".cs", new[] { "// first line 1", "// second line", "// copyright" } } };
      await replacer.RemoveOrReplaceHeader (new LicenseHeaderPathInput (@"C:\Users\gabriel.ratschiller\Desktop\TestFile.cs", headers));
    }
  }
}