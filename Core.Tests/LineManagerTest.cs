using System;
using Core;
using NUnit.Framework;

namespace LicenseHeaderManager.Test
{
  [TestFixture]
  public class LineManagerTest
  {
    [Test]
    public void DetectLineEnd_CRLF ()
    {
      var text = "test\r\ntest";
      Assert.AreEqual ("\r\n", NewLineManager.DetectMostFrequentLineEnd (text));
    }

    [Test]
    public void DetectLineEnd_OnlyCR ()
    {
      var text = "test\rtest";
      Assert.AreEqual ("\r", NewLineManager.DetectMostFrequentLineEnd (text));
    }

    [Test]
    public void DetectLineEnd_OnlyLF ()
    {
      var text = "test\ntest";
      Assert.AreEqual ("\n", NewLineManager.DetectMostFrequentLineEnd (text));
    }

    [Test]
    public void DetectMostFrequentLineEnd_DetectFullLineEnding ()
    {
      var text = "test\n\r\n\r\n test\r\n te\r restasd";
      var result = NewLineManager.DetectMostFrequentLineEnd (text);
      Assert.AreEqual ("\r\n", result);
    }

    [Test]
    public void ReplaceAllLineEnds_ReplaceFullLineEnding ()
    {
      var text1 = "test1";
      var text2 = "test2";
      var text3 = "test3";
      var text4 = "test4";
      var fulltext = text1 + "\r\n" + text2 + "\n" + text3 + "\r" + text4;
      Assert.AreEqual (text1 + text2 + text3 + text4, NewLineManager.ReplaceAllLineEnds (fulltext, string.Empty));
    }
  }
}