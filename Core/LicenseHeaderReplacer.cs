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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Core.Properties;

namespace Core
{
  public class LicenseHeaderReplacer
  {
    /// <summary>
    ///   Used to keep track of the user selection when he is trying to insert invalid headers into all files,
    ///   so that the warning is only displayed once per file extension.
    /// </summary>
    private readonly IDictionary<string, bool> _extensionsWithInvalidHeaders = new Dictionary<string, bool>();

    private readonly IEnumerable<string> _keywords;
    private readonly IEnumerable<Language> _languages;
    private readonly SemaphoreSlim _progressReportSemaphore;
    private readonly SemaphoreSlim _taskStartSemaphore;

    private int _processedFileCount;
    private int _totalFileCount;

    public LicenseHeaderReplacer (IEnumerable<Language> languages, IEnumerable<string> keywords, int maxSimultaneousTasks = 15)
    {
      _languages = languages;
      _keywords = keywords;

      _progressReportSemaphore = new SemaphoreSlim (1, 1);
      _taskStartSemaphore = new SemaphoreSlim (maxSimultaneousTasks, maxSimultaneousTasks);
    }

    public void ResetExtensionsWithInvalidHeaders ()
    {
      _extensionsWithInvalidHeaders.Clear();
    }

    /// <summary>
    ///   Tries to open a given project item as a Document which can be used to add or remove headers.
    /// </summary>
    /// <param name="documentPath">The project item.</param>
    /// <param name="document">The document which was created or null if an error occurred (see return value).</param>
    /// <param name="additionalProperties"></param>
    /// <param name="headers">
    ///   A dictionary of headers using the file extension as key and the header as value or null if
    ///   headers should only be removed.
    /// </param>
    /// <returns>A value indicating the result of the operation. Document will be null unless DocumentCreated is returned.</returns>
    public CreateDocumentResult TryCreateDocument (LicenseHeaderInput licenseHeaderInput, out Document document)
    {
      document = null;

      if (licenseHeaderInput.Extension == LicenseHeader.Extension)
        return CreateDocumentResult.LicenseHeaderDocument;

      var language = _languages.FirstOrDefault (x => x.Extensions.Any (y => licenseHeaderInput.Extension.EndsWith (y, StringComparison.OrdinalIgnoreCase)));

      if (language == null)
        return CreateDocumentResult.LanguageNotFound;

      string[] header = null;
      if (licenseHeaderInput.Headers != null)
      {
        var extension = licenseHeaderInput.Headers.Keys
            .OrderByDescending (x => x.Length)
            .FirstOrDefault (x => licenseHeaderInput.Extension.EndsWith (x, StringComparison.OrdinalIgnoreCase));

        if (extension == null)
          return CreateDocumentResult.NoHeaderFound;

        header = licenseHeaderInput.Headers[extension];

        if (header.All (string.IsNullOrEmpty))
          return CreateDocumentResult.EmptyHeader;
      }

      document = new Document (licenseHeaderInput, language, header, licenseHeaderInput.AdditionalProperties, _keywords);

      return CreateDocumentResult.DocumentCreated;
    }

    public async Task<ReplacerResult<string, ReplacerError<LicenseHeaderContentInput>>> RemoveOrReplaceHeader (
        LicenseHeaderContentInput licenseHeaderInput,
        bool calledByUser)
    {
      try
      {
        var result = TryCreateDocument (licenseHeaderInput, out var document);

        string message;
        switch (result)
        {
          case CreateDocumentResult.DocumentCreated:
            if (!await document.ValidateHeader() && !licenseHeaderInput.IgnoreNonCommentText)
            {
              message = string.Format (Resources.Warning_InvalidLicenseHeader, Path.GetExtension (licenseHeaderInput.DocumentPath)).ReplaceNewLines();
              return new ReplacerResult<string, ReplacerError<LicenseHeaderContentInput>> (
                  new ReplacerError<LicenseHeaderContentInput> (licenseHeaderInput, calledByUser, ReplacerErrorType.NonCommentText, message));
            }

            try
            {
              var newContent = await document.ReplaceHeaderIfNecessaryContent (new CancellationToken());
              return new ReplacerResult<string, ReplacerError<LicenseHeaderContentInput>> (newContent);
            }
            catch (ParseException)
            {
              message = string.Format (Resources.Error_InvalidLicenseHeader, licenseHeaderInput.DocumentPath).ReplaceNewLines();
              return new ReplacerResult<string, ReplacerError<LicenseHeaderContentInput>> (
                  new ReplacerError<LicenseHeaderContentInput> (licenseHeaderInput, calledByUser, ReplacerErrorType.ParsingError, message));
            }

          case CreateDocumentResult.LanguageNotFound:
            if (calledByUser)
            {
              message = string.Format (Resources.Error_LanguageNotFound, Path.GetExtension (licenseHeaderInput.DocumentPath)).ReplaceNewLines();

              // TODO test with project with .snk file (e.g. DependDB.Util)...last attempt: works, but window closes immediately after showing (threading issue)
              return new ReplacerResult<string, ReplacerError<LicenseHeaderContentInput>> (
                  new ReplacerError<LicenseHeaderContentInput> (licenseHeaderInput, true, ReplacerErrorType.LanguageNotFound, message));
            }

            break;
          case CreateDocumentResult.EmptyHeader:
            message = string.Format (Resources.Error_HeaderNullOrEmpty, licenseHeaderInput.Extension);
            return new ReplacerResult<string, ReplacerError<LicenseHeaderContentInput>> (
                new ReplacerError<LicenseHeaderContentInput> (licenseHeaderInput, calledByUser, ReplacerErrorType.EmptyHeader, message));
          case CreateDocumentResult.NoHeaderFound:
            if (calledByUser)
            {
              message = string.Format (Resources.Error_NoHeaderFound).ReplaceNewLines();
              return new ReplacerResult<string, ReplacerError<LicenseHeaderContentInput>> (
                  new ReplacerError<LicenseHeaderContentInput> (licenseHeaderInput, true, ReplacerErrorType.NoHeaderFound, message));
            }

            break;
        }
      }
      catch (ArgumentException ex)
      {
        var message = $"{ex.Message} {licenseHeaderInput.DocumentPath}";
        return new ReplacerResult<string, ReplacerError<LicenseHeaderContentInput>> (
            new ReplacerError<LicenseHeaderContentInput> (licenseHeaderInput, calledByUser, ReplacerErrorType.Miscellaneous, message));
      }

      return new ReplacerResult<string, ReplacerError<LicenseHeaderContentInput>> (
          new ReplacerError<LicenseHeaderContentInput> (licenseHeaderInput, ReplacerErrorType.Miscellaneous, "An unexpected Error occurred"));
    }

    /// <summary>
    ///   Removes or replaces the header of a given project item.
    /// </summary>
    /// <param name="licenseHeaderInput">The licenseHeaderInput item.</param>
    /// <param name="calledByUser">
    ///   Specifies whether the command was called by the user (as opposed to automatically by a
    ///   linked command or by ItemAdded)
    /// </param>
    public async Task<ReplacerResult<ReplacerError<LicenseHeaderPathInput>>> RemoveOrReplaceHeader (LicenseHeaderPathInput licenseHeaderInput, bool calledByUser)
    {
      var returnObject = new ReplacerResult<ReplacerError<LicenseHeaderPathInput>>();
      try
      {
        var result = TryCreateDocument (licenseHeaderInput, out var document);

        switch (result)
        {
          case CreateDocumentResult.DocumentCreated:
            if (!await document.ValidateHeader() && !licenseHeaderInput.IgnoreNonCommentText)
            {
              var message = string.Format (Resources.Warning_InvalidLicenseHeader, Path.GetExtension (licenseHeaderInput.DocumentPath)).ReplaceNewLines();
              returnObject = new ReplacerResult<ReplacerError<LicenseHeaderPathInput>> (
                  new ReplacerError<LicenseHeaderPathInput> (licenseHeaderInput, calledByUser, ReplacerErrorType.NonCommentText, message));
              break;
            }

            try
            {
              await document.ReplaceHeaderIfNecessaryPath (new CancellationToken());
            }
            catch (ParseException)
            {
              var message = string.Format (Resources.Error_InvalidLicenseHeader, licenseHeaderInput.DocumentPath).ReplaceNewLines();
              returnObject = new ReplacerResult<ReplacerError<LicenseHeaderPathInput>> (
                  new ReplacerError<LicenseHeaderPathInput> (licenseHeaderInput, calledByUser, ReplacerErrorType.ParsingError, message));
            }

            break;
          case CreateDocumentResult.LanguageNotFound:
            if (calledByUser)
            {
              var message = string.Format (Resources.Error_LanguageNotFound, Path.GetExtension (licenseHeaderInput.DocumentPath)).ReplaceNewLines();

              // TODO test with project with .snk file (e.g. DependDB.Util)...last attempt: works, but window closes immediately after showing (threading issue)
              returnObject = new ReplacerResult<ReplacerError<LicenseHeaderPathInput>> (
                  new ReplacerError<LicenseHeaderPathInput> (licenseHeaderInput, true, ReplacerErrorType.LanguageNotFound, message));
            }

            break;
          case CreateDocumentResult.EmptyHeader:
            break;
          case CreateDocumentResult.NoHeaderFound:
            if (calledByUser)
            {
              var message = string.Format (Resources.Error_NoHeaderFound).ReplaceNewLines();
              returnObject = new ReplacerResult<ReplacerError<LicenseHeaderPathInput>> (
                  new ReplacerError<LicenseHeaderPathInput> (licenseHeaderInput, true, ReplacerErrorType.NoHeaderFound, message));
            }

            break;
        }
      }
      catch (ArgumentException ex)
      {
        var message = $"{ex.Message} {licenseHeaderInput.DocumentPath}";
        returnObject = new ReplacerResult<ReplacerError<LicenseHeaderPathInput>> (
            new ReplacerError<LicenseHeaderPathInput> (licenseHeaderInput, calledByUser, ReplacerErrorType.Miscellaneous, message));
      }

      return returnObject;
    }

    public async Task<IEnumerable<ReplacerResult<ReplacerSuccess, ReplacerError<LicenseHeaderContentInput>>>> RemoveOrReplaceHeader (
        ICollection<LicenseHeaderContentInput> licenseHeaders,
        IProgress<ReplacerProgressReport> progress,
        CancellationToken cancellationToken)
    {
      var results = new List<ReplacerResult<ReplacerSuccess, ReplacerError<LicenseHeaderContentInput>>>();
      ResetProgress (licenseHeaders.Count);

      var tasks = new List<Task<ReplacerResult<ReplacerSuccess, ReplacerError<LicenseHeaderContentInput>>>>();
      foreach (var licenseHeaderInput in licenseHeaders)
      {
        await _taskStartSemaphore.WaitAsync (cancellationToken);
        tasks.Add (
            Task.Run (
                () =>
                {
                  try
                  {
                    return RemoveOrReplaceHeaderForOneFile (licenseHeaderInput, progress, cancellationToken);
                  }
                  finally
                  {
                    _taskStartSemaphore.Release();
                  }
                },
                cancellationToken));
      }

      await Task.WhenAll (tasks);

      foreach (var replacerResultTask in tasks)
      {
        var replacerResult = await replacerResultTask;
        if (replacerResult == null)
          continue;

        results.Add (replacerResult);
      }

      return results;
    }

    public async Task<ReplacerResult<IEnumerable<ReplacerError<LicenseHeaderPathInput>>>> RemoveOrReplaceHeader (
        ICollection<LicenseHeaderPathInput> licenseHeaders,
        IProgress<ReplacerProgressReport> progress,
        CancellationToken cancellationToken)
    {
      var errorList = new ConcurrentQueue<ReplacerError<LicenseHeaderPathInput>>();
      ResetProgress (licenseHeaders.Count);

      var tasks = new List<Task>();
      foreach (var licenseHeaderInput in licenseHeaders)
      {
        await _taskStartSemaphore.WaitAsync (cancellationToken);
        tasks.Add (
            Task.Run (
                () =>
                {
                  try
                  {
                    return RemoveOrReplaceHeaderForOneFile (licenseHeaderInput, progress, cancellationToken, errorList);
                  }
                  finally
                  {
                    _taskStartSemaphore.Release();
                  }
                },
                cancellationToken));
      }

      await Task.WhenAll (tasks);

      return errorList.Count == 0
          ? new ReplacerResult<IEnumerable<ReplacerError<LicenseHeaderPathInput>>>()
          : new ReplacerResult<IEnumerable<ReplacerError<LicenseHeaderPathInput>>> (errorList);
    }

    private async Task<ReplacerResult<ReplacerSuccess, ReplacerError<LicenseHeaderContentInput>>> RemoveOrReplaceHeaderForOneFile (
        LicenseHeaderContentInput licenseHeaderInput,
        IProgress<ReplacerProgressReport> progress,
        CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();
      if (TryCreateDocument (licenseHeaderInput, out var document) != CreateDocumentResult.DocumentCreated)
      {
        await ReportProgress (progress, cancellationToken);
        return null;
      }

      string message;
      if (!await document.ValidateHeader() && !licenseHeaderInput.IgnoreNonCommentText)
      {
        cancellationToken.ThrowIfCancellationRequested();
        await ReportProgress (progress, cancellationToken);

        var extension = Path.GetExtension (licenseHeaderInput.DocumentPath);
        message = string.Format (Resources.Warning_InvalidLicenseHeader, extension).ReplaceNewLines();
        return new ReplacerResult<ReplacerSuccess, ReplacerError<LicenseHeaderContentInput>> (
            new ReplacerError<LicenseHeaderContentInput> (licenseHeaderInput, ReplacerErrorType.NonCommentText, message));
      }

      try
      {
        cancellationToken.ThrowIfCancellationRequested();
        var newContent = await document.ReplaceHeaderIfNecessaryContent (cancellationToken);
        await ReportProgress (progress, cancellationToken);
        return new ReplacerResult<ReplacerSuccess, ReplacerError<LicenseHeaderContentInput>> (new ReplacerSuccess (licenseHeaderInput.DocumentPath, newContent));
      }
      catch (ParseException)
      {
        message = string.Format (Resources.Error_InvalidLicenseHeader, licenseHeaderInput.DocumentPath).ReplaceNewLines();
        return new ReplacerResult<ReplacerSuccess, ReplacerError<LicenseHeaderContentInput>> (
            new ReplacerError<LicenseHeaderContentInput> (licenseHeaderInput, ReplacerErrorType.ParsingError, message));
      }
    }

    private async Task RemoveOrReplaceHeaderForOneFile (
        LicenseHeaderPathInput licenseHeaderInput,
        IProgress<ReplacerProgressReport> progress,
        CancellationToken cancellationToken,
        ConcurrentQueue<ReplacerError<LicenseHeaderPathInput>> errors)
    {
      cancellationToken.ThrowIfCancellationRequested();
      if (TryCreateDocument (licenseHeaderInput, out var document) != CreateDocumentResult.DocumentCreated)
      {
        await ReportProgress (progress, cancellationToken);
        return;
      }

      string message;
      if (!await document.ValidateHeader() && !licenseHeaderInput.IgnoreNonCommentText)
      {
        var extension = Path.GetExtension (licenseHeaderInput.DocumentPath);
        message = string.Format (Resources.Warning_InvalidLicenseHeader, extension).ReplaceNewLines();
        errors.Enqueue (new ReplacerError<LicenseHeaderPathInput> (licenseHeaderInput, ReplacerErrorType.NonCommentText, message));

        cancellationToken.ThrowIfCancellationRequested();
        await ReportProgress (progress, cancellationToken);
        return;
      }

      try
      {
        cancellationToken.ThrowIfCancellationRequested();
        await document.ReplaceHeaderIfNecessaryPath (cancellationToken);
      }
      catch (ParseException)
      {
        message = string.Format (Resources.Error_InvalidLicenseHeader, licenseHeaderInput.DocumentPath).ReplaceNewLines();
        errors.Enqueue (new ReplacerError<LicenseHeaderPathInput> (licenseHeaderInput, ReplacerErrorType.ParsingError, message));
      }

      await ReportProgress (progress, cancellationToken);
    }

    private void ResetProgress (int totalFileCount)
    {
      _processedFileCount = 0;
      _totalFileCount = totalFileCount;
    }

    private async Task ReportProgress (IProgress<ReplacerProgressReport> progress, CancellationToken cancellationToken)
    {
      await _progressReportSemaphore.WaitAsync (cancellationToken);
      try
      {
        _processedFileCount++;
        progress.Report (new ReplacerProgressReport (_totalFileCount, _processedFileCount));
      }
      finally
      {
        _progressReportSemaphore.Release();
      }
    }
  }
}