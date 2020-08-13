using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using Core;
using EnvDTE;
using LicenseHeaderManager.Headers;
using LicenseHeaderManager.Interfaces;

namespace LicenseHeaderManager.Utils
{
  internal static class CoreHelpers
  {
    public static void OnProgressReported (ReplacerProgressReport obj)
    {
      OutputWindowHandler.WriteMessage($"Processed {obj.ProcessedFileCount} of {obj.TotalFileCount} files.");
    }

    /// <summary>
    ///   Is executed when the Core reports that the license header definition file to be used on a specific file contains
    ///   content that is not recognized as comments for the respective language.
    /// </summary>
    /// <param name="message">Specific message reported by core.</param>
    /// <returns>True if the license header should still be inserted, otherwise false.</returns>
    public static bool NonCommentLicenseHeaderDefinitionInquiry (string message)
    {
      return MessageBox.Show (message, Resources.Warning, MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) == MessageBoxResult.Yes;
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
      if (MessageBox.Show (message, Resources.Error, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes)
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
        MessageBox.Show ($"Error: {result.Error.Description}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    public static void HandleResult (ReplacerResult<IEnumerable<ReplacerError>> result)
    {
      if (!result.IsSuccess)
        MessageBox.Show ($"Encountered errors in {result.Error.Count()} files", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }
  }
}