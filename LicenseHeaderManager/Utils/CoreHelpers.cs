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
using System.Reflection;
using System.Threading;
using Core;
using EnvDTE;
using LicenseHeaderManager.Headers;
using LicenseHeaderManager.Interfaces;
using LicenseHeaderManager.UpdateViewModels;
using log4net;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace LicenseHeaderManager.Utils
{
  internal static class CoreHelpers
  {
    private static readonly ILog s_log = LogManager.GetLogger (MethodBase.GetCurrentMethod().DeclaringType);

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
      if (progress.ProcessedFileCount <= 1 || progress.ProcessedFileCount > baseUpdateViewModel.ProcessedFilesCountCurrentProject)
        baseUpdateViewModel.ProcessedFilesCountCurrentProject = progress.ProcessedFileCount;

      if (baseUpdateViewModel is SolutionUpdateViewModel solutionUpdateViewModel)
        solutionUpdateViewModel.CurrentProject = projectName;
    }

    public static IProgress<ReplacerProgressReport> CreateProgress (BaseUpdateViewModel viewModel, string projectName)
    {
      return new Progress<ReplacerProgressReport> (report => OnProgressReportedAsync (report, viewModel, projectName).FireAndForget());
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

    public static async Task HandleResultAsync (ReplacerResult<ReplacerError> result, ILicenseHeaderExtension extension)
    {
      if (result.IsSuccess)
        return;

      var error = result.Error;
      switch (error.Type)
      {
        case ErrorType.NonCommentText:
          error.Input.IgnoreNonCommentText = true;
          if (MessageBoxHelper.AskYesNo (error.Description, Resources.Warning, true))
            await extension.LicenseHeaderReplacer.RemoveOrReplaceHeader (error.Input, error.CalledByUser);
          return;

        case ErrorType.LanguageNotFound:
          if (MessageBoxHelper.AskYesNo (error.Description, Resources.Error))
            extension.ShowLanguagesPage();
          return;
      }

      MessageBoxHelper.ShowError ($"An unexpected error has occurred: {error.Description}");
      s_log.Error ($"File '{error.Input.DocumentPath}' failed: {error.Description}");
    }

    public static async Task HandleResultAsync (
        ReplacerResult<IEnumerable<ReplacerError>> result,
        LicenseHeaderReplacer replacer,
        BaseUpdateViewModel viewModel,
        string projectName,
        CancellationToken cancellationToken)
    {
      if (result.IsSuccess)
        return;


      // collect NonCommentText-errors and ask if license header should still be inserted
      var errors = result.Error.ToList();
      var nonCommentTextErrorsByExtension = errors.Where (x => x.Type == ErrorType.NonCommentText).GroupBy (x => Path.GetExtension (x.Input.DocumentPath));

      var inputIgnoringNonCommentText = new List<LicenseHeaderInput>();
      foreach (var extension in nonCommentTextErrorsByExtension)
      {
        var message = string.Format (Resources.Warning_InvalidLicenseHeader, extension.Key).ReplaceNewLines();
        if (!MessageBoxHelper.AskYesNo (message, Resources.Warning, true))
          continue;

        foreach (var failedFile in extension)
        {
          failedFile.Input.IgnoreNonCommentText = true;
          inputIgnoringNonCommentText.Add (failedFile.Input);
        }
      }

      // collect other errors and the ones that occurred while "force-inserting" headers with non-comment-text
      var overallErrors = errors.Where (x => x.Type != ErrorType.NonCommentText).ToList();
      if (inputIgnoringNonCommentText.Count > 0)
      {
        viewModel.FileCountCurrentProject = inputIgnoringNonCommentText.Count;
        var resultIgnoringNonCommentText = await replacer.RemoveOrReplaceHeader (inputIgnoringNonCommentText, CreateProgress (viewModel, projectName), cancellationToken);

        if (!resultIgnoringNonCommentText.IsSuccess)
          overallErrors.AddRange (resultIgnoringNonCommentText.Error);
      }

      // display all errors collected from "first attempt" and "force-insertion"
      if (overallErrors.Count == 0)
        return;

      MessageBoxHelper.ShowError ($"{overallErrors.Count} unexpected errors have occurred. See output window or log file for more details");
      foreach (var otherError in overallErrors)
        s_log.Error ($"File '{otherError.Input.DocumentPath}' failed: {otherError.Description}");
    }
  }
}