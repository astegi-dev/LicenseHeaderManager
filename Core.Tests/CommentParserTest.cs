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

namespace Core.Tests
{
  [TestFixture]
  public class CommentParserTest
  {
    private void Test_Positive(string[] header, string[] text)
    {
      var headerString = string.Join(Environment.NewLine, header);
      var textString = string.Join(Environment.NewLine, text);

      if (header.Length > 0 && text.Length > 0)
        headerString += Environment.NewLine;

      var parser = new CommentParser("//", "/*", "*/", "#region", "#endregion");
      Assert.That(headerString, Is.EqualTo(parser.Parse(headerString + textString)));
    }

    private void Test_Negative(string[] text)
    {
      var textString = string.Join(Environment.NewLine, text);
      var parser = new CommentParser("//", "/*", "*/", "#region", "#endregion");
      Assert.That(() => parser.Parse(textString), Throws.InstanceOf<ParseException>());
    }

    [Test]
    public void Parse_CommentBeforeRegionWithText_ReturnsHeaderWithoutNonCommentTextWithEndingNewLine()
    {
      var header = new[]
                   {
                       "",
                       "//This is a comment."
                   };

      var text = new[]
                 {
                     "#region copyright",
                     "//This is a comment.",
                     "This is not a comment.",
                     "#endregion"
                 };

      Test_Positive(header, text);
    }

    [Test]
    public void Parse_EmptyFile_ReturnsEmptyString()
    {
      var header = new string[0];
      var text = new string[0];

      Test_Positive(header, text);
    }

    [Test]
    public void Parse_EmptyLinesBeforeComment_ReturnsHeaderWithoutNonCommentTextWithEndingNewLine()
    {
      var header = new[]
                   {
                       "",
                       "",
                       "//This is a comment."
                   };

      var text = new[]
                 { "This is not a comment." };

      Test_Positive(header, text);
    }

    [Test]
    public void Parse_EndRegionWithSpace_ReturnsHeaderWithEndingNewLine()
    {
      var header = new[]
                   {
                       "#Region ",
                       "//This is a comment.",
                       "#End Region"
                   };

      var headerString = string.Join(Environment.NewLine, header);

      headerString += Environment.NewLine;

      var parser = new CommentParser("//", "/*", "*/", "#Region", "#End Region");
      Assert.That(headerString, Is.EqualTo(parser.Parse(headerString)));
    }

    [Test]
    public void Parse_EveryCommentPossible_ReturnsHeaderWithoutNonCommentText()
    {
      var header = new[]
                   {
                       "",
                       " ",
                       "#region copyright",
                       "  ",
                       "//This is a comment.",
                       "",
                       "/*This is also part",
                       "",
                       "of the header*/",
                       "#region something else",
                       "",
                       "//So is this.",
                       "#endregion",
                       " ",
                       "#endregion",
                       " "
                   };

      var text = new[]
                 { "This is not a comment." };

      Test_Positive(header, text);
    }

    [Test]
    public void Parse_HeaderOnly_ReturnsHeader()
    {
      var header = new[]
                   { "//This is a comment." };

      var text = new string[0];

      Test_Positive(header, text);
    }

    [Test]
    public void Parse_HeaderOnlyWithEmptyLines_ReturnsHeader()
    {
      var header = new[]
                   {
                       "",
                       "",
                       "//This is a comment.",
                       "",
                       ""
                   };

      var text = new string[0];

      Test_Positive(header, text);
    }

    [Test]
    public void Parse_MissingEndComment_ThrowsParseException()
    {
      var text = new[]
                 {
                     "/* This is a comment.",
                     "This is not a comment."
                 };

      Test_Negative(text);
    }

    [Test]
    public void Parse_MissingEndRegion_ReturnsEmptyString()
    {
      var header = new string[0];

      var text = new[]
                 {
                     "#region copyright",
                     "//This is a comment.",
                     "This is not a comment."
                 };

      Test_Positive(header, text);
    }

    [Test]
    public void Parse_MixedComments_ReturnsHeaderWithEndingNewLine()
    {
      var header = new[]
                   {
                       "/* This is a comment that",
                       "   spans multiple lines.  */",
                       "// This is also part of the header.",
                       ""
                   };

      var text = new[]
                 { "//This is another comment." };

      Test_Positive(header, text);
    }

    [Test]
    public void Parse_MultiLineBeginEndComment_ReturnsHeaderWithoutNonCommentTextWithEndingNewLine()
    {
      var header = new[]
                   {
                       "/* This is a comment that ",
                       "   spans multiple lines.  */"
                   };

      var text = new[]
                 { "This is not a comment." };

      Test_Positive(header, text);
    }

    [Test]
    public void Parse_MultiLineHeaderOnly_ReturnsHeaderWithoutEndingNewLine()
    {
      var header = new[]
                   {
                       "/*",
                       "This is a comment that ",
                       "spans multiple lines.",
                       "*/"
                   };

      var text = new string[0];

      Test_Positive(header, text);
    }

    [Test]
    public void Parse_MultipleBeginEndComments_ReturnsHeaderWithoutNonCommentTextWithEndingNewLine()
    {
      var header = new[]
                   {
                       "/* This is a comment that */",
                       "/* spans multiple lines.  */"
                   };

      var text = new[]
                 { "This is not a comment." };

      Test_Positive(header, text);
    }

    [Test]
    public void Parse_MultipleLineBeginEndCommentsWithEmptyLine_ReturnsHeaderWithEndingNewLine()
    {
      var header = new[]
                   {
                       "/* This is a comment that ",
                       "   spans multiple lines.  */",
                       ""
                   };

      var text = new[]
                 { "/* This is another comment. */" };

      Test_Positive(header, text);
    }

    [Test]
    public void Parse_MultipleLineComments_ReturnsHeaderWithoutNonCommentText()
    {
      var header = new[]
                   {
                       "//This is a comment that",
                       "//spans multiple lines."
                   };

      var text = new[]
                 { "This is not a comment." };

      Test_Positive(header, text);
    }

    [Test]
    public void Parse_MultipleLineCommentsWithEmptyLine_ReturnsHeaderWithEndingNewLine()
    {
      var header = new[]
                   {
                       "//This is a comment that",
                       "//spans multiple lines.",
                       ""
                   };

      var text = new[]
                 { "//This is another comment." };

      Test_Positive(header, text);
    }

    [Test]
    public void Parse_MultipleRegions_ReturnsHeaderWithoutNonCommentText()
    {
      var header = new[]
                   {
                       "#region copyright",
                       "//This is a comment.",
                       "",
                       "#endregion",
                       "#region something else",
                       "",
                       "#endregion"
                   };

      var text = new[]
                 { "This is not a comment." };

      Test_Positive(header, text);
    }

    [Test]
    public void Parse_NestedMissingEndRegion_ReturnsEmptyString()
    {
      var header = new string[0];

      var text = new[]
                 {
                     "#region copyright",
                     "//This is a comment.",
                     "#region something else",
                     "",
                     "#endregion",
                     "This is not a comment."
                 };

      Test_Positive(header, text);
    }

    [Test]
    public void Parse_NestedRegions_ReturnsHeaderWithEndingNewLine()
    {
      var header = new[]
                   {
                       "#region copyright",
                       "//This is a comment.",
                       "",
                       "#region something else",
                       "",
                       "#endregion",
                       "#endregion"
                   };

      var text = new[]
                 { "This is not a comment." };

      Test_Positive(header, text);
    }

    [Test]
    public void Parse_NoHeader_ReturnsEmptyString()
    {
      var header = new string[0];

      var text = new[]
                 {
                     "This is not a comment.",
                     "//This is not part of the header."
                 };

      Test_Positive(header, text);
    }

    [Test]
    public void Parse_NoHeaderButNormalRegion_ReturnsEmptyString()
    {
      var header = new string[0];

      var text = new[]
                 {
                     "#region normal region",
                     "This is not a comment.",
                     "#endregion"
                 };

      Test_Positive(header, text);
    }

    [Test]
    public void Parse_NoHeaderWithEmptyLines_ReturnsEmptyString()
    {
      var header = new string[0];

      var text = new[]
                 {
                     "",
                     "",
                     "This is not a comment.",
                     "//This is not part of the header."
                 };

      Test_Positive(header, text);
    }

    [Test]
    public void Parse_Region_ReturnsHeaderWithoutNonCommentTextWithEndingNewLine()
    {
      var header = new[]
                   {
                       "#region copyright",
                       "//This is a comment.",
                       "#endregion"
                   };

      var text = new[]
                 { "This is not a comment." };

      Test_Positive(header, text);
    }

    [Test]
    public void Parse_RegionWithEmptyLines_ReturnsHeaderWithEndingNewLine()
    {
      var header = new[]
                   {
                       "#region copyright",
                       "//This is a comment.",
                       "",
                       "//This is also part of the header.",
                       "#endregion",
                       ""
                   };

      var text = new[]
                 { "//This is another comment." };

      Test_Positive(header, text);
    }

    [Test]
    public void Parse_TextInRegion_ReturnsEmptyString()
    {
      var header = new string[0];

      var text = new[]
                 {
                     "#region copyright",
                     "//This is a comment.",
                     "This is not a comment.",
                     "#endregion"
                 };

      Test_Positive(header, text);
    }

    [Test]
    public void Parse_RegionsInText_ReturnsHeaderWithEndingNewLine()
    {
      var header = new[]
                   {
                       "#region copyright",
                       "#region something",
                       "",
                       "#endregion",
                       "#endregion"
                   };

      var text = new[]
                 {
                     "#region something",
                     "#region something"
                 };

      Test_Positive(header, text);
    }

    [Test]
    public void Parse_EndRegionWithoutRegion_ThrowsParseException()
    {
      var header = new[]
                   {
                       "#endregion"
                   };

      Test_Negative(header);
    }

    [Test]
    public void Parse_EndRegionWithoutRegionAndSplitEndregion_ThrowsParseException()
    {
      var parser = new CommentParser("//", "/*", "*/", "#region", "#end region");
      var header = new[]
                   {
                       "#end",
                       "region"
                   };

      var textString = string.Join(Environment.NewLine, header);
      Assert.That(() => parser.Parse(textString), Throws.InstanceOf<ParseException>());
    }
  }
}