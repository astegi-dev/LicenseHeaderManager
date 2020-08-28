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
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Core;
using EnvDTE;
using LicenseHeaderManager.Headers;
using LicenseHeaderManager.Interfaces;
using log4net;
using Microsoft.VisualStudio.Shell;
using LicenseHeader = LicenseHeaderManager.Headers.LicenseHeader;
using Task = System.Threading.Tasks.Task;

namespace LicenseHeaderManager.Utils
{
  internal static class Extensions
  {
    private static readonly ILog s_log = LogManager.GetLogger (MethodBase.GetCurrentMethod().DeclaringType);

    /// <summary>
    /// Sets the <see cref="LicenseHeaderPathInput.IgnoreNonCommentText"/> property to <see langword="true" /> for all element of the enumerable.
    /// </summary>
    /// <param name="inputs">The <see cref="IEnumerable{T}"/> whose generic type parameter is <see cref="LicenseHeaderPathInput"/> whose items should be mutated.</param>
    /// <remarks>This operation might be useful if the license header input represented by <paramref name="inputs"/> is used only for remove operations. In that case,
    /// no confirmations regarding non-comment text are needed.</remarks>
    public static void IgnoreNonCommentText (this IEnumerable<LicenseHeaderContentInput> inputs)
    {
      foreach (var licenseHeaderInput in inputs)
        licenseHeaderInput.IgnoreNonCommentText = true;
    }

    /// <summary>
    ///   Replaces occurrences of "\n" in a string by new line characters.
    /// </summary>
    /// <returns>A <see cref="string" /> where all occurrences of "\n" have been replaced by new line characters</returns>
    public static string ReplaceNewLines (this string input)
    {
      return input.Replace (@"\n", "\n");
    }

    public static IEnumerable<AdditionalProperty> GetAdditionalProperties (this ProjectItem item)
    {
      // ThreadHelper.ThrowIfNotOnUIThread();

      return new List<AdditionalProperty>
             {
                 CreateAdditionalProperty ("%Project%", () => item.ContainingProject != null, () => item.ContainingProject.Name),
                 CreateAdditionalProperty (
                     "%Namespace%",
                     () => item.FileCodeModel != null && item.FileCodeModel.CodeElements.Cast<CodeElement>().Any (ce => ce.Kind == vsCMElement.vsCMElementNamespace),
                     () => item.FileCodeModel.CodeElements.Cast<CodeElement>().First (ce => ce.Kind == vsCMElement.vsCMElementNamespace).Name)
             };
    }

    private static AdditionalProperty CreateAdditionalProperty (string token, Func<bool> canCreateValue, Func<string> createValue)
    {
      return new AdditionalProperty (token, canCreateValue() ? createValue() : token);
    }

    public static async Task AddLicenseHeaderToItemAsync (
        this ProjectItem item,
        ILicenseHeaderExtension extension,
        bool calledByUser)
    {
      if (item == null || ProjectItemInspection.IsLicenseHeader (item))
        return;

      await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
      var headers = LicenseHeaderFinder.GetHeaderDefinitionForItem (item);
      if (headers != null) // TODO get rid of nested if (get content a view lines above)
      {
        var content = item.GetContent(out var wasAlreadyOpen);
        if (content == null)
          return;

        var result = await extension.LicenseHeaderReplacer.RemoveOrReplaceHeader (
            new LicenseHeaderContentInput (content, item.FileNames[1], headers, item.GetAdditionalProperties()),
            calledByUser);
        await CoreHelpers.HandleResultAsync(result, extension, wasAlreadyOpen);
        return;
      }

      if (calledByUser && LicenseHeader.ShowQuestionForAddingLicenseHeaderFile (item.ContainingProject, extension.DefaultLicenseHeaderPageModel))
        await AddLicenseHeaderToItemAsync (item, extension, true);
    }

    public static string GetContent (this ProjectItem item, out bool wasAlreadyOpen)
    {
      wasAlreadyOpen = item.IsOpen();

      if (!wasAlreadyOpen && !TryOpenDocument(item))
        return null;

      if (!(item.Document.Object ("TextDocument") is TextDocument textDocument))
        return null;

      return textDocument.CreateEditPoint (textDocument.StartPoint).GetText (textDocument.EndPoint);
    }

    public static bool IsOpen(this ProjectItem item)
    {
      return item.IsOpen[Constants.vsViewKindTextView];
    }

    private static bool TryOpenDocument (ProjectItem item)
    {
      try
      {
        item.Open (Constants.vsViewKindTextView);
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

    public static bool TrySetContent (this string itemPath, Solution solution, string content, bool wasOpen)
    {
      var item = solution.FindProjectItem (itemPath);
      if (item == null)
        return false;

      if (!(item.Document.Object ("TextDocument") is TextDocument textDocument))
        return false;

      var wasSaved = item.Document.Saved;

      textDocument.CreateEditPoint (textDocument.StartPoint).Delete (textDocument.EndPoint);
      textDocument.CreateEditPoint (textDocument.StartPoint).Insert (content);

      if (wasOpen)
      {
        // if document had no unsaved changes before, it should not have any now (analogously for when it did have unsaved changes)
        if (wasSaved)
          item.Document.Save();
      }
      else
      {
        item.Document.Close (vsSaveChanges.vsSaveChangesYes);
      }

      return true;
    }

    public static void FireAndForget (this Task task)
    {
      // note: this code is inspired by a tweet from Ben Adams: https://twitter.com/ben_a_adams/status/1045060828700037125
      // Only care about tasks that may fault (not completed) or are faulted,
      // so fast-path for SuccessfullyCompleted and Canceled tasks.
      if (!task.IsCompleted || task.IsFaulted)
          // use "_" (Discard operation) to remove the warning IDE0058: Because this call is not awaited, execution of the current method continues before the call is completed
          // https://docs.microsoft.com/en-us/dotnet/csharp/discards#a-standalone-discard
        _ = ForgetAwaited (task);

      // Allocate the async/await state machine only when needed for performance reason.
      // More info about the state machine: https://blogs.msdn.microsoft.com/seteplia/2017/11/30/dissecting-the-async-methods-in-c/
      static async Task ForgetAwaited (Task task)
      {
        try
        {
          // No need to resume on the original SynchronizationContext, so use ConfigureAwait(false)
          await task.ConfigureAwait (false);
        }
        catch (Exception ex)
        {
          MessageBoxHelper.ShowError ($"Asynchronous Task failed: {ex.Message}\nSee output pane or log file for more details.", Resources.TaskFailed);
          s_log.Error ("Asynchronous Task failed", ex);
        }
      }
    }
  }
}