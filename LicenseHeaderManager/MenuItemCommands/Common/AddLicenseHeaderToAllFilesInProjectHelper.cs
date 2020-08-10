#region copyright

// Copyright (c) rubicon IT GmbH

// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
// FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE. 

#endregion

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Core;
using EnvDTE;
using LicenseHeaderManager.Headers;
using LicenseHeaderManager.ResultObjects;
using LicenseHeaderManager.Utils;
using LicenseHeaderReplacer = LicenseHeaderManager.Headers.LicenseHeaderReplacer;

namespace LicenseHeaderManager.MenuItemCommands.Common
{
  internal class AddLicenseHeaderToAllFilesInProjectHelper
  {
    private LicenseHeaderReplacer _licenseReplacer;
    private Core.LicenseHeaderReplacer _licenseHeaderReplacer;

    public AddLicenseHeaderToAllFilesInProjectHelper (
        LicenseHeaderReplacer licenseReplacer,
        Core.LicenseHeaderReplacer licenseHeaderReplacer)
    {
      _licenseHeaderReplacer = licenseHeaderReplacer;
      _licenseReplacer = licenseReplacer;
    }

    public AddLicenseHeaderToAllFilesResult Execute (object projectOrProjectItem)
    {
      var project = projectOrProjectItem as Project;
      var projectItem = projectOrProjectItem as ProjectItem;

      var countSubLicenseHeadersFound = 0;
      IDictionary<string, string[]> headers = null;
      var linkedItems = new List<ProjectItem>();

      if (project != null || projectItem != null)
      {
        _licenseReplacer.ResetExtensionsWithInvalidHeaders();
        ProjectItems projectItems;

        if (project != null)
        {
          headers = LicenseHeaderFinder.GetHeaderDefinitionForProjectWithFallback (project);
          projectItems = project.ProjectItems;
        }
        else
        {
          headers = LicenseHeaderFinder.GetHeaderDefinitionForItem (projectItem);
          projectItems = projectItem.ProjectItems;
        }

        foreach (ProjectItem item in projectItems)
        {
          if (ProjectItemInspection.IsPhysicalFile (item) && ProjectItemInspection.IsLink (item))
            linkedItems.Add (item);
          else
            countSubLicenseHeadersFound = _licenseReplacer.RemoveOrReplaceHeaderRecursive (item, headers);
        }
      }

      Execute1 (projectOrProjectItem);
      return new AddLicenseHeaderToAllFilesResult (countSubLicenseHeadersFound, headers == null, linkedItems);
    }

    public async Task<AddLicenseHeaderToAllFilesResult> Execute1 (object projectOrProjectItem)
    {
      var project = projectOrProjectItem as Project;
      var projectItem = projectOrProjectItem as ProjectItem;
      var files = new List<LicenseHeaderInput>();

      var countSubLicenseHeadersFound = 0;
      IDictionary<string, string[]> headers = null;
      var linkedItems = new List<ProjectItem>();

      if (project == null && projectItem == null)
        return new AddLicenseHeaderToAllFilesResult (countSubLicenseHeadersFound, headers == null, linkedItems);

      _licenseHeaderReplacer.ResetExtensionsWithInvalidHeaders();
      ProjectItems projectItems;

      if (project != null)
      {
        headers = LicenseHeaderFinder.GetHeaderDefinitionForProjectWithFallback (project);
        projectItems = project.ProjectItems;
      }
      else
      {
        headers = LicenseHeaderFinder.GetHeaderDefinitionForItem (projectItem);
        projectItems = projectItem.ProjectItems;
      }

      foreach (ProjectItem item in projectItems)
      {
        if (ProjectItemInspection.IsPhysicalFile (item) && ProjectItemInspection.IsLink (item))
        {
          linkedItems.Add (item);
        }
        else
        {
          files.AddRange (GetFilesToProcess (item, headers, out var subLicenseHeaders));
          countSubLicenseHeadersFound = subLicenseHeaders;
        }
      }

      var errors = await _licenseHeaderReplacer.RemoveOrReplaceHeader (files);
      if (errors.Count > 0)
        MessageBox.Show ($"Encountered {errors.Count} errors.", "Error(s)", MessageBoxButton.OK, MessageBoxImage.Error);

      return new AddLicenseHeaderToAllFilesResult (countSubLicenseHeadersFound, headers == null, linkedItems);
    }

    private IEnumerable<LicenseHeaderInput> GetFilesToProcess (
        ProjectItem item,
        IDictionary<string, string[]> headers,
        out int countSubLicenseHeaders,
        bool searchForLicenseHeaders = true)
    {
      var files = new List<LicenseHeaderInput>();
      countSubLicenseHeaders = 0;

      if (item.ProjectItems == null)
        return files;

      if (item.FileCount == 1 && System.IO.File.Exists(item.FileNames[1]))
      {
        files.Add (new LicenseHeaderInput (item.FileNames[1], headers, /* TODO */ null));
      }
      else if (item.Document != null && System.IO.File.Exists(item.Document.FullName))
      {
        files.Add (new LicenseHeaderInput (item.Document.FullName, headers, /* TODO */ null));
      }

      var childHeaders = headers;
      if (searchForLicenseHeaders)
      {
        childHeaders = LicenseHeaderFinder.SearchItemsDirectlyGetHeaderDefinition (item.ProjectItems);
        if (childHeaders != null)
          countSubLicenseHeaders++;
        else
          childHeaders = headers;
      }

      foreach (ProjectItem child in item.ProjectItems)
      {
        var subFiles = GetFilesToProcess (child, childHeaders, out var subLicenseHeaders, searchForLicenseHeaders);
        files.AddRange (subFiles);
        countSubLicenseHeaders += subLicenseHeaders;
      }

      return files;
    }
  }
}