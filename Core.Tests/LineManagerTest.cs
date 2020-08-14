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