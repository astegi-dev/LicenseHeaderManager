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
using System.IO;
using System.Linq;
using Core;
using EnvDTE;
using LicenseHeaderManager.Headers;
using LicenseHeaderManager.Interfaces;
using LicenseHeaderManager.UpdateViewModels;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace LicenseHeaderManager.Utils
{
  internal static class CoreHelpers
  {
    public static async Task OnProgressReportedAsync (ReplacerProgressReport progress, BaseUpdateViewModel baseUpdateViewModel, string projectName)
    {
      if (baseUpdateViewModel == null)
        return;

      await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
      baseUpdateViewModel.FileCountCurrentProject = progress.TotalFileCount;

      // IProgress relies on SynchronizationContext. Thus, in the current architecture, OnProgressReportAsync callbacks are not always guaranteed to be executed
      // in the same order as they are reported from the Core (especially if reports happen at the same time). Countering that, the ProgressBar value is updated
      // only if reported value is higher than last one (or on reset). The resulting drawback that some progress "steps" might be skipped is negligible and not
      // detrimental to the user feedback (for instance, it happens if a few Core threads being responsible for small files finish at roughly the same time).
      // if (progress.ProcessedFileCount <= 1 || progress.ProcessedFileCount > baseUpdateViewModel.ProcessedFilesCountCurrentProject)
      baseUpdateViewModel.ProcessedFilesCountCurrentProject = progress.ProcessedFileCount;

      if (baseUpdateViewModel is SolutionUpdateViewModel solutionUpdateViewModel)
        solutionUpdateViewModel.CurrentProject = projectName;
    }

    /// <summary>
    ///   Is executed when the Core reports that the license header definition file to be used on a specific file contains
    ///   content that is not recognized as comments for the respective language.
    /// </summary>
    /// <param name="message">Specific message reported by core.</param>
    /// <returns>True if the license header should still be inserted, otherwise false.</returns>
    public static bool NonCommentLicenseHeaderDefinitionInquiry (string message)
    {
      return MessageBoxHelper.AskYesNo (message, Resources.Warning, true);
    }

    /// <summary>
    ///   Is executed when the Core reports that for given files (i. e. languages), no license header definition(s) could be
    ///   found.
    /// </summary>
    /// <param name="message">
    ///   Specific message reported by core, contains number of files for which no license header
    ///   definition could be found.
    /// </param>
    /// <param name="licenseHeaderExtension">
    ///   An Instance of <see cref="ILicenseHeaderExtension" /> used to display the languages page, which might
    ///   be used to add the languages for which no definitions were found to the configuration.
    /// </param>
    public static void NoLicenseHeaderDefinitionFound (string message, ILicenseHeaderExtension licenseHeaderExtension)
    {
      if (MessageBoxHelper.AskYesNo (message, Resources.Error))
        licenseHeaderExtension.ShowLanguagesPage();
    }

    public static ICollection<LicenseHeaderInput> GetFilesToProcess (
        ProjectItem item,
        IDictionary<string, string[]> headers,
        out int countSubLicenseHeaders,
        bool searchForLicenseHeaders = true)
    {
      var files = new List<LicenseHeaderInput>();
      countSubLicenseHeaders = 0;

      if (item.ProjectItems == null)
        return files;

      if (item.FileCount == 1 && File.Exists (item.FileNames[1]))
        files.Add (new LicenseHeaderInput (item.FileNames[1], headers, item.GetAdditionalProperties()));

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

    public static void HandleResult (ReplacerResult<ReplacerError> result)
    {
      if (!result.IsSuccess)
        MessageBoxHelper.ShowError ($"Error: {result.Error.Description}");
    }

    public static void HandleResult (ReplacerResult<IEnumerable<ReplacerError>> result)
    {
      if (!result.IsSuccess)
        MessageBoxHelper.ShowError ($"Encountered errors in {result.Error.Count()} files");
    }
  }
}