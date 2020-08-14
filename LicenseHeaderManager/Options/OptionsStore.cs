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
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using Core;
using LicenseHeaderManager.Utils;
using Newtonsoft.Json;
using ErrorEventArgs = Newtonsoft.Json.Serialization.ErrorEventArgs;

namespace LicenseHeaderManager.Options
{
  internal class OptionsStore : IOptionsStore
  {
    private const bool c_defaultInsertHeaderIntoNewFiles = false;
    private const bool c_defaultUseRequiredKeywords = true;
    private const string c_defaultRequiredKeywords = "license, copyright, (c), ©";

    private static readonly ObservableCollection<LinkedCommand> _defaultLinkedCommands = new ObservableCollection<LinkedCommand>();

    private readonly string _defaultDefaultLicenseHeaderFileText = GetDefaultLicenseHeader();

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

    static OptionsStore ()
    {
      CurrentConfig = new OptionsStore (true);
    }

    [JsonConstructor]
    public OptionsStore ()
    {
      SetDefaults();
    }

    public OptionsStore (bool initializeWithDefaultValues)
    {
      if (initializeWithDefaultValues)
        SetDefaults();
    }

    /// <summary>
    ///   Gets or sets the currently up-to-date configuration of the License Header Manager Extension.
    /// </summary>
    public static OptionsStore CurrentConfig { get; set; }

    [JsonConverter (typeof (JsonBoolConverter))]
    public bool InsertHeaderIntoNewFiles { get; set; }

    [JsonConverter (typeof (JsonBoolConverter))]
    public bool UseRequiredKeywords { get; set; }

    public string RequiredKeywords { get; set; }

    public IEnumerable<LinkedCommand> LinkedCommands { get; set; }

    public string DefaultLicenseHeaderFileText { get; set; }

    public IEnumerable<Language> Languages { get; set; }

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
    ///   Serializes an <see cref="IOptionsStore" /> instance to a file in the file system.
    /// </summary>
    /// <param name="options">The <see cref="IOptionsStore" /> instance to serialize.</param>
    /// <param name="filePath">The path to which an options file should be persisted.</param>
    public static void Save (IOptionsStore options, string filePath)
    {
      var errors = new List<string>();
      var serializer = new JsonSerializer
                       {
                           Formatting = Formatting.Indented
                       };
      serializer.Error += (sender, args) => OnSerializerError (args, errors);

      using var sw = new StreamWriter (filePath);
      using JsonWriter writer = new JsonTextWriter (sw);
      serializer.Serialize (writer, options);

      if (errors.Count == 0)
        return;

      MessageBox.Show (
          $"{errors.Count} errors were encountered while deserializing:\n*) {string.Join ("\n*) ", errors)}",
          "Deserialization failed",
          MessageBoxButton.OK,
          MessageBoxImage.Error);
    }

    /// <summary>
    ///   Deserializes an <see cref="IOptionsStore" /> instance from a file in the file system.
    /// </summary>
    /// <param name="filePath">
    ///   The path to an options file from which a corresponding <see cref="IOptionsStore" /> instance
    ///   should be constructed.
    /// </param>
    /// <returns>
    ///   An <see cref="IOptionsStore" /> instance that represents to configuration contained in the file specified by
    ///   <paramref name="filePath" />.
    ///   If there were errors upon deserialization, <see langword="null" /> is returned.
    /// </returns>
    public static OptionsStore Load (string filePath)
    {
      var errors = new List<string>();
      using var file = File.OpenText (filePath);
      var serializer = new JsonSerializer
                       {
                           ObjectCreationHandling = ObjectCreationHandling.Replace
                       };

      serializer.Error += (sender, args) => OnSerializerError (args, errors);
      var deserializedObject = (OptionsStore) serializer.Deserialize (file, typeof (OptionsStore));

      if (errors.Count == 0)
        return deserializedObject;

      MessageBox.Show (
          $"{errors.Count} errors were encountered while deserializing:\n*) {string.Join ("\n*) ", errors)}",
          "Deserialization failed",
          MessageBoxButton.OK,
          MessageBoxImage.Error);
      return null;
    }

    private static void OnSerializerError (ErrorEventArgs args, ICollection<string> errors)
    {
      // log errors only once
      if (args.CurrentObject != args.ErrorContext.OriginalObject)
        return;
      args.ErrorContext.Handled = true;

      if (!errors.Contains (args.ErrorContext.Error.Message))
        errors.Add (args.ErrorContext.Error.Message);
    }

    /// <summary>
    ///   Sets all public members of this <see cref="IOptionsStore" /> instance to their default values.
    /// </summary>
    /// <remarks>The default values are implementation-dependent.</remarks>
    public void SetDefaults ()
    {
      InsertHeaderIntoNewFiles = c_defaultInsertHeaderIntoNewFiles;
      UseRequiredKeywords = c_defaultUseRequiredKeywords;
      RequiredKeywords = c_defaultRequiredKeywords;
      LinkedCommands = new ObservableCollection<LinkedCommand> (_defaultLinkedCommands);
      DefaultLicenseHeaderFileText = _defaultDefaultLicenseHeaderFileText;
      Languages = new ObservableCollection<Language> (_defaultLanguages);
    }

    private static string GetDefaultLicenseHeader ()
    {
      using var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream (typeof (LicenseHeadersPackage), "Resources.default.licenseheader");
      if (resource == null)
        return string.Empty;

      using var reader = new StreamReader (resource, Encoding.UTF8);
      return reader.ReadToEnd();
    }
  }
}