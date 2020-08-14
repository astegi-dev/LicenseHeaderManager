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
using System.ComponentModel;
using LicenseHeaderManager.Interfaces;
using LicenseHeaderManager.MenuItemCommands.Common;
using LicenseHeaderManager.UpdateViewModels;
using LicenseHeaderManager.UpdateViews;
using LicenseHeaderManager.Utils;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace LicenseHeaderManager.ButtonHandler
{
  internal class FolderProjectButtonHandler
  {
    private readonly ILicenseHeaderExtension _licenseHeaderExtension;
    private readonly ButtonOperation _operation;

    private FolderProjectUpdateDialog _dialog;

    public FolderProjectButtonHandler (ILicenseHeaderExtension licenseHeaderExtension, ButtonOperation operation)
    {
      _licenseHeaderExtension = licenseHeaderExtension;
      _operation = operation;
    }

    public void HandleButton (object sender, EventArgs e)
    {
      var folderProjectUpdateViewModel = new FolderProjectUpdateViewModel();
      IButtonCommand command;
      switch (_operation)
      {
        case ButtonOperation.Add:
          command = new AddLicenseHeaderToAllFilesInFolderProjectHelper (_licenseHeaderExtension, folderProjectUpdateViewModel);
          break;
        case ButtonOperation.Remove:
          command = new RemoveLicenseHeaderToAllFilesInFolderProjectHelper (_licenseHeaderExtension, folderProjectUpdateViewModel);
          break;
        default:
          throw new ArgumentOutOfRangeException (nameof(_operation), _operation, null);
      }

      _dialog = new FolderProjectUpdateDialog (folderProjectUpdateViewModel);
      _dialog.Closing += DialogOnClosing;

      Task.Run (() => HandleButtonInternalAsync (command)).FireAndForget();
      _dialog.ShowModal();
    }

    private async Task HandleButtonInternalAsync (IButtonCommand command)
    {
      try
      {
        await command.ExecuteAsync (null, _dialog);
      }
      catch (Exception exception)
      {
        MessageBoxHelper.ShowInformation (
            $"The command '{command.GetCommandName()}' failed with the exception '{exception.Message}'. See Visual Studio Output Window for Details.");
        OutputWindowHandler.WriteMessage (exception.ToString());
      }

      await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
      _dialog.Close();
    }

    private void DialogOnClosing (object sender, CancelEventArgs e)
    {
      // TODO how to cancel Core operation?
    }
  }
}