using NUnit.Framework;
using System.Collections.Generic;

namespace Core.Tests
{
  [TestFixture]
  class IntegrationTest
  {

    [Test]
    public void test()
    {
      var languages = new List<Language>
    {
      new Language { Extensions = new[] { ".cs" }, LineComment = "//", BeginComment = "/*", EndComment = "*/", BeginRegion = "#region", EndRegion = "#endregion" },
      new Language { Extensions = new[] { ".js", ".ts" }, LineComment = "//", BeginComment = "/*", EndComment = "*/", SkipExpression = @"(/// *<reference.*/>( |\t)*(\n|\r\n|\r)?)*" },
    };

      var replacer = new LicenseHeaderReplacer(languages, new [] {"1"});

      var headers = new Dictionary<string, string[]>();
      headers.Add(".cs", new[] { "// first line 1", "// second line", "// copyright" });
      var res = replacer.RemoveOrReplaceHeader(new LicenseHeaderInput(@"C:\Users\gabriel.ratschiller\Desktop\TestFile.cs", headers, null));
    }

  }
}
