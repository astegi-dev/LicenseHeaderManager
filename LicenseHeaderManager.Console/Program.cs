using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using Core;
using Core.Options;

namespace LicenseHeaderManager.Console
{
  public static class Program
  {
    private const string c_defaultMode = "add";

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
                                () => c_defaultMode,
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
          (modeString, configuration, files, directory, recursive) =>
          {
            modeString = modeString ?? c_defaultMode;
            if (!modeString.Equals("add", StringComparison.OrdinalIgnoreCase) && !modeString.Equals("remove", StringComparison.OrdinalIgnoreCase))
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

            var mode = modeString.Equals("add", StringComparison.OrdinalIgnoreCase) ? UpdateMode.Add : UpdateMode.Remove;
            try
            {
              UpdateLicenseHeaders(mode, configuration, files, directory, recursive);
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

    private static void UpdateLicenseHeaders(
        UpdateMode mode = UpdateMode.Add,
        FileInfo configuration = null,
        FileInfo[] files = null,
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

    private static void UpdateLicenseHeadersForFiles(UpdateMode mode, FileSystemInfo headerDefinitionFile, IReadOnlyList<FileInfo> files)
    {
      if (files.Count == 1)
        UpdateLicenseHeaderForOneFile(mode, headerDefinitionFile.FullName, files[0].FullName);

      if (files.Count > 1)
        UpdateLicenseHeadersForMultipleFiles(mode, headerDefinitionFile.FullName, files.Select(x => x.FullName));
    }

    private static void UpdateLicenseHeadersForDirectory(UpdateMode mode, FileInfo headerDefinitionFile, DirectoryInfo directory, bool recursive)
    {
      var files = directory.EnumerateFiles("*", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
    }

    private static void UpdateLicenseHeaderForOneFile(UpdateMode mode, string definitionFilePath, string filePath)
    {
      var headerExtractor = new LicenseHeaderExtractor();
      var defaultCoreSettings = new CoreOptions(true);
      var replacer = new LicenseHeaderReplacer(defaultCoreSettings.Languages, CoreOptions.RequiredKeywordsAsEnumerable(defaultCoreSettings.RequiredKeywords));

      var headers = mode == UpdateMode.Add ? headerExtractor.ExtractHeaderDefinitions(definitionFilePath) : null;
      var replacerInput = new LicenseHeaderPathInput(filePath, headers);

      var replacerResult = replacer.RemoveOrReplaceHeader(replacerInput).GetAwaiter().GetResult();

      if (!replacerResult.IsSuccess)
      {
        System.Console.WriteLine($"\nAn error of type '{replacerResult.Error.Type}' occurred: '{replacerResult.Error.Description}'");
        Exit(false);
      }

      System.Console.WriteLine($"\n{(mode == UpdateMode.Add ? "Adding/Replacing" : "Removing")} succeeded.");
      Exit(true);
    }

    private static void UpdateLicenseHeadersForMultipleFiles(UpdateMode mode, string definitionFilePath, IEnumerable<string> filePath)
    {
      throw new NotImplementedException();
    }

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