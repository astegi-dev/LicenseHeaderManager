using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Threading;
using Core;
using Core.Options;

namespace LicenseHeaderManager.Console
{
  public static class Program
  {
    /// <summary>
    /// Specifies if the license headers should be inserted or removed from the files.
    /// </summary>
    private enum UpdateMode
    {
      Add = 0,
      Remove = 1
    }

    public static int Main(string[] args)
    {
      var rootCommand = new RootCommand
                        {
                            new Option<string> (
                                new[] { "-m", "--mode" },
                                () => "add",
                                "Specifies whether license headers should be added or removed. Must be \"add\" or \"remove\" (case-insensitive)."),
                            new Option<FileInfo> (
                                new[] { "-c", "--configuration" },
                                () => null,
                                "Specifies the path to the license header definition file to be used for the update operations. Must be present."),
                            new Option<FileInfo[]> (
                                new[] { "-f", "--files" },
                                () => null,
                                "Specifies the path (or paths) to the files whose headers should be updated. Must not be present if \"directory\" is present."),
                            new Option<DirectoryInfo> (
                                new[] { "-d", "--directory" },
                                () => null,
                                "Specifies the path of the directory containing the files whose headers should be updated. Must not be present if \"files\" is present."),
                            new Option<bool?> (
                                new[] { "-r", "--recursive" },
                                () => null,
                                "Specifies whether the directory represented by \"directory\" should be searched recursively. Is ignored if \"files\" is present.")
                        };

      rootCommand.Description = "Updates license headers for files";

      // Note that the parameters of the handler method are matched according to the names of the options
      rootCommand.Handler = CommandHandler.Create<string, FileInfo, FileInfo[], DirectoryInfo, bool?>(
          (mode, configuration, files, directory, recursive) =>
          {
            if (!mode.Equals("add", StringComparison.OrdinalIgnoreCase) && !mode.Equals("remove", StringComparison.OrdinalIgnoreCase))
            {
              System.Console.WriteLine("Invalid mode");
              Exit(false);
            }

            if (files != null && directory != null || files == null && directory == null)
            {
              System.Console.WriteLine("Exactly one of the arguments \"files\" and \"directory\" must be present.");
              Exit(false);
            }

            if (files != null && recursive.HasValue)
              System.Console.WriteLine("Since \"files\" is present, option \"recursive\" is ignored");

            if (directory != null && !recursive.HasValue)
            {
              System.Console.WriteLine("Since \"directory\" is present, option \"recursive\" must be present");
              Exit(false);
            }

            var modeEnum = mode.Equals("add", StringComparison.OrdinalIgnoreCase) ? UpdateMode.Add : UpdateMode.Remove;
            try
            {
              UpdateLicenseHeaders(modeEnum, configuration, files, directory, recursive);
            }
            catch (Exception ex)
            {
              System.Console.WriteLine("\nEncountered an unhandled error:");
              System.Console.WriteLine(ex);
              Exit(false);
            }
          });

      // Parse the incoming args and invoke the handler
      return rootCommand.InvokeAsync(args).Result;
    }

    /// <summary>
    /// Updates the headers in the files of the given paths or in the files in the given directory, depending on the options passed.
    /// </summary>
    /// <param name="mode">Specifies whether the license headers should be added or removed to/from the files (or directory).</param>
    /// <param name="configuration">Specifies the path to the license header definition file.</param>
    /// <param name="files">Specifies the path (or paths) to the files that should be updated.</param>
    /// <param name="directory">Specifies the path of the directory containing the files that should be updated.</param>
    /// <param name="recursive">Specifies whether the directory should be searched recursively.</param>
    private static void UpdateLicenseHeaders(
        UpdateMode mode = UpdateMode.Add,
        FileSystemInfo configuration = null,
        IReadOnlyList<FileInfo> files = null,
        DirectoryInfo directory = null,
        bool? recursive = false)
    {
      if (files != null && directory == null)
        UpdateLicenseHeadersForFiles(mode, configuration, files);

      if (!recursive.HasValue)
        throw new ArgumentNullException(nameof(recursive));

      if (directory != null && files == null)
        UpdateLicenseHeadersForDirectory(mode, configuration, directory, recursive.Value);

      Exit(true);
    }

    /// <summary>
    /// Updates the headers of one file or multiple files.
    /// </summary>
    /// <param name="mode">Specifies whether the license headers should be added or removed to/from the files.</param>
    /// <param name="headerDefinitionFile">Specifies the path to the license header definition file.</param>
    /// <param name="files">Specifies the path (or paths) to the files that should be updated.</param>
    private static void UpdateLicenseHeadersForFiles(UpdateMode mode, FileSystemInfo headerDefinitionFile, IReadOnlyList<FileInfo> files)
    {
      if (files.Count == 1)
        UpdateLicenseHeaderForOneFile(mode, headerDefinitionFile.FullName, files[0].FullName);

      if (files.Count > 1)
        UpdateLicenseHeadersForMultipleFiles(mode, headerDefinitionFile.FullName, files.Select(x => x.FullName));
    }

    /// <summary>
    /// Updates the headers of all files in the directory passed. If \"recursive\" is true, all files in the subfolders are updated as well.
    /// Otherwise only the files in the top-level directory are updated.
    /// </summary>
    /// <param name="mode">Specifies whether the license headers should be added or removed to/from the files in the directory passed.</param>
    /// <param name="headerDefinitionFile"></param>
    /// <param name="directory">Specifies the path of the directory containing the files that should be updated.</param>
    /// <param name="recursive">Specifies whether the directory should be searched recursively.</param>
    private static void UpdateLicenseHeadersForDirectory(UpdateMode mode, FileSystemInfo headerDefinitionFile, DirectoryInfo directory, bool recursive)
    {
      var files = directory.EnumerateFiles("*", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
      UpdateLicenseHeadersForMultipleFiles(mode, headerDefinitionFile.FullName, files.Select(x => x.FullName));
    }

    /// <summary>
    /// Updates the headers of one file.
    /// </summary>
    /// <param name="mode">Specifies whether the license headers should be added or removed to/from the files.</param>
    /// <param name="definitionFilePath">Specifies the path to the license header definition file.</param>
    /// <param name="filePath">Specifies the path to the file that should be updated.</param>
    private static void UpdateLicenseHeaderForOneFile(UpdateMode mode, string definitionFilePath, string filePath)
    {
      var headerExtractor = new LicenseHeaderExtractor();
      var defaultCoreSettings = new CoreOptions(true);
      var replacer = new LicenseHeaderReplacer(defaultCoreSettings.Languages, CoreOptions.RequiredKeywordsAsEnumerable(defaultCoreSettings.RequiredKeywords));

      var headers = mode == UpdateMode.Add ? headerExtractor.ExtractHeaderDefinitions(definitionFilePath) : null;
      var replacerInput = new LicenseHeaderPathInput(filePath, headers);

      var replacerResult = replacer.RemoveOrReplaceHeader(replacerInput).Result;

      if (!replacerResult.IsSuccess)
      {
        System.Console.WriteLine($"\nAn error of type '{replacerResult.Error.Type}' occurred: '{replacerResult.Error.Description}'");
        Exit(false);
      }

      System.Console.WriteLine($"\n{(mode == UpdateMode.Add ? "Adding/Replacing" : "Removing")} succeeded.");
      Exit(true);
    }

    /// <summary>
    /// Updates the headers of multiple files.
    /// </summary>
    /// <param name="mode">Specifies whether the license headers should be added or removed to/from the files.</param>
    /// <param name="definitionFilePath">Specifies the path to the license header definition file.</param>
    /// <param name="filePaths">Specifies the paths to the files that should be updated.</param>
    private static void UpdateLicenseHeadersForMultipleFiles(UpdateMode mode, string definitionFilePath, IEnumerable<string> filePaths)
    {
      var headerExtractor = new LicenseHeaderExtractor();
      var defaultCoreSettings = new CoreOptions(true);
      var replacer = new LicenseHeaderReplacer(defaultCoreSettings.Languages, CoreOptions.RequiredKeywordsAsEnumerable(defaultCoreSettings.RequiredKeywords));

      var headers = mode == UpdateMode.Add ? headerExtractor.ExtractHeaderDefinitions(definitionFilePath) : null;
      var replacerInput = filePaths.Select(x => new LicenseHeaderPathInput(x, headers));

      var replacerResult = replacer.RemoveOrReplaceHeader(
          replacerInput.ToList(), 
          new ConsoleProgress<ReplacerProgressReport> (progress =>
          {
            System.Console.WriteLine ($"File '{progress.ProcessedFilePath}' updated ({progress.ProcessedFileCount}/{progress.TotalFileCount})");
          }), 
          new CancellationToken()).Result;

      if (replacerResult.IsSuccess)
      {
        System.Console.WriteLine($"\n{(mode == UpdateMode.Add ? "Adding/Replacing" : "Removing")} succeeded.");
        Exit(true);
      }

      foreach (var error in replacerResult.Error)
        System.Console.WriteLine($"\nAn error of type '{error.Type}' occurred: '{error.Description}'");

      Exit(false);
    }

    /// <summary>
    /// Terminates the console application and exits with the exit code according to the success parameter.
    /// </summary>
    /// <param name="success">Specifies whether the console application should terminate with the success exit code (true) or with the failure exit code (false).</param>
    private static void Exit(bool success)
    {
      System.Console.WriteLine("\nPress any key to exit.");

      // read and intercept ("suppress") available keys in the input stream (possibly stemming from mistakenly pasting multiline text into the console)
      while (System.Console.KeyAvailable)
        System.Console.ReadKey(true);

      // wait for the "actually intended" key press by the user, then exit
      System.Console.ReadKey();
      Environment.Exit(success ? 0 : 1);
    }
  }
}