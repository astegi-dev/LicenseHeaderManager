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
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Core.Options;

namespace LicenseHeaderManager.Options
{
  [LicenseHeaderManagerOptions]
  internal class VisualStudioOptions : IVisualStudioOptions
  {
    private const bool c_defaultInsertHeaderIntoNewFiles = false;
    private static readonly ObservableCollection<LinkedCommand> _defaultLinkedCommands = new ObservableCollection<LinkedCommand>();
    private ObservableCollection<LinkedCommand> _linkedCommands;

    static VisualStudioOptions ()
    {
      CurrentConfig = new VisualStudioOptions (true);
    }

    public VisualStudioOptions ()
    {
      SetDefaults();
    }

    public VisualStudioOptions (bool initializeWithDefaultValues)
    {
      if (initializeWithDefaultValues)
        SetDefaults();
    }

    /// <summary>
    ///   Gets or sets the currently up-to-date configuration of the License Header Manager Extension.
    /// </summary>
    public static VisualStudioOptions CurrentConfig { get; set; }

    public bool InsertHeaderIntoNewFiles { get; set; }

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

    public IVisualStudioOptions Clone ()
    {
      var clonedObject = new VisualStudioOptions
                         {
                             InsertHeaderIntoNewFiles = InsertHeaderIntoNewFiles,
                             LinkedCommands = LinkedCommands.Select (x => x.Clone()).ToList()
                         };
      return clonedObject;
    }

    public event EventHandler<NotifyCollectionChangedEventArgs> LinkedCommandsChanged;

    /// <summary>
    ///   Serializes an <see cref="IVisualStudioOptions" /> instance to a file in the file system.
    /// </summary>
    /// <param name="visualStudioOptions">The <see cref="IVisualStudioOptions" /> instance to serialize.</param>
    /// <param name="filePath">The path to which an options file should be persisted.</param>
    public static async Task SaveAsync (VisualStudioOptions visualStudioOptions, string filePath)
    {
      await JsonOptionsManager.SerializeAsync (visualStudioOptions, filePath);
    }

    /// <summary>
    ///   Deserializes an <see cref="IVisualStudioOptions" /> instance from a file in the file system.
    /// </summary>
    /// <param name="filePath">
    ///   The path to an options file from which a corresponding <see cref="IVisualStudioOptions" /> instance
    ///   should be constructed.
    /// </param>
    /// <returns>
    ///   An <see cref="IVisualStudioOptions" /> instance that represents to configuration contained in the file specified by
    ///   <paramref name="filePath" />.
    ///   If there were errors upon deserialization, <see langword="null" /> is returned.
    /// </returns>
    public static async Task<VisualStudioOptions> LoadAsync (string filePath)
    {
      return await JsonOptionsManager.DeserializeAsync<VisualStudioOptions> (filePath);
    }

    /// <summary>
    ///   Sets all public members of this <see cref="IVisualStudioOptions" /> instance to their default values.
    /// </summary>
    /// <remarks>The default values are implementation-dependent.</remarks>
    public void SetDefaults ()
    {
      InsertHeaderIntoNewFiles = c_defaultInsertHeaderIntoNewFiles;
      LinkedCommands = new ObservableCollection<LinkedCommand> (_defaultLinkedCommands);
    }

    protected virtual void InvokeLinkedCommandsChanged (object sender, NotifyCollectionChangedEventArgs e)
    {
      LinkedCommandsChanged?.Invoke (sender, e);
    }
  }
}