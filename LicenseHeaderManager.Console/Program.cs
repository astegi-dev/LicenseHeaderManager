using System;
using Core;
using Core.Options;

namespace LicenseHeaderManager.Console
{
  public static class Program
  {
    /* Should soon support the following CLI arguments
       add -c <licenseHeaderDefinitionFilePath> -f <filePath> [<filePath2>...]
       add -c <licenseHeaderDefinitionFilePath> -d <directoryPath>
       add -c <licenseHeaderDefinitionFilePath> -d -r <directoryPath> (recursive)
       remove -c <licenseHeaderDefinitionFilePath> -f <filePath1> [<filePath2>...]
       remove -c <licenseHeaderDefinitionFilePath> -d <directoryPath>
       remove -c <licenseHeaderDefinitionFilePath> -d -r <directoryPath> (recursive)
     */

    public static void Main (string[] args)
    {
      try
      {
        var definitionFile = GetOneFileFromInput ("Enter the path of a license header definition file:", true);
        var singleFile = GetBoolFromInput ("Do you want to update the license headers of a single (Y) or multiple files (N)?");

        if (singleFile)
          InsertIntoOneFile (definitionFile);
        else
          InsertIntoMultipleFiles (definitionFile);

        Exit (true);
      }
      catch (Exception ex)
      {
        System.Console.WriteLine ("\nEncountered an unhandled error:");
        System.Console.WriteLine (ex);
        Exit (false);
      }
    }

    private static void Exit (bool success)
    {
      System.Console.WriteLine ("\nPress any key to exit.");

      // read and intercept ("suppress") available keys in the input stream (possibly stemming from mistakenly pasting multiline text into the console)
      while (System.Console.KeyAvailable)
        System.Console.ReadKey (true);

      // wait for the "actually intended" key press by the user, then exit
      System.Console.ReadKey();
      Environment.Exit (success ? 0 : 1);
    }

    private static void InsertIntoOneFile (string licenseHeaderDefinitionFilePath)
    {
      var headerExtractor = new LicenseHeaderExtractor();
      var defaultCoreSettings = new CoreOptions (true);
      var replacer = new LicenseHeaderReplacer (defaultCoreSettings.Languages, CoreOptions.RequiredKeywordsAsEnumerable (defaultCoreSettings.RequiredKeywords));
      var filePath = GetOneFileFromInput ("\nSpecify the file whose license headers should be updated:", true);

      var add = GetBoolFromInput ("Should headers be added (Y) or removed (N)?");
      var headers = add ? headerExtractor.ExtractHeaderDefinitions (licenseHeaderDefinitionFilePath) : null;
      var replacerInput = new LicenseHeaderPathInput (filePath, headers);

      var replacerResult = replacer.RemoveOrReplaceHeader (replacerInput).GetAwaiter().GetResult();

      if (!replacerResult.IsSuccess)
      {
        System.Console.WriteLine ($"\nAn error of type '{replacerResult.Error.Type}' occurred: '{replacerResult.Error.Description}'");
        Exit (false);
      }

      System.Console.WriteLine ($"\n{(add ? "Adding/Replacing" : "Removing")} succeeded.");
      Exit (true);
    }

    private static void InsertIntoMultipleFiles (string licenseHeaderDefinitionFilePath)
    {
      throw new NotImplementedException();
    }

    private static bool GetBoolFromInput (string prompt = "Enter yes or no.")
    {
      string result;
      do
      {
        System.Console.WriteLine ($"\n{prompt} (Y/N)");
        result = System.Console.ReadLine();
      } while (result?.Equals ("y", StringComparison.OrdinalIgnoreCase) != true && result?.Equals ("n", StringComparison.OrdinalIgnoreCase) != true);

      return result.Equals ("y", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetOneFileFromInput (string prompt = "Enter file path", bool exitOnFailure = false)
    {
      System.Console.WriteLine (prompt);
      var filePath = System.Console.ReadLine()?.Trim ('\"');

      if (!string.IsNullOrWhiteSpace (filePath))
        return filePath;

      System.Console.WriteLine ("The path must point to a file that actually exists!");
      System.Console.WriteLine ($"You entered: \"{filePath}\"");
      if (exitOnFailure)
        Exit (false);

      return null;
    }
  }
}