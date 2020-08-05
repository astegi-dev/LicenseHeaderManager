using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework.Constraints;

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
      headers.Add(".cs", new[] { "//1", "//2", "//3" });
      var res = replacer.RemoveOrReplaceHeader(@"C:\Users\gabriel.ratschiller\Desktop\TestFile.cs", headers);
    }

  }
}
