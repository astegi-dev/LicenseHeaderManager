using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Core;

namespace LicenseHeaderManager.Options
{
  internal class OptionsStore : IOptionsStore
  {
    private readonly ObservableCollection<Language> _defaultLanguages = new ObservableCollection<Language>
                                                                        {
                                                                            new Language
                                                                            {
                                                                                Extensions = new[] { ".cs" }, LineComment = "//", BeginComment = "/*",
                                                                                EndComment = "*/", BeginRegion = "#region", EndRegion = "#endregion"
                                                                            },
                                                                            new Language
                                                                            {
                                                                                Extensions = new[] { ".c", ".cpp", ".cxx", ".h", ".hpp" },
                                                                                LineComment = "//", BeginComment = "/*", EndComment = "*/"
                                                                            },
                                                                            new Language
                                                                            {
                                                                                Extensions = new[] { ".vb" }, LineComment = "'", BeginRegion = "#Region",
                                                                                EndRegion = "#End Region"
                                                                            },
                                                                            new Language
                                                                            {
                                                                                Extensions = new[] { ".aspx", ".ascx" }, BeginComment = "<%--",
                                                                                EndComment = "--%>"
                                                                            },
                                                                            new Language
                                                                            {
                                                                                Extensions = new[]
                                                                                             {
                                                                                                 ".htm", ".html", ".xhtml", ".xml", ".xaml", ".resx",
                                                                                                 ".config", ".xsd"
                                                                                             },
                                                                                BeginComment = "<!--", EndComment = "-->",
                                                                                SkipExpression =
                                                                                    @"(<\?xml(.|\s)*?\?>)?(\s*<!DOCTYPE(.|\s)*?>)?( |\t)*(\n|\r\n|\r)?"
                                                                            },
                                                                            new Language { Extensions = new[] { ".css" }, BeginComment = "/*", EndComment = "*/" },
                                                                            new Language
                                                                            {
                                                                                Extensions = new[] { ".js", ".ts" }, LineComment = "//", BeginComment = "/*",
                                                                                EndComment = "*/",
                                                                                SkipExpression = @"(/// *<reference.*/>( |\t)*(\n|\r\n|\r)?)*"
                                                                            },
                                                                            new Language
                                                                            {
                                                                                Extensions = new[] { ".sql" }, BeginComment = "/*", EndComment = "*/",
                                                                                LineComment = "--"
                                                                            },
                                                                            new Language
                                                                            {
                                                                                Extensions = new[] { ".php" }, BeginComment = "/*", EndComment = "*/",
                                                                                LineComment = "//"
                                                                            },
                                                                            new Language
                                                                            {
                                                                                Extensions = new[] { ".wxs", ".wxl", ".wxi" }, BeginComment = "<!--",
                                                                                EndComment = "-->"
                                                                            },
                                                                            new Language { Extensions = new[] { ".py" }, BeginComment = "\"\"\"", EndComment = "\"\"\"" },
                                                                            new Language
                                                                            {
                                                                                Extensions = new[] { ".fs" }, BeginComment = "(*", EndComment = "*)",
                                                                                LineComment = "//"
                                                                            },
                                                                            new Language
                                                                            {
                                                                                Extensions = new[] { ".cshtml", ".vbhtml" }, BeginComment = "@*",
                                                                                EndComment = "*@"
                                                                            }
                                                                        };

    public OptionsStore ()
    {
      SetDefaults();
    }

    public bool InsertHeaderIntoNewFiles { get; set; }

    public bool UseRequiredKeywords { get; set; }

    public string RequiredKeywords { get; set; }

    public IEnumerable<LinkedCommand> LinkedCommands { get; set; }

    public string DefaultLicenseHeaderFileText { get; set; }

    public IEnumerable<Language> Languages { get; set; }

    public void Save (string filePath)
    {
      throw new NotImplementedException();
    }

    public IOptionsStore Load (string filePath)
    {
      throw new NotImplementedException();
    }

    public IOptionsStore Clone ()
    {
      var clonedObject = new OptionsStore
                         {
                             InsertHeaderIntoNewFiles = InsertHeaderIntoNewFiles,
                             UseRequiredKeywords = UseRequiredKeywords,
                             RequiredKeywords = RequiredKeywords,
                             LinkedCommands = LinkedCommands.Select (x => x.Clone()),
                             DefaultLicenseHeaderFileText = DefaultLicenseHeaderFileText,
                             Languages = Languages.Select (x => x.Clone())
                         };

      return clonedObject;
    }

    public void SetDefaults ()
    {
      InsertHeaderIntoNewFiles = false;
      UseRequiredKeywords = true;
      RequiredKeywords = "license, copyright, (c), ©";
      LinkedCommands = new ObservableCollection<LinkedCommand>();
      DefaultLicenseHeaderFileText = GetDefaultLicenseHeader();
      Languages = new ObservableCollection<Language> (_defaultLanguages);
    }

    private string GetDefaultLicenseHeader ()
    {
      using var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream (typeof (LicenseHeadersPackage), "Resources.default.licenseheader");
      if (resource == null)
        return string.Empty;

      using var reader = new StreamReader (resource, Encoding.UTF8);
      return reader.ReadToEnd();
    }
  }
}