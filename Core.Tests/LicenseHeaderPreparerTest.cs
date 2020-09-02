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
using NUnit.Framework;
using Rhino.Mocks;

namespace Core.Tests
{
  [TestFixture]
  public class LicenseHeaderPreparerTest
  {
    [Test]
    public void TestLicenseHeaderWhereCurrentHeaderHasComment ()
    {
      var currentHeaderText = "//HighlyRelevantComment";
      var toBeInsertedHeaderText = "//Testtext" + Environment.NewLine;

      var commentParserMock = MockRepository.GenerateMock<ICommentParser>();
      commentParserMock.Expect (x => x.Parse (currentHeaderText)).Return ("OhOurCurrentHeaderTextHasAComment");

      var preparedHeader = LicenseHeaderPreparer.Prepare (toBeInsertedHeaderText, currentHeaderText, commentParserMock);

      Assert.AreEqual (toBeInsertedHeaderText + Environment.NewLine, preparedHeader);
    }

    [Test]
    public void TestLicenseHeaderWhereCurrentHeaderHasCommentAndFileStartsWithNewLine ()
    {
      var currentHeaderText = Environment.NewLine + "//HighlyRelevantComment";
      var toBeInsertedHeaderText = "//Testtext";

      var commentParserMock = MockRepository.GenerateMock<ICommentParser>();
      commentParserMock.Expect (x => x.Parse (currentHeaderText)).Return ("OhOurCurrentHeaderTextHasAComment");

      var preparedHeader = LicenseHeaderPreparer.Prepare (toBeInsertedHeaderText, currentHeaderText, commentParserMock);

      Assert.AreEqual (toBeInsertedHeaderText, preparedHeader);
    }

    [Test]
    public void TestLicenseHeaderWithEndingNewLine ()
    {
      var currentHeaderText = "nothingRelevant";
      var toBeInsertedHeaderText = "//Testtext" + Environment.NewLine;

      var commentParserMock = MockRepository.GenerateMock<ICommentParser>();
      commentParserMock.Expect (x => x.Parse (currentHeaderText)).Return (string.Empty);

      var preparedHeader = LicenseHeaderPreparer.Prepare (toBeInsertedHeaderText, currentHeaderText, commentParserMock);

      Assert.AreEqual (toBeInsertedHeaderText, preparedHeader);
    }

    [Test]
    public void TestLicenseHeaderWithoutEndingNewLine ()
    {
      var currentHeaderText = "nothingRelevant";
      var toBeInsertedHeaderText = "//Testtext";

      var commentParserMock = MockRepository.GenerateMock<ICommentParser>();
      commentParserMock.Expect (x => x.Parse (currentHeaderText)).Return (string.Empty);

      var preparedHeader = LicenseHeaderPreparer.Prepare (toBeInsertedHeaderText, currentHeaderText, commentParserMock);

      Assert.AreEqual (toBeInsertedHeaderText, preparedHeader);
    }
  }
}