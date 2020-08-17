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
using System.Threading;
using System.Windows.Input;
using LicenseHeaderManager.Interfaces;
using LicenseHeaderManager.UpdateViewModels;
using LicenseHeaderManager.UpdateViews;
using LicenseHeaderManager.Utils;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace LicenseHeaderManager.MenuItemButtonHandler
{
  internal class FolderProjectMenuItemButtonHandler : IMenuItemButtonHandler
  {
    private readonly MenuItemButtonHandlerHelper _handler;
    private FolderProjectUpdateDialog _dialog;
    private CancellationTokenSource _cancellationTokenSource;

    public FolderProjectMenuItemButtonHandler (MenuItemButtonOperation mode, MenuItemButtonLevel level, MenuItemButtonHandlerHelper handler)
    {
      Mode = mode;
      Level = level;
      _handler = handler;
    }

    public MenuItemButtonLevel Level { get; }

    public MenuItemButtonOperation Mode { get; }

    public void HandleButton (object sender, EventArgs e)
    {
      Mouse.OverrideCursor = Cursors.Wait;
      var folderProjectUpdateViewModel = new FolderProjectUpdateViewModel();

      _dialog = new FolderProjectUpdateDialog (folderProjectUpdateViewModel) { WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner };
      _dialog.Closing += DialogOnClosing;

      Task.Run (() => HandleButtonInternalAsync (_handler, folderProjectUpdateViewModel)).FireAndForget();
      Mouse.OverrideCursor = null;
      _dialog.ShowModal();
    }

    private async Task HandleButtonInternalAsync (MenuItemButtonHandlerHelper handler, BaseUpdateViewModel folderProjectUpdateViewModel)
    {
      _cancellationTokenSource = new CancellationTokenSource();
      try
      {
        await handler.DoWorkAsync (_cancellationTokenSource.Token, folderProjectUpdateViewModel, null, _dialog);
      }
      catch (OperationCanceledException)
      {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        _dialog.Close();
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
      _cancellationTokenSource?.Cancel(true);
    }
  }
}