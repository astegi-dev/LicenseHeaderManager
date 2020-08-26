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
using Core.Options;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;

namespace LicenseHeaderManager.Options
{
  public class OptionsFacade : IOptionsFacade
  {
    private readonly CoreOptions _coreOptions;

    private readonly VisualStudioOptions _visualStudioOptions;

    public static readonly string DefaultCoreOptionsPath = Environment.ExpandEnvironmentVariables (@"%APPDATA%\rubicon\LicenseHeaderManager\CoreOptions.json");
    public static readonly string DefaultVisualStudioOptionsPath = Environment.ExpandEnvironmentVariables (@"%APPDATA%\rubicon\LicenseHeaderManager\VisualStudioOptions.json");
    public static readonly string DefaultLogPath = Environment.ExpandEnvironmentVariables (@"%APPDATA%\rubicon\LicenseHeaderManager\logs_lhm");

    /// <summary>
    ///   Gets or sets the currently up-to-date configuration of the License Header Manager Extension, along
    ///   with the corresponding options for the Core.
    /// </summary>
    public static OptionsFacade CurrentOptions { get; set; }

    static OptionsFacade ()
    {
      CurrentOptions = new OptionsFacade();
    }

    public OptionsFacade ()
    {
      _coreOptions = new CoreOptions();
      _visualStudioOptions = new VisualStudioOptions();
      _visualStudioOptions.LinkedCommandsChanged += InvokeLinkedCommandsChanged;
    }

    private OptionsFacade (CoreOptions coreOptions, VisualStudioOptions visualStudioOptions)
    {
      _coreOptions = coreOptions;
      _visualStudioOptions = visualStudioOptions;
      _visualStudioOptions.LinkedCommandsChanged += InvokeLinkedCommandsChanged;
    }

    public bool UseRequiredKeywords
    {
      get => _coreOptions.UseRequiredKeywords;
      set => _coreOptions.UseRequiredKeywords = value;
    }

    public string RequiredKeywords
    {
      get => _coreOptions.RequiredKeywords;
      set => _coreOptions.RequiredKeywords = value;
    }

    public string LicenseHeaderFileText
    {
      get => _coreOptions.LicenseHeaderFileText;
      set => _coreOptions.LicenseHeaderFileText = value;
    }

    public ObservableCollection<Language> Languages
    {
      get => _coreOptions.Languages;
      set => _coreOptions.Languages = value;
    }

    public bool InsertInNewFiles
    {
      get => _visualStudioOptions.InsertInNewFiles;
      set => _visualStudioOptions.InsertInNewFiles = value;
    }

    public ObservableCollection<LinkedCommand> LinkedCommands
    {
      get => _visualStudioOptions.LinkedCommands;
      set => _visualStudioOptions.LinkedCommands = _visualStudioOptions.LinkedCommands != null ? new ObservableCollection<LinkedCommand> (value) : null;
    }

    public string Version
    {
      get => _coreOptions.Version;
      set => _coreOptions.Version = value;
    }

    /// <summary>
    ///   Serializes an <see cref="OptionsFacade" /> instance along with its underlying
    ///   <see cref="CoreOptions"/> and <see cref="VisualStudioOptions"/> instances into separate files
    ///   in the file system.
    /// </summary>
    /// <param name="options">The <see cref="OptionsFacade" /> instance to serialize.</param>
    /// <param name="coreOptionsFilePath">The path to which the <see cref="CoreOptions"/> should be serialized.</param>
    /// <param name="visualStudioOptionsFilePath">The path to which the <see cref="VisualStudioOptions"/> should be serialized.</param>
    public static async Task SaveAsync (OptionsFacade options, string coreOptionsFilePath = null, string visualStudioOptionsFilePath = null)
    {
      await JsonOptionsManager.SerializeAsync (options._coreOptions, coreOptionsFilePath ?? DefaultCoreOptionsPath);
      await JsonOptionsManager.SerializeAsync (options._visualStudioOptions, visualStudioOptionsFilePath ?? DefaultVisualStudioOptionsPath);
    }

    /// <summary>
    ///   Deserializes an <see cref="OptionsFacade" /> instance from files representing
    ///   <see cref="CoreOptions"/> and <see cref="VisualStudioOptions"/> instances in the file system.
    /// </summary>
    /// <param name="coreOptionsFilePath">
    ///   The path to an options file from which a corresponding <see cref="CoreOptions" /> instance
    ///   should be constructed.
    /// </param>
    /// <param name="visualStudioOptionsFilePath">
    ///   The path to an options file from which a corresponding <see cref="VisualStudioOptions" /> instance
    ///   should be constructed.
    /// </param>
    /// <returns>
    ///   An <see cref="OptionsFacade" /> instance that represents to configuration contained in the file specified by
    ///   <paramref name="coreOptionsFilePath" />.
    ///   If there were errors upon deserialization, <see langword="null" /> is returned.
    /// </returns>
    public static async Task<OptionsFacade> LoadAsync (string coreOptionsFilePath = null, string visualStudioOptionsFilePath = null)
    {
      var corePath = coreOptionsFilePath ?? DefaultCoreOptionsPath;
      var visualStudioPath = visualStudioOptionsFilePath ?? DefaultVisualStudioOptionsPath;

      // if either of the option files is not found, create it with default options and save it before loading
      if (!System.IO.File.Exists (corePath))
        await CoreOptions.SaveAsync (new CoreOptions(), corePath);

      if (!System.IO.File.Exists (visualStudioPath))
        await VisualStudioOptions.SaveAsync (new VisualStudioOptions(), visualStudioPath);

      var coreOptions = await JsonOptionsManager.DeserializeAsync<CoreOptions> (corePath);
      var visualStudioOptions = await JsonOptionsManager.DeserializeAsync<VisualStudioOptions> (visualStudioPath);

      return new OptionsFacade (coreOptions, visualStudioOptions);
    }

    public event EventHandler<NotifyCollectionChangedEventArgs> LinkedCommandsChanged;

    public IOptionsFacade Clone ()
    {
      var clonedObject = new OptionsFacade
                         {
                             UseRequiredKeywords = UseRequiredKeywords,
                             RequiredKeywords = RequiredKeywords,
                             LicenseHeaderFileText = LicenseHeaderFileText,
                             Languages = new ObservableCollection<Language>(Languages.Select (x => x.Clone())),
                             InsertInNewFiles = InsertInNewFiles,
                             LinkedCommands = new ObservableCollection<LinkedCommand> (LinkedCommands.Select (x => x.Clone()))
                         };
      return clonedObject;
    }

    protected virtual void InvokeLinkedCommandsChanged (object sender, NotifyCollectionChangedEventArgs e)
    {
      LinkedCommandsChanged?.Invoke (sender, e);
    }
  }
}