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
using System.Threading;
using System.Threading.Tasks;
using Core.Properties;

namespace Core
{
  /// <summary>
  ///   Updates (i. e. replaces, adds, removes) license headers from files.
  /// </summary>
  public class LicenseHeaderReplacer
  {
    /// <summary>
    ///   The file extension of License Header Definition files.
    /// </summary>
    public const string HeaderDefinitionExtension = ".licenseheader";

    /// <summary>
    ///   The text representing the start of the listing of extensions belonging to one license header definition within a
    ///   license header definition file.
    /// </summary>
    public const string ExtensionKeyword = "extensions:";

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

    public Language GetLanguageFromExtension (string extension)
    {
      return _languages.FirstOrDefault (x => x.Extensions.Any (y => extension.EndsWith (y, StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>
    ///   Determines whether a file is a generally valid input file for
    ///   <see cref="RemoveOrReplaceHeader(LicenseHeaderPathInput)" /> or
    ///   <see
    ///     cref="RemoveOrReplaceHeader(ICollection{LicenseHeaderPathInput},IProgress{ReplacerProgressReport},CancellationToken)" />
    ///   .
    /// </summary>
    /// <param name="path">The path to the file to be examined.</param>
    /// <returns>
    ///   Returns <see langword="true" /> if the file specified by <paramref name="path" /> is a valid path input,
    ///   otherwise false.
    /// </returns>
    /// <remarks>
    ///   A return value of <see langword="true" /> does not necessarily mean that an invocation of
    ///   <see cref="RemoveOrReplaceHeader(LicenseHeaderPathInput)" /> or
    ///   <see
    ///     cref="RemoveOrReplaceHeader(ICollection{LicenseHeaderPathInput},IProgress{ReplacerProgressReport},CancellationToken)" />
    ///   would not yield a <see cref="ReplacerErrorType.NonCommentText" /> or <see cref="ReplacerErrorType.ParsingError" />
    ///   error, but only that is a fundamentally valid input - i. e. the file exists and this
    ///   <see cref="LicenseHeaderReplacer" /> instance is able to interpret it in the way required.
    /// </remarks>
    public bool IsValidPathInput (string path)
    {
      return File.Exists (path) && TryCreateDocument (new LicenseHeaderPathInput (path, null), out _) == CreateDocumentResult.DocumentCreated;
    }

    /// <summary>
    ///   Tries to open a given project item as a Document which can be used to add or remove headers.
    /// </summary>
    /// <param name="licenseHeaderInput">A <see cref="LicenseHeaderInput" /> instance representing the document to be opened.</param>
    /// <param name="document">
    ///   In case of a success, i. e. return value of <see cref="CreateDocumentResult.DocumentCreated" />,
    ///   this parameter represents the <see cref="Document" /> instance that was created in the process.Otherwise
    ///   <see langword="null" />.
    /// </param>
    /// <returns>
    ///   Returns a <see cref="CreateDocumentResult" /> member describing the success status of the document opening
    ///   attempt.
    /// </returns>
    private CreateDocumentResult TryCreateDocument (LicenseHeaderInput licenseHeaderInput, out Document document)
    {
      document = null;

      if (licenseHeaderInput.Extension == HeaderDefinitionExtension)
        return CreateDocumentResult.LicenseHeaderDocument;

      var language = GetLanguageFromExtension (licenseHeaderInput.Extension);
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

    /// <summary>
    ///   Updates license headers in a file based on its content, i. e. the new content is returned. Setting the new content is
    ///   the caller's responsibility.
    /// </summary>
    /// <param name="licenseHeaderInput">
    ///   An <see cref="LicenseHeaderContentInput" /> instance representing the file whose
    ///   license headers are to be updated.
    /// </param>
    /// <returns>
    ///   Returns a <see cref="ReplacerResult{TSuccess,TError}" /> instance containing information about the success or
    ///   error of the operation.
    /// </returns>
    public async Task<ReplacerResult<ReplacerSuccess, ReplacerError<LicenseHeaderContentInput>>> RemoveOrReplaceHeader (LicenseHeaderContentInput licenseHeaderInput)
    {
      return await RemoveOrReplaceHeader (
          licenseHeaderInput,
          async (input, document) =>
          {
            var newContent = await document.ReplaceHeaderIfNecessaryContent (new CancellationToken());
            return new ReplacerResult<ReplacerSuccess, ReplacerError<LicenseHeaderContentInput>> (new ReplacerSuccess (licenseHeaderInput.DocumentPath, newContent));
          },
          (input, errorType, message) =>
              new ReplacerResult<ReplacerSuccess, ReplacerError<LicenseHeaderContentInput>> (new ReplacerError<LicenseHeaderContentInput> (input, errorType, message)));
    }

    /// <summary>
    ///   Updates license headers in a file based on its path, i. e. the new content is written directly to the file.
    /// </summary>
    /// <param name="licenseHeaderInput">
    ///   An <see cref="LicenseHeaderContentInput" /> instance representing the file whose
    ///   license headers are to be updated.
    /// </param>
    /// <returns>
    ///   Returns a <see cref="ReplacerResult{TSuccess,TError}" /> instance containing information about the success or
    ///   error of the operation.
    /// </returns>
    public async Task<ReplacerResult<ReplacerError<LicenseHeaderPathInput>>> RemoveOrReplaceHeader (LicenseHeaderPathInput licenseHeaderInput)
    {
      return await RemoveOrReplaceHeader (
          licenseHeaderInput,
          async (input, document) =>
          {
            await document.ReplaceHeaderIfNecessaryPath (new CancellationToken());
            return new ReplacerResult<ReplacerError<LicenseHeaderPathInput>>();
          },
          (input, errorType, message) =>
              new ReplacerResult<ReplacerSuccess, ReplacerError<LicenseHeaderPathInput>> (new ReplacerError<LicenseHeaderPathInput> (input, errorType, message)));
    }

    /// <summary>
    ///   Updates license headers in a file.
    /// </summary>
    /// <typeparam name="TReturn">The return type of this function.</typeparam>
    /// <typeparam name="TInput">The <see cref="LicenseHeaderInput" /> subtype used as input within this method.</typeparam>
    /// <param name="licenseHeaderInput">The file whose license headers should be updated.</param>
    /// <param name="successSupplier">
    ///   A function that generates the return value in the case of an success, given the
    ///   respective <see cref="LicenseHeaderInput" /> and the <see cref="Document" /> that was created in the process.
    /// </param>
    /// <param name="errorSupplier">
    ///   A function that generates the return value in the case of an error, given the respective
    ///   <see cref="LicenseHeaderInput" />, determined <see cref="ReplacerErrorType" /> and error description.
    /// </param>
    /// <returns>
    ///   Returns either object representing a success, supplied by <paramref name="successSupplier" />, or an object
    ///   representing an error, supplied by <paramref name="errorSupplier" />.
    /// </returns>
    private async Task<TReturn> RemoveOrReplaceHeader<TReturn, TInput> (
        TInput licenseHeaderInput,
        Func<TInput, Document, Task<TReturn>> successSupplier,
        Func<TInput, ReplacerErrorType, string, TReturn> errorSupplier)
        where TInput : LicenseHeaderInput
        where TReturn : ReplacerResult
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
              return errorSupplier (licenseHeaderInput, ReplacerErrorType.NonCommentText, message);
            }

            try
            {
              return await successSupplier (licenseHeaderInput, document);
            }
            catch (ParseException)
            {
              message = string.Format (Resources.Error_InvalidLicenseHeader, licenseHeaderInput.DocumentPath).ReplaceNewLines();
              return errorSupplier (licenseHeaderInput, ReplacerErrorType.ParsingError, message);
            }

          case CreateDocumentResult.LanguageNotFound:
            message = string.Format (Resources.Error_LanguageNotFound, Path.GetExtension (licenseHeaderInput.DocumentPath)).ReplaceNewLines();
            return errorSupplier (licenseHeaderInput, ReplacerErrorType.LanguageNotFound, message);

          case CreateDocumentResult.EmptyHeader:
            message = string.Format (Resources.Error_HeaderNullOrEmpty, licenseHeaderInput.Extension);
            return errorSupplier (licenseHeaderInput, ReplacerErrorType.EmptyHeader, message);

          case CreateDocumentResult.NoHeaderFound:
            message = string.Format (Resources.Error_NoHeaderFound).ReplaceNewLines();
            return errorSupplier (licenseHeaderInput, ReplacerErrorType.NoHeaderFound, message);

          case CreateDocumentResult.LicenseHeaderDocument:
            message = string.Format (HeaderDefinitionExtension).ReplaceNewLines();
            return errorSupplier (licenseHeaderInput, ReplacerErrorType.Miscellaneous, message);

          default:
            throw new ArgumentOutOfRangeException();
        }
      }
      catch (ArgumentException ex)
      {
        var message = $"{ex.Message} {licenseHeaderInput.DocumentPath}";
        return errorSupplier (licenseHeaderInput, ReplacerErrorType.Miscellaneous, message);
      }
    }

    /// <summary>
    ///   Updates license headers of files based on their contents, i. e. the new contents are returned. Setting the new
    ///   contents is the caller's responsibility.
    /// </summary>
    /// <param name="licenseHeaderInputs">
    ///   An range of <see cref="LicenseHeaderContentInput" /> instances representing the files
    ///   whose license headers are to be updated.
    /// </param>
    /// <param name="progress">
    ///   A <see cref="IProgress{T}" /> whose generic type parameter is
    ///   <see cref="ReplacerProgressContentReport" /> that invokes callbacks for each reported progress value.
    /// </param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the work.</param>
    /// <returns>
    ///   Returns a range of <see cref="ReplacerResult{TSuccess,TError}" /> instances containing information about the
    ///   success or error of the operations.
    /// </returns>
    public async Task<IEnumerable<ReplacerResult<ReplacerSuccess, ReplacerError<LicenseHeaderContentInput>>>> RemoveOrReplaceHeader (
        ICollection<LicenseHeaderContentInput> licenseHeaderInputs,
        IProgress<ReplacerProgressContentReport> progress,
        CancellationToken cancellationToken)
    {
      var results = new List<ReplacerResult<ReplacerSuccess, ReplacerError<LicenseHeaderContentInput>>>();
      ResetProgress (licenseHeaderInputs.Count);

      var tasks = new List<Task<ReplacerResult<ReplacerSuccess, ReplacerError<LicenseHeaderContentInput>>>>();
      foreach (var licenseHeaderInput in licenseHeaderInputs)
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

    /// <summary>
    ///   Updates license headers of files based on their paths, i. e. the new contents written directly to the files.
    /// </summary>
    /// <param name="licenseHeaderInputs">
    ///   An range of <see cref="LicenseHeaderPathInput" /> instances representing the files
    ///   whose license headers are to be updated.
    /// </param>
    /// <param name="progress">
    ///   A <see cref="IProgress{T}" /> whose generic type parameter is
    ///   <see cref="ReplacerProgressReport" /> that invokes callbacks for each reported progress value.
    /// </param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the work.</param>
    /// <returns>
    ///   Returns a <see cref="ReplacerResult{TSuccess,TError}" /> instance containing information about errors that
    ///   have potentially occurred during the update operations.
    /// </returns>
    public async Task<ReplacerResult<IEnumerable<ReplacerError<LicenseHeaderPathInput>>>> RemoveOrReplaceHeader (
        ICollection<LicenseHeaderPathInput> licenseHeaderInputs,
        IProgress<ReplacerProgressReport> progress,
        CancellationToken cancellationToken)
    {
      var errorList = new ConcurrentQueue<ReplacerError<LicenseHeaderPathInput>>();
      ResetProgress (licenseHeaderInputs.Count);

      var tasks = new List<Task>();
      foreach (var licenseHeaderInput in licenseHeaderInputs)
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
        IProgress<ReplacerProgressContentReport> progress,
        CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();
      if (TryCreateDocument (licenseHeaderInput, out var document) != CreateDocumentResult.DocumentCreated)
      {
        await ReportProgress (progress, cancellationToken, licenseHeaderInput.DocumentPath, licenseHeaderInput.DocumentContent);
        return null;
      }

      string message;
      if (!await document.ValidateHeader() && !licenseHeaderInput.IgnoreNonCommentText)
      {
        cancellationToken.ThrowIfCancellationRequested();
        await ReportProgress (progress, cancellationToken, licenseHeaderInput.DocumentPath, licenseHeaderInput.DocumentContent);

        var extension = Path.GetExtension (licenseHeaderInput.DocumentPath);
        message = string.Format (Resources.Warning_InvalidLicenseHeader, extension).ReplaceNewLines();
        return new ReplacerResult<ReplacerSuccess, ReplacerError<LicenseHeaderContentInput>> (
            new ReplacerError<LicenseHeaderContentInput> (licenseHeaderInput, ReplacerErrorType.NonCommentText, message));
      }

      try
      {
        cancellationToken.ThrowIfCancellationRequested();
        var newContent = await document.ReplaceHeaderIfNecessaryContent (cancellationToken);
        await ReportProgress (progress, cancellationToken, licenseHeaderInput.DocumentPath, newContent);
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

    private async Task ReportProgress (IProgress<ReplacerProgressContentReport> progress, CancellationToken cancellationToken, string filePath, string newContent)
    {
      await _progressReportSemaphore.WaitAsync (cancellationToken);
      try
      {
        _processedFileCount++;
        progress.Report (new ReplacerProgressContentReport (_totalFileCount, _processedFileCount, filePath, newContent));
      }
      finally
      {
        _progressReportSemaphore.Release();
      }
    }

    /// <summary>
    ///   Represents the result of a <see cref="TryCreateDocument" /> invocation.
    /// </summary>
    private enum CreateDocumentResult
    {
      /// <summary>
      ///   Document was created successfully.
      /// </summary>
      DocumentCreated,

      /// <summary>
      ///   The <see cref="LicenseHeaderReplacer" /> instance was not initialized with a <see cref="Language" /> instance
      ///   representing the document's language, as determined by its file extension.
      /// </summary>
      LanguageNotFound,

      /// <summary>
      ///   Header definition for the language specified by the document's extension was not found in the license header
      ///   definition file.
      /// </summary>
      NoHeaderFound,

      /// <summary>
      ///   The document is a license header definition document.
      /// </summary>
      LicenseHeaderDocument,

      /// <summary>
      ///   The header definition for the language specified by the document's extension was found in the license header
      ///   definition, but is null or empty.
      /// </summary>
      EmptyHeader
    }
  }
}