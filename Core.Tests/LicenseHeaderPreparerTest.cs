using System;
using Core;
using NUnit.Framework;
using Rhino.Mocks;

namespace LicenseHeaderManager.Test
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