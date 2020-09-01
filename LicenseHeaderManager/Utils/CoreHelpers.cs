﻿/* Copyright (c) rubicon IT GmbH
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
using System.Runtime.InteropServices;
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

    private static async Task OnProgressReportedAsync (
        ReplacerProgressContentReport progress,
        BaseUpdateViewModel baseUpdateViewModel,
        string projectName,
        IDictionary<string, bool> fileOpenedStatus,
        CancellationToken cancellationToken)
    {
      await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
      
      if (!cancellationToken.IsCancellationRequested)
      {
        var result = new ReplacerResult<ReplacerSuccess, ReplacerError<LicenseHeaderContentInput>> (
            new ReplacerSuccess (progress.ProcessedFilePath, progress.ProcessFileNewContent));
        if (fileOpenedStatus.TryGetValue (progress.ProcessedFilePath, out var wasOpen))
          await HandleResultAsync (result, LicenseHeadersPackage.Instance, wasOpen, false);
        else
          await HandleResultAsync (result, LicenseHeadersPackage.Instance, false, false);
      }

      if (baseUpdateViewModel == null)
        return;

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

    public static IProgress<ReplacerProgressContentReport> CreateProgress (BaseUpdateViewModel viewModel, string projectName, IDictionary<string, bool> fileOpenedStatus, CancellationToken cancellationToken)
    {
      return new Progress<ReplacerProgressContentReport> (report => OnProgressReportedAsync (report, viewModel, projectName, fileOpenedStatus, cancellationToken).FireAndForget());
    }

    public static ICollection<LicenseHeaderContentInput> GetFilesToProcess (
        ProjectItem item,
        IDictionary<string, string[]> headers,
        out int countSubLicenseHeaders,
        out IDictionary<string, bool> fileOpenedStatus,
        bool searchForLicenseHeaders = true)
    {
      fileOpenedStatus = new Dictionary<string, bool>();
      var files = new List<LicenseHeaderContentInput>();
      countSubLicenseHeaders = 0;

      if (item.ProjectItems == null)
        return files;

      if (item.FileCount == 1 && File.Exists (item.FileNames[1]))
      {
        var content = item.GetContent (out var wasAlreadyOpen, LicenseHeadersPackage.Instance);
        if (content != null)
        {
          files.Add (new LicenseHeaderContentInput (content, item.FileNames[1], headers, item.GetAdditionalProperties()));
          fileOpenedStatus[item.FileNames[1]] = wasAlreadyOpen;
        }
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
        var subFiles = GetFilesToProcess (child, childHeaders, out var subLicenseHeaders, out var subFileOpenedStatus, searchForLicenseHeaders);

        files.AddRange (subFiles);
        foreach (var status in subFileOpenedStatus)
          fileOpenedStatus[status.Key] = status.Value;

        countSubLicenseHeaders += subLicenseHeaders;
      }

      return files;
    }

    public static async Task HandleResultAsync (
        ReplacerResult<ReplacerSuccess, ReplacerError<LicenseHeaderContentInput>> result,
        ILicenseHeaderExtension extension,
        bool isOpen,
        bool calledByUser)
    {
      if (result.IsSuccess)
      {
        if (!File.Exists (result.Success.FilePath) || TrySetContent (result.Success.FilePath, extension.Dte2.Solution, result.Success.NewContent, isOpen, extension))
          return;

        MessageBoxHelper.ShowError ($"Updating license header for file {result.Success.FilePath} failed.");
        s_log.Error ($"Updating license header for file {result.Success.FilePath} failed.");
        return;
      }

      if (!calledByUser)
        return;

      // TODO check for error after core call ignoring non comment text
      var error = result.Error;
      switch (error.Type)
      {
        case ReplacerErrorType.NonCommentText:
          error.Input.IgnoreNonCommentText = true;
          if (MessageBoxHelper.AskYesNo (error.Description, Resources.Warning, true))
            await extension.LicenseHeaderReplacer.RemoveOrReplaceHeader (error.Input);
          return;

        case ReplacerErrorType.LanguageNotFound:
          // TODO test with project with .snk file (e.g. DependDB.Util)...last attempt: works, but window closes immediately after showing (threading issue)
          if (MessageBoxHelper.AskYesNo (error.Description, Resources.Error))
            extension.ShowLanguagesPage();
          return;
      }

      MessageBoxHelper.ShowError ($"An unexpected error has occurred: {error.Description}");
      s_log.Error ($"File '{error.Input.DocumentPath}' failed: {error.Description}");
    }

    public static async Task HandleResultAsync (
        IEnumerable<ReplacerResult<ReplacerSuccess, ReplacerError<LicenseHeaderContentInput>>> result,
        ILicenseHeaderExtension licenseHeaderExtension,
        BaseUpdateViewModel viewModel,
        string projectName,
        IDictionary<string, bool> fileOpenedStatus,
        CancellationToken cancellationToken)
    {
      // collect NonCommentText-errors and ask if license header should still be inserted
      var errors = result.Where (replacerResult => !replacerResult.IsSuccess).Select (replacerResult => replacerResult.Error).ToList();

      var nonCommentTextErrorsByExtension = errors.Where (x => x.Type == ReplacerErrorType.NonCommentText).GroupBy (x => Path.GetExtension (x.Input.DocumentPath));

      var inputIgnoringNonCommentText = new List<LicenseHeaderContentInput>();
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
      var overallErrors = errors.Where (x => x.Type != ReplacerErrorType.NonCommentText).ToList();
      if (inputIgnoringNonCommentText.Count > 0)
      {
        viewModel.FileCountCurrentProject = inputIgnoringNonCommentText.Count;
        var resultIgnoringNonCommentText = await licenseHeaderExtension.LicenseHeaderReplacer.RemoveOrReplaceHeader (
            inputIgnoringNonCommentText,
            CreateProgress (viewModel, projectName, fileOpenedStatus, cancellationToken),
            cancellationToken);

        overallErrors.AddRange (resultIgnoringNonCommentText.Where (replacerResult => !replacerResult.IsSuccess).Select (replacerResult => replacerResult.Error));
      }

      // display all errors collected from "first attempt" and "force-insertion"
      if (overallErrors.Count == 0)
        return;

      MessageBoxHelper.ShowError ($"{overallErrors.Count} unexpected errors have occurred. See output window or log file for more details");
      foreach (var otherError in overallErrors)
        s_log.Error ($"File '{otherError.Input.DocumentPath}' failed: {otherError.Description}");
    }

    public static bool TrySetContent(string itemPath, Solution solution, string content, bool wasOpen, ILicenseHeaderExtension extension)
    {
      var item = solution.FindProjectItem(itemPath);
      if (item == null)
        return false;

      if (!wasOpen && !TryOpenDocument(item, extension))
        return false;

      // returning false from this method would signify an error, which we do not want since this circumstance is expected to occur with unknown file extensions
      var languageForExtension = extension.LicenseHeaderReplacer.GetLanguageFromExtension(Path.GetExtension(item.FileNames[1]));
      if (languageForExtension == null)
        return true;

      if (!(item.Document.Object("TextDocument") is TextDocument textDocument))
        return false;

      var wasSaved = item.Document.Saved;

      textDocument.CreateEditPoint(textDocument.StartPoint).Delete(textDocument.EndPoint);
      textDocument.CreateEditPoint(textDocument.StartPoint).Insert(content);

      SaveAndCloseIfNecessary(item, wasOpen, wasSaved);

      return true;
    }

    public static bool TryOpenDocument(ProjectItem item, ILicenseHeaderExtension extension)
    {
      try
      {
        // Opening files potentially having non-text content (.png, .snk) might result in a Visual Studio error "Some bytes have been replaced with the
        // Unicode substitution character while loading file ...". In order to avoid this, files with unknown extensions are not opened. However, in order
        // to keep such files eligible as Core input, still return true
        var languageForExtension = extension.LicenseHeaderReplacer.GetLanguageFromExtension(Path.GetExtension(item.FileNames[1]));
        if (languageForExtension == null)
          return true;

        item.Open(Constants.vsViewKindTextView);
        return true;
      }
      catch (COMException)
      {
        return false;
      }
      catch (IOException)
      {
        return false;
      }
    }

    public static void SaveAndCloseIfNecessary(ProjectItem item, bool wasOpen, bool wasSaved)
    {
      if (wasOpen)
      {
        // if document had no unsaved changes before, it should not have any now (analogously for when it did have unsaved changes)
        if (wasSaved)
          item.Document.Save();
      }
      else
      {
        item.Document.Close(vsSaveChanges.vsSaveChangesYes);
      }
    }
  }
}