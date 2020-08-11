using Core;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

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

    /// <summary>
    /// Gets or sets the currently up-to-date configuration of the License Header Manager Extension.
    /// </summary>
    public static OptionsStore CurrentConfig { get; set; }

    static OptionsStore ()
    {
      CurrentConfig = new OptionsStore();
      CurrentConfig.SetDefaults();
    }

    public OptionsStore ()
    {
    }

    public bool InsertHeaderIntoNewFiles { get; set; }

    public bool UseRequiredKeywords { get; set; }

    public string RequiredKeywords { get; set; }

    public IEnumerable<LinkedCommand> LinkedCommands { get; set; }

    public string DefaultLicenseHeaderFileText { get; set; }

    public IEnumerable<Language> Languages { get; set; }

    /// <summary>
    ///   Serializes an <see cref="IOptionsStore" /> instance to a file in the file system.
    /// </summary>
    /// <param name="options">The <see cref="IOptionsStore" /> instance to serialize.</param>
    /// <param name="filePath">The path to which an options file should be persisted.</param>
    public static void Save (OptionsStore options, string filePath)
    {
      var serializer = new JsonSerializer
                       {
                           Formatting = Formatting.Indented,
                           NullValueHandling = NullValueHandling.Include
                       };
      using var sw = new StreamWriter (filePath);
      using JsonWriter writer = new JsonTextWriter (sw);
      serializer.Serialize (writer, options);
    }

    /// <summary>
    ///   Deserializes an <see cref="IOptionsStore" /> instance from a file in the file system.
    /// </summary>
    /// <param name="filePath">The path to an options file from which a corresponding <see cref="IOptionsStore"/> instance should be constructed.</param>
    /// <returns>An <see cref="IOptionsStore"/> instance that represents to configuration contained in the file specified by <paramref name="filePath"/>.</returns>
    public static OptionsStore Load (string filePath)
    {
      using var file = File.OpenText (filePath);
      var serializer = new JsonSerializer
                       {
                           NullValueHandling = NullValueHandling.Include
                       };
      return (OptionsStore) serializer.Deserialize (file, typeof (OptionsStore));
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

    /// <summary>
    /// Sets all public members of this <see cref="IOptionsStore"/> instance to their default values.
    /// </summary>
    /// <remarks>The default values are implementation-dependent.</remarks>
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