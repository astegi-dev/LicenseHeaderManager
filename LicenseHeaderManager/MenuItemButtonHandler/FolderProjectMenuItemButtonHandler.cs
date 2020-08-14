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
  internal class FolderProjectMenuItemButtonHandler
  {
    private readonly ILicenseHeaderExtension _licenseHeaderExtension;
    private readonly MenuItemButtonOperation _mode;

    private FolderProjectUpdateDialog _dialog;

    public FolderProjectMenuItemButtonHandler (ILicenseHeaderExtension licenseHeaderExtension, MenuItemButtonOperation mode)
    {
      _licenseHeaderExtension = licenseHeaderExtension;
      _mode = mode;
    }

    public void HandleButton (object sender, EventArgs e)
    {
      var folderProjectUpdateViewModel = new FolderProjectUpdateViewModel();
      IMenuItemButtonHandler handler;
      switch (_mode)
      {
        case MenuItemButtonOperation.Add:
          handler = new AddLicenseHeaderToAllFilesInFolderProjectHelper (_licenseHeaderExtension, folderProjectUpdateViewModel);
          break;
        case MenuItemButtonOperation.Remove:
          handler = new RemoveLicenseHeaderToAllFilesInFolderProjectHelper (_licenseHeaderExtension, folderProjectUpdateViewModel);
          break;
        default:
          throw new ArgumentOutOfRangeException (nameof(_mode), _mode, null);
      }

      _dialog = new FolderProjectUpdateDialog (folderProjectUpdateViewModel);
      _dialog.Closing += DialogOnClosing;

      Task.Run (() => HandleButtonInternalAsync (handler)).FireAndForget();
      _dialog.ShowModal();
    }

    private async Task HandleButtonInternalAsync (IMenuItemButtonHandler handler)
    {
      try
      {
        await handler.ExecuteAsync (null, _dialog);
      }
      catch (Exception exception)
      {
        MessageBoxHelper.ShowMessage (
            $"The operation '{handler.Description}' failed with the exception '{exception.Message}'. See Visual Studio Output Window for Details.");
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