using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Core.Options
{
  [LicenseHeaderManagerOptions]
  public class CoreOptions : ICoreOptions
  {
    public const bool c_defaultUseRequiredKeywords = true;
    public const string c_defaultRequiredKeywords = "license, copyright, (c), ©";
    public static readonly string _defaultDefaultLicenseHeaderFileText = GetDefaultLicenseHeader();

    public static readonly ObservableCollection<Language> _defaultLanguages = new ObservableCollection<Language>
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

    public CoreOptions ()
    {
      SetDefaultValues();
    }

    public CoreOptions (bool initializeWithDefaultValues)
    {
      if (initializeWithDefaultValues)
        SetDefaultValues();
      else
        InitializeValues();
    }

    public bool UseRequiredKeywords { get; set; }

    public string RequiredKeywords { get; set; }

    public string DefaultLicenseHeaderFileText { get; set; }

    public ObservableCollection<Language> Languages { get; set; }

    public string Version { get; set; }

    /// <summary>
    ///   Sets all public members of this <see cref="ICoreOptions" /> instance to pre-configured default values.
    /// </summary>
    /// <remarks>The default values are implementation-dependent.</remarks>
    private void SetDefaultValues ()
    {
      UseRequiredKeywords = c_defaultUseRequiredKeywords;
      RequiredKeywords = c_defaultRequiredKeywords;
      DefaultLicenseHeaderFileText = _defaultDefaultLicenseHeaderFileText;
      Languages = new ObservableCollection<Language> (_defaultLanguages);
    }

    /// <summary>
    ///   Initializes all public members of this <see cref="ICoreOptions" /> instance.
    /// </summary>
    /// <remarks>The default values are implementation-dependent.</remarks>
    private void InitializeValues ()
    {
      Languages = new ObservableCollection<Language> (_defaultLanguages);
    }

    public ICoreOptions Clone ()
    {
      var clonedObject = new CoreOptions
                         {
                             UseRequiredKeywords = UseRequiredKeywords,
                             RequiredKeywords = RequiredKeywords,
                             DefaultLicenseHeaderFileText = DefaultLicenseHeaderFileText,
                             Languages = new ObservableCollection<Language> (Languages.Select (x => x.Clone()).ToList())
                         };

      return clonedObject;
    }

    /// <summary>
    ///   Serializes an <see cref="CoreOptions" /> instance to a file in the file system.
    /// </summary>
    /// <param name="options">The <see cref="CoreOptions" /> instance to serialize.</param>
    /// <param name="filePath">The path to which an options file should be persisted.</param>
    public static async Task SaveAsync (CoreOptions options, string filePath)
    {
      await JsonOptionsManager.SerializeAsync (options, filePath);
    }

    /// <summary>
    ///   Deserializes an <see cref="CoreOptions" /> instance from a file in the file system.
    /// </summary>
    /// <param name="filePath">
    ///   The path to an options file from which a corresponding <see cref="CoreOptions" /> instance
    ///   should be constructed.
    /// </param>
    /// <returns>
    ///   An <see cref="CoreOptions" /> instance that represents to configuration contained in the file specified by
    ///   <paramref name="filePath" />.
    ///   If there were errors upon deserialization, <see langword="null" /> is returned.
    /// </returns>
    public static async Task<CoreOptions> LoadAsync (string filePath)
    {
      return await JsonOptionsManager.DeserializeAsync<CoreOptions> (filePath);
    }

    private static string GetDefaultLicenseHeader ()
    {
      using var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream (typeof (ICoreOptions), "default.licenseheader");
      if (resource == null)
        return string.Empty;

      using var reader = new StreamReader (resource, Encoding.UTF8);
      return reader.ReadToEnd();
    }
  }
}