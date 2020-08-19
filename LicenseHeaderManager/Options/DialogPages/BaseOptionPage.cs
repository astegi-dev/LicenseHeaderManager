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
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using LicenseHeaderManager.Options.Converters;
using LicenseHeaderManager.Options.Model;
using LicenseHeaderManager.Utils;
using Microsoft.VisualStudio.Shell;
using Microsoft.Win32;

namespace LicenseHeaderManager.Options.DialogPages
{
  /// <summary>
  /// A base class for a DialogPage to show in Tools -> Options.
  /// </summary>
  public class BaseOptionPage<T> : DialogPage
      where T : BaseOptionModel<T>, new()
  {
    protected readonly BaseOptionModel<T> Model;

    private static bool s_firstDialogPageLoaded = true;

    /// <summary>
    ///  Serialized properties.
    ///  Is managed by VisualStudio and placed persistently in the Registry
    /// </summary>
    public string Version { get; set; }

    public BaseOptionPage ()
    {
#pragma warning disable VSTHRD104 // Offer async methods
      Model = ThreadHelper.JoinableTaskFactory.Run (BaseOptionModel<T>.CreateAsync);
#pragma warning restore VSTHRD104 // Offer async methods
    }

    public override object AutomationObject => Model;

    public override void LoadSettingsFromStorage ()
    {
      Model.Load();

      //Could happen if you install a LicenseHeaderManager (LHM) version which is older than the ever installed highest version
      //Should only happen to developers of LHM, but could theoretically also happen if someone downgrades LHM.
      if (GetParsedRegistryVersion() > GetCurrentlyInstalledVersion())
      {
        if (s_firstDialogPageLoaded)
        {
          MessageBoxHelper.ShowMessage(
              "We detected that you are downgrading LicenseHeaderManager from a higher version." + Environment.NewLine +
              "As we don't know what you did to get to that state, it is possible that you missed an update for the Language Settings."
              + Environment.NewLine +
              "If some of your license headers do not update, check if your Language Settings (Options -> LicenseHeaderManager -> Languages) "
              + Environment.NewLine +
              "contain all the extensions you require.");

          s_firstDialogPageLoaded = false;
        }

        Version = LicenseHeadersPackage.Version;
        Model.Save();
      }
      else
      {
        var saveRequired = false;

        foreach (var updateStep in GetVersionUpdateSteps())
          saveRequired |= Update(updateStep);

        if (Version != LicenseHeadersPackage.Version)
          saveRequired |= Update(new UpdateStep(GetCurrentlyInstalledVersion()));

        if (saveRequired)
          Model.Save();
      }
    }

    public override void SaveSettingsToStorage ()
    {
      Model.Save();
    }

    protected virtual IEnumerable<UpdateStep> GetVersionUpdateSteps()
    {
      return Enumerable.Empty<UpdateStep>();
    }

    private bool Update(UpdateStep updateStep)
    {
      var registryVersion = GetParsedRegistryVersion();
      if (registryVersion >= updateStep.TargetVersion)
        return false;

      updateStep.ExecuteActions();

      Version = updateStep.TargetVersion.ToString();
      return true;
    }

    private Version GetParsedRegistryVersion ()
    {
      System.Version.TryParse (Version, out var result);
      return result;
    }

    private Version GetCurrentlyInstalledVersion ()
    {
      return System.Version.Parse (LicenseHeadersPackage.Version);
    }

    #region migration to 3.0.1

    protected void LoadRegistryValuesBefore_3_0_0(DialogPage dialogPage = null)
    {
      using var key = GetOldRegistryKey();
      foreach (var property in GetVisibleProperties())
      {
        var converter = GetPropertyConverterOrDefault(property);
        var registryValue = GetRegistryValue(key, property.Name);

        if (registryValue != null)
          try
          {
            property.SetValue(dialogPage ?? AutomationObject, DeserializeValue(converter, registryValue));
          }
          catch (Exception)
          {
            OutputWindowHandler.WriteMessage($"Could not restore registry value for {property.Name}");
          }
      }
    }

    private RegistryKey GetOldRegistryKey()
    {
      var oldSettingsRegistryPath = $"DialogPage\\LicenseHeaderManager.Options.{GetType().Name}";
      var service = (AsyncPackage)GetService(typeof(AsyncPackage));
      return service?.UserRegistryRoot.OpenSubKey(oldSettingsRegistryPath);
    }

    private IEnumerable<PropertyDescriptor> GetVisibleProperties()
    {
      return TypeDescriptor.GetProperties(AutomationObject)
          .Cast<PropertyDescriptor>();
    }

    private TypeConverter GetPropertyConverterOrDefault(PropertyDescriptor propertyDescriptor)
    {
      if (propertyDescriptor.Name == nameof(LanguagesPageModelOld.Languages))
        return new LanguageConverter();

      if (propertyDescriptor.Name == nameof(GeneralOptionsPageModelOld.LinkedCommands))
        return new LinkedCommandConverter();

      return propertyDescriptor.Converter;
    }

    private string GetRegistryValue(RegistryKey key, string subKeyName)
    {
      return key?.GetValue(subKeyName)?.ToString();
    }

    private object DeserializeValue(TypeConverter converter, string value)
    {
      return converter.ConvertFromInvariantString(value);
    }

    protected U ThreeWaySelectionForMigration<U>(U currentValue, U migratedValue, U defaultValue)
    {
      if (defaultValue is ICollection)
        throw new InvalidOperationException("ThreeWaySelectionForMigration does currently not support ICollections.");

      if (currentValue.Equals(defaultValue))
        return migratedValue;

      return currentValue;
    }

    #endregion

  }
}