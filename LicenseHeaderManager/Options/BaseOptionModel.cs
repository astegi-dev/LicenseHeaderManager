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

using Microsoft;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Settings;
using Microsoft.VisualStudio.Threading;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;

namespace LicenseHeaderManager.Options
{
  /// <summary>
  /// A base class for specifying options
  /// </summary>
  public abstract class BaseOptionModel<T>
      where T : BaseOptionModel<T>, new()
  {
    private static readonly AsyncLazy<T> s_liveModel = new AsyncLazy<T> (CreateAsync, ThreadHelper.JoinableTaskFactory);

    private static readonly AsyncLazy<ShellSettingsManager> s_settingsManager = new AsyncLazy<ShellSettingsManager> (
        GetSettingsManagerAsync,
        ThreadHelper.JoinableTaskFactory);

    protected BaseOptionModel ()
    {
    }

    /// <summary>
    /// A singleton instance of the options. MUST be called form UI thread only
    /// </summary>
    public static T Instance
    {
      get
      {
        ThreadHelper.ThrowIfNotOnUIThread();

#pragma warning disable VSTHRD104 // Offer async methods
        return ThreadHelper.JoinableTaskFactory.Run (GetLiveInstanceAsync);
#pragma warning restore VSTHRD104 // Offer async methods
      }
    }

    /// <summary>
    /// Get the singleton instance of the options. Thread safe.
    /// </summary>
    public static Task<T> GetLiveInstanceAsync () => s_liveModel.GetValueAsync();

    /// <summary>
    /// Creates a new instance of the options class and loads the values from the store. For internal use only
    /// </summary>
    /// <returns></returns>
    public static async Task<T> CreateAsync ()
    {
      var instance = new T();
      await instance.LoadAsync();
      return instance;
    }

    /// <summary>
    /// The name of the options collection as stored in the registry.
    /// </summary>
    protected virtual string CollectionName { get; } = typeof (T).FullName;

    /// <summary>
    /// Hydrates the properties from the registry.
    /// </summary>
    public virtual void Load ()
    {
      ThreadHelper.JoinableTaskFactory.Run (LoadAsync);
    }

    /// <summary>
    /// Hydrates the properties from the registry asynchronously.
    /// </summary>
    public virtual async System.Threading.Tasks.Task LoadAsync ()
    {
      // TODO load from JSON via Facade
      ShellSettingsManager manager = await s_settingsManager.GetValueAsync();
      SettingsStore settingsStore = manager.GetReadOnlySettingsStore (SettingsScope.UserSettings);

      if (!settingsStore.CollectionExists (CollectionName))
      {
        return;
      }

      //foreach (PropertyInfo property in GetOptionProperties())
      //{
      //  try
      //  {
      //    string serializedProp = settingsStore.GetString (CollectionName, property.Name);
      //    object value = DeserializeValue (serializedProp, property.PropertyType);
      //    property.SetValue (this, value);
      //  }
      //  catch (Exception ex)
      //  {
      //    System.Diagnostics.Debug.Write (ex);
      //  }
      foreach (PropertyInfo property in GetOptionProperties())
      {
        if (typeof (IOptionsFacade).GetProperty (property.Name)?.PropertyType == property.PropertyType)
        {
          var facadeProperty = typeof (IOptionsFacade).GetProperty (property.Name);
          if (facadeProperty != null)
            property.SetValue (this, facadeProperty.GetValue (OptionsFacade.CurrentOptions));
        }
      }
    }

    /// <summary>
    /// Saves the properties to the registry.
    /// </summary>
    public virtual void Save ()
    {
      ThreadHelper.JoinableTaskFactory.Run (SaveAsync);
    }

    /// <summary>
    /// Saves the properties to the registry asynchronously.
    /// </summary>
    public virtual async System.Threading.Tasks.Task SaveAsync ()
    {
      // TODO load from JSON via Facade
      ShellSettingsManager manager = await s_settingsManager.GetValueAsync();
      WritableSettingsStore settingsStore = manager.GetWritableSettingsStore (SettingsScope.UserSettings);

      if (!settingsStore.CollectionExists (CollectionName))
      {
        settingsStore.CreateCollection (CollectionName);
      }

      foreach (PropertyInfo property in GetOptionProperties())
      {
        //string output = SerializeValue (property.GetValue (this));
        //settingsStore.SetString (CollectionName, property.Name, output);
        if (typeof (IOptionsFacade).GetProperty (property.Name)?.PropertyType == property.PropertyType)
        {
          var facadeProperty = typeof (IOptionsFacade).GetProperty (property.Name);
          facadeProperty?.SetValue (OptionsFacade.CurrentOptions, property.GetValue (this));
        }
      }

      T liveModel = await GetLiveInstanceAsync();

      if (this != liveModel)
      {
        await liveModel.LoadAsync();
      }
    }

    /// <summary>
    /// Serializes an object value to a string using the binary serializer.
    /// </summary>
    protected virtual string SerializeValue (object value)
    {
      using (var stream = new MemoryStream())
      {
        var formatter = new BinaryFormatter();
        formatter.Serialize (stream, value);
        stream.Flush();
        return Convert.ToBase64String (stream.ToArray());
      }
    }

    /// <summary>
    /// Deserializes a string to an object using the binary serializer.
    /// </summary>
    protected virtual object DeserializeValue (string value, Type type)
    {
      byte[] b = Convert.FromBase64String (value);

      using (var stream = new MemoryStream (b))
      {
        var formatter = new BinaryFormatter();
        return formatter.Deserialize (stream);
      }
    }

    private static async Task<ShellSettingsManager> GetSettingsManagerAsync ()
    {
#pragma warning disable VSTHRD010
      // False-positive in Threading Analyzers. Bug tracked here https://github.com/Microsoft/vs-threading/issues/230
      //var svc = await AsyncServiceProvider.GlobalProvider.GetServiceAsync (typeof (IServiceProvider)) as IServiceProvider;
#pragma warning restore VSTHRD010

      //Assumes.Present (svc);

      await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
      return new ShellSettingsManager (ServiceProvider.GlobalProvider);
    }

    private IEnumerable<PropertyInfo> GetOptionProperties ()
    {
      return GetType()
          .GetProperties()
          .Where (p => p.PropertyType.IsSerializable && p.PropertyType.IsPublic);
    }
  }
}