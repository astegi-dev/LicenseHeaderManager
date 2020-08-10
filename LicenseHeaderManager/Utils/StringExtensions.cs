#region copyright
// Copyright (c) rubicon IT GmbH

// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
// FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE. 
#endregion

using System;
using System.Threading.Tasks;
using System.Windows;

namespace LicenseHeaderManager.Utils
{
  internal static class StringExtensions
  {

    public static void FireAndForget (this Task task)
    {
      // note: this code is inspired by a tweet from Ben Adams: https://twitter.com/ben_a_adams/status/1045060828700037125
      // Only care about tasks that may fault (not completed) or are faulted,
      // so fast-path for SuccessfullyCompleted and Canceled tasks.
      if (!task.IsCompleted || task.IsFaulted)
      {
        // use "_" (Discard operation) to remove the warning IDE0058: Because this call is not awaited, execution of the current method continues before the call is completed
        // https://docs.microsoft.com/en-us/dotnet/csharp/discards#a-standalone-discard
        _ = ForgetAwaited (task);
      }

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
          MessageBox.Show ($"Task failed: {ex.Message}", "Task failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
      }
    }

    internal static int CountOccurrence (this string inputString, string searchString)
    {
      if (inputString == null)
        throw new ArgumentNullException ("inputString");
      if (string.IsNullOrEmpty (searchString))
        throw new ArgumentNullException ("searchString");

      var idx = 0;
      var count = 0;
      while ((idx = inputString.IndexOf (searchString, idx)) != -1)
      {
        idx += searchString.Length;
        count++;
      }
      return count;
    }
  }
}