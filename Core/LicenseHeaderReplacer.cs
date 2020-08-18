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
    ///   Removes or replaces the header of a given project item.
    /// </summary>
    /// <param name="licenseHeaderInput">The licenseHeaderInput item.</param>
    /// <param name="calledByUser">
    ///   Specifies whether the command was called by the user (as opposed to automatically by a
    ///   linked command or by ItemAdded)
    /// </param>
    /// <param name="nonCommentTextInquiry">
    ///   Determines whether license headers should be inserted even if they contain non-comment text for the respective
    ///   language.
    ///   Is supplied with a <see cref="string" /> argument that represents a specific message describing the issue. If null,
    ///   license headers are inserted.
    /// </param>
    /// <param name="commentDefinitionNotFoundAction">
    ///   Is executed if there there is no license header definition configured for the language of a specific file.
    ///   If null, no action is executed in this case.
    /// </param>
    public async Task<ReplacerResult<ReplacerError>> RemoveOrReplaceHeader (
        LicenseHeaderInput licenseHeaderInput,
        bool calledByUser,
        Func<string, bool> nonCommentTextInquiry = null,
        Action<string> commentDefinitionNotFoundAction = null)
    {
      var returnObject = new ReplacerResult<ReplacerError>();
      try
      {
        var result = TryCreateDocument (licenseHeaderInput, out var document);

        switch (result)
        {
          case CreateDocumentResult.DocumentCreated:
            if (!await document.ValidateHeader())
            {
              var message = string.Format (Resources.Warning_InvalidLicenseHeader, Path.GetExtension (licenseHeaderInput.DocumentPath)).ReplaceNewLines();
              var addDespiteNonCommentText = nonCommentTextInquiry?.Invoke (message) ?? true;
              if (!addDespiteNonCommentText)
              {
                message = $"Execution of {nameof(RemoveOrReplaceHeader)} was cancelled by caller";
                returnObject = new ReplacerResult<ReplacerError> (new ReplacerError (licenseHeaderInput.DocumentPath, ErrorType.NonCommentText, message));
                break;
              }
            }

            try
            {
              await document.ReplaceHeaderIfNecessary (new CancellationToken());
            }
            catch (ParseException)
            {
              var message = string.Format (Resources.Error_InvalidLicenseHeader, licenseHeaderInput.DocumentPath).ReplaceNewLines();
              returnObject = new ReplacerResult<ReplacerError> (new ReplacerError (licenseHeaderInput.DocumentPath, ErrorType.ParsingError, message));
            }

            break;
          case CreateDocumentResult.LanguageNotFound:
            if (calledByUser)
            {
              var message = string.Format (Resources.Error_LanguageNotFound, Path.GetExtension (licenseHeaderInput.DocumentPath)).ReplaceNewLines();

              // TODO test with project with .snk file (e.g. DependDB.Util): works, but window closes immediately after showing
              if (commentDefinitionNotFoundAction != null)
                commentDefinitionNotFoundAction (message);
              else
                returnObject = new ReplacerResult<ReplacerError> (new ReplacerError (licenseHeaderInput.DocumentPath, ErrorType.LanguageNotFound, message));
            }

            break;
          case CreateDocumentResult.EmptyHeader:
            break;
          case CreateDocumentResult.NoHeaderFound:
            if (calledByUser)
            {
              var message = string.Format (Resources.Error_NoHeaderFound).ReplaceNewLines();
              returnObject = new ReplacerResult<ReplacerError> (new ReplacerError (licenseHeaderInput.DocumentPath, ErrorType.NoHeaderFound, message));
            }

            break;
        }
      }
      catch (ArgumentException ex)
      {
        var message = $"{ex.Message} {licenseHeaderInput.DocumentPath}";
        returnObject = new ReplacerResult<ReplacerError> (new ReplacerError (licenseHeaderInput.DocumentPath, ErrorType.Miscellaneous, message));
      }

      return returnObject;
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

    private async Task RemoveOrReplaceHeaderForOneFile (
        LicenseHeaderInput licenseHeaderInput,
        Func<string, bool> nonCommentTextInquiry,
        IProgress<ReplacerProgressReport> progress,
        CancellationToken cancellationToken,
        ConcurrentQueue<ReplacerError> errors)
    {
      cancellationToken.ThrowIfCancellationRequested();
      if (TryCreateDocument (licenseHeaderInput, out var document) != CreateDocumentResult.DocumentCreated)
      {
        await ReportProgress (progress, cancellationToken);
        return;
      }

      string message;
      var replace = true;

      if (!await document.ValidateHeader())
      {
        var extension = Path.GetExtension (licenseHeaderInput.DocumentPath);
        if (!_extensionsWithInvalidHeaders.TryGetValue (extension, out replace))
        {
          message = string.Format (Resources.Warning_InvalidLicenseHeader, extension).ReplaceNewLines();
          replace = nonCommentTextInquiry?.Invoke (message) ?? true;
          _extensionsWithInvalidHeaders[extension] = replace;
        }
      }

      if (!replace)
      {
        cancellationToken.ThrowIfCancellationRequested();
        await ReportProgress (progress, cancellationToken);
        return;
      }

      try
      {
        cancellationToken.ThrowIfCancellationRequested();
        await document.ReplaceHeaderIfNecessary (cancellationToken);
      }
      catch (ParseException)
      {
        message = string.Format (Resources.Error_InvalidLicenseHeader, licenseHeaderInput.DocumentPath).ReplaceNewLines();
        errors.Enqueue (new ReplacerError (licenseHeaderInput.DocumentPath, ErrorType.ParsingError, message));
      }

      await ReportProgress (progress, cancellationToken);
    }

    public async Task<ReplacerResult<IEnumerable<ReplacerError>>> RemoveOrReplaceHeader (
        ICollection<LicenseHeaderInput> licenseHeaders,
        IProgress<ReplacerProgressReport> progress,
        CancellationToken cancellationToken,
        Func<string, bool> nonCommentTextInquiry = null)
    {
      var errorList = new ConcurrentQueue<ReplacerError>();
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
                    return RemoveOrReplaceHeaderForOneFile (licenseHeaderInput, nonCommentTextInquiry, progress, cancellationToken, errorList);
                  }
                  finally
                  {
                    _taskStartSemaphore.Release();
                  }
                },
                cancellationToken));
      }

      await Task.WhenAll (tasks);

      return errorList.Count == 0 ? new ReplacerResult<IEnumerable<ReplacerError>>() : new ReplacerResult<IEnumerable<ReplacerError>> (errorList);
    }

    public static bool IsLicenseHeader (string documentPath)
    {
      return Path.GetExtension (documentPath) == LicenseHeader.Extension;
    }

    /// <summary>
    ///   Tries to open a given project item as a Document which can be used to add or remove headers.
    /// </summary>
    /// <param name="documentPath">The project item.</param>
    /// <param name="document">The document which was created or null if an error occured (see return value).</param>
    /// <param name="additionalProperties"></param>
    /// <param name="headers">
    ///   A dictionary of headers using the file extension as key and the header as value or null if
    ///   headers should only be removed.
    /// </param>
    /// <returns>A value indicating the result of the operation. Document will be null unless DocumentCreated is returned.</returns>
    public CreateDocumentResult TryCreateDocument (LicenseHeaderInput licenseHeaderInput, out Document document)
    {
      document = null;

      if (IsLicenseHeader (licenseHeaderInput.DocumentPath))
        return CreateDocumentResult.LicenseHeaderDocument;

      var language = _languages.FirstOrDefault (x => x.Extensions.Any (y => licenseHeaderInput.DocumentPath.EndsWith (y, StringComparison.OrdinalIgnoreCase)));

      if (language == null)
        return CreateDocumentResult.LanguageNotFound;

      string[] header = null;
      if (licenseHeaderInput.Headers != null)
      {
        var extension = licenseHeaderInput.Headers.Keys
            .OrderByDescending (x => x.Length)
            .FirstOrDefault (x => licenseHeaderInput.DocumentPath.EndsWith (x, StringComparison.OrdinalIgnoreCase));

        if (extension == null)
          return CreateDocumentResult.NoHeaderFound;

        header = licenseHeaderInput.Headers[extension];

        if (header.All (string.IsNullOrEmpty))
          return CreateDocumentResult.EmptyHeader;
      }

      document = new Document (licenseHeaderInput.DocumentPath, language, header, licenseHeaderInput.AdditionalProperties, _keywords);

      return CreateDocumentResult.DocumentCreated;
    }
  }
}