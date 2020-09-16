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

namespace Core.Tests
{
  [TestFixture]
  public class DocumentHeaderPropertyTest
  {
    private DocumentHeaderProperty _documentHeaderPropertyTrue;
    private DocumentHeaderProperty _documentHeaderPropertyFalse;
    private string _token;

    [SetUp]
    public void Setup()
    {
      _token = "%TestToken%";
      _documentHeaderPropertyTrue = new DocumentHeaderProperty(_token, header => true, header => "// test header");
      _documentHeaderPropertyFalse = new DocumentHeaderProperty(_token, header => false, header => null);
    }

    [Test]
    public void DocumentHeaderProperty_ValidInput_ReturnsValidProperties()
    {
      var actualToken = _documentHeaderPropertyTrue.Token;

      Assert.That(actualToken, Is.EqualTo(_token));
    }

    [Test]
    public void CanCreateValue_DocumentHeaderPropertyTrue_ReturnsTrue()
    {
      var actual = _documentHeaderPropertyTrue.CanCreateValue(null);

      Assert.That (actual, Is.True);
    }

    [Test]
    public void CanCreateValue_DocumentHeaderPropertyFalse_ReturnsFalse()
    {
      var actual = _documentHeaderPropertyFalse.CanCreateValue(null);

      Assert.That(actual, Is.False);
    }

    [Test]
    public void CreateValue_DocumentHeaderPropertyTrue_ReturnsDocumentHeader()
    {
      var actual = _documentHeaderPropertyTrue.CreateValue(null);

      Assert.That(actual, Is.EqualTo("// test header"));
    }

    [Test]
    public void CreateValue_DocumentHeaderPropertyFalse_ReturnsNull()
    {
      var actual = _documentHeaderPropertyFalse.CreateValue(null);

      Assert.That(actual, Is.Null);
    }
  }
}