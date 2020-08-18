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

using Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

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

    private ObservableCollection<LinkedCommand> _linkedCommands;

    private static readonly JsonSerializerOptions _jsonSerializerOptions =
        new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            Converters = { new JsonStringEnumConverter (JsonNamingPolicy.CamelCase, false) }
        };

    static OptionsStore ()
    {
      CurrentConfig = new OptionsStore (true);
    }

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

    //[JsonConverter (typeof (JsonBoolConverter))]
    public bool InsertHeaderIntoNewFiles { get; set; }

    //[JsonConverter (typeof (JsonBoolConverter))]
    public bool UseRequiredKeywords { get; set; }

    public string RequiredKeywords { get; set; }

    public ICollection<LinkedCommand> LinkedCommands
    {
      get => _linkedCommands;
      set
      {
        if (_linkedCommands != null)
        {
          _linkedCommands.CollectionChanged -= InvokeLinkedCommandsChanged;
          InvokeLinkedCommandsChanged (_linkedCommands, new NotifyCollectionChangedEventArgs (NotifyCollectionChangedAction.Remove, _linkedCommands));
        }

        _linkedCommands = new ObservableCollection<LinkedCommand> (value);
        if (_linkedCommands != null)
        {
          _linkedCommands.CollectionChanged += InvokeLinkedCommandsChanged;
          InvokeLinkedCommandsChanged (_linkedCommands, new NotifyCollectionChangedEventArgs (NotifyCollectionChangedAction.Add, _linkedCommands));
        }
      }
    }

    public string DefaultLicenseHeaderFileText { get; set; }

    public IEnumerable<Language> Languages { get; set; }

    public IOptionsStore Clone ()
    {
      var clonedObject = new OptionsStore
                         {
                             InsertHeaderIntoNewFiles = InsertHeaderIntoNewFiles,
                             UseRequiredKeywords = UseRequiredKeywords,
                             RequiredKeywords = RequiredKeywords,
                             LinkedCommands = LinkedCommands.Select (x => x.Clone()).ToList(),
                             DefaultLicenseHeaderFileText = DefaultLicenseHeaderFileText,
                             Languages = Languages.Select (x => x.Clone())
                         };

      return clonedObject;
    }

    public event EventHandler<NotifyCollectionChangedEventArgs> LinkedCommandsChanged;

    /// <summary>
    ///   Serializes an <see cref="IOptionsStore" /> instance to a file in the file system.
    /// </summary>
    /// <param name="options">The <see cref="IOptionsStore" /> instance to serialize.</param>
    /// <param name="filePath">The path to which an options file should be persisted.</param>
    public static async Task SaveAsync (OptionsStore options, string filePath)
    {
      try
      {
        using var stream = new FileStream (filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        await JsonSerializer.SerializeAsync (stream, options, _jsonSerializerOptions);
      }
      catch (ArgumentNullException ex)
      {
        throw new SerializationException ("File stream for serializing configuration was not present", ex);
      }
      catch (NotSupportedException ex)
      {
        throw new SerializationException ("At least one JSON converter for serializing configuration members was not found", ex);
      }
      catch (Exception ex)
      {
        throw new SerializationException ("An unspecified error occured while serializing configuration", ex);
      }
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
    public static async Task<OptionsStore> LoadAsync (string filePath)
    {
      try
      {
        using var stream = new FileStream (filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return await JsonSerializer.DeserializeAsync<OptionsStore> (stream, _jsonSerializerOptions);
      }
      catch (ArgumentNullException ex)
      {
        throw new SerializationException ("File stream for deserializing configuration was not present", ex);
      }
      catch (NotSupportedException ex)
      {
        throw new SerializationException ("At least one JSON converter for deserializing configuration members was not found", ex);
      }
      catch (FileNotFoundException ex)
      {
        throw new SerializationException ("File to deserialize configuration from was not found", ex);
      }
      catch (JsonException ex)
      {
        throw new SerializationException ("The file content is not in a valid format", ex);
      }
      catch (Exception ex)
      {
        throw new SerializationException ("An unspecified error occured while deserializing configuration", ex);
      }
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

    protected virtual void InvokeLinkedCommandsChanged (object sender, NotifyCollectionChangedEventArgs e)
    {
      LinkedCommandsChanged?.Invoke (sender, e);
    }
  }
}