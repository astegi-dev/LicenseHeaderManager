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
using System.Linq;
using System.Threading.Tasks;
using Core;
using EnvDTE;
using LicenseHeaderManager.Headers;
using LicenseHeaderManager.Interfaces;
using Microsoft.VisualStudio.Shell;
using LicenseHeader = LicenseHeaderManager.Headers.LicenseHeader;
using Task = System.Threading.Tasks.Task;

namespace LicenseHeaderManager.Utils
{
  internal static class Extensions
  {
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

    public static async Task<ReplacerResult<ReplacerError>> AddLicenseHeaderToItemAsync (this ProjectItem item, ILicenseHeaderExtension extension, bool calledByUser)
    {
      if (item == null || ProjectItemInspection.IsLicenseHeader (item))
        return new ReplacerResult<ReplacerError>();

      await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
      var headers = LicenseHeaderFinder.GetHeaderDefinitionForItem (item);
      if (headers != null)
        return await extension.LicenseHeaderReplacer.RemoveOrReplaceHeader (
            new LicenseHeaderInput (item.FileNames[1], headers, item.GetAdditionalProperties()),
            calledByUser,
            CoreHelpers.NonCommentLicenseHeaderDefinitionInquiry,
            message => CoreHelpers.NoLicenseHeaderDefinitionFound (message, extension));

      if (calledByUser && LicenseHeader.ShowQuestionForAddingLicenseHeaderFile (item.ContainingProject, extension.DefaultLicenseHeaderPageModel))
        return await AddLicenseHeaderToItemAsync (item, extension, true);

      return new ReplacerResult<ReplacerError>();
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
          // TODO logging (in general) + log exception stack trace
          MessageBoxHelper.ShowError ($"Task failed: {ex.Message}", Resources.TaskFailed);
        }
      }
    }
  }
}