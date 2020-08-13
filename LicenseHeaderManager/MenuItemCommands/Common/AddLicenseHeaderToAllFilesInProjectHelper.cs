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
using Core;
using EnvDTE;
using LicenseHeaderManager.Headers;
using LicenseHeaderManager.ResultObjects;
using LicenseHeaderManager.SolutionUpdateViewModels;
using LicenseHeaderManager.Utils;

namespace LicenseHeaderManager.MenuItemCommands.Common
{
  internal class AddLicenseHeaderToAllFilesInProjectHelper
  {
    private readonly LicenseHeaderReplacer _licenseHeaderReplacer;
    private readonly SolutionUpdateViewModel _solutionUpdateViewModel;

    public AddLicenseHeaderToAllFilesInProjectHelper (LicenseHeaderReplacer licenseHeaderReplacer, SolutionUpdateViewModel solutionUpdateViewModel = null)
    {
      _licenseHeaderReplacer = licenseHeaderReplacer;
      _solutionUpdateViewModel = solutionUpdateViewModel;
    }

    public async Task<AddLicenseHeaderToAllFilesResult> ExecuteAsync (object projectOrProjectItem)
    {
      var project = projectOrProjectItem as Project;
      var projectItem = projectOrProjectItem as ProjectItem;
      var replacerInput = new List<LicenseHeaderInput>();

      var countSubLicenseHeadersFound = 0;
      IDictionary<string, string[]> headers;
      var linkedItems = new List<ProjectItem>();

      if (project == null && projectItem == null)
        return new AddLicenseHeaderToAllFilesResult (countSubLicenseHeadersFound, true, linkedItems);

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
        if (ProjectItemInspection.IsPhysicalFile (item) && ProjectItemInspection.IsLink (item))
        {
          linkedItems.Add (item);
        }
        else
        {
          replacerInput.AddRange (CoreHelpers.GetFilesToProcess (item, headers, out var subLicenseHeaders));
          countSubLicenseHeadersFound = subLicenseHeaders;
        }

      var result = await _licenseHeaderReplacer.RemoveOrReplaceHeader (
          replacerInput,
          new Progress<ReplacerProgressReport> (report => CoreHelpers.OnProgressReportedAsync (report, _solutionUpdateViewModel).FireAndForget()),
          CoreHelpers.NonCommentLicenseHeaderDefinitionInquiry);
      CoreHelpers.HandleResult (result);

      return new AddLicenseHeaderToAllFilesResult (countSubLicenseHeadersFound, headers == null, linkedItems);
    }
  }
}