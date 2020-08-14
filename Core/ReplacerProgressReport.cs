using System;
using System.Collections.Generic;

namespace Core
{
  public class ReplacerProgressReport
  {
    /// <summary>
    ///   Initializes a new instance of the <see cref="ReplacerProgressReport" /> class.
    /// </summary>
    /// <param name="totalFileCount">The overall number of files that are to be updated.</param>
    /// <param name="processedFileCount">The number of file that have already been processed.</param>
    public ReplacerProgressReport (int totalFileCount, int processedFileCount)
    {
      TotalFileCount = totalFileCount;
      ProcessedFileCount = processedFileCount;
    }

    /// <summary>
    ///   Gets the overall number of files that are to be updated over the course of one invocation of the
    ///   <see
    ///     cref="LicenseHeaderReplacer.RemoveOrReplaceHeader(System.Collections.Generic.ICollection{Core.LicenseHeaderInput}, IProgress{ReplacerProgressReport}, Func{string, bool})" />
    ///   method.
    /// </summary>
    public int TotalFileCount { get; }

    /// <summary>
    ///   Gets the number of file that have already been processed over the course of one invocation of the
    ///   <see
    ///     cref="LicenseHeaderReplacer.RemoveOrReplaceHeader(ICollection{LicenseHeaderInput}, IProgress{ReplacerProgressReport}, Func{string, bool})" />
    ///   method.
    /// </summary>
    public int ProcessedFileCount { get; }
  }
}