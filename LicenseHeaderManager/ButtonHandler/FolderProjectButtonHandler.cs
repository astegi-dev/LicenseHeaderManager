using LicenseHeaderManager.Interfaces;
using LicenseHeaderManager.MenuItemCommands.Common;
using LicenseHeaderManager.UpdateViewModels;
using LicenseHeaderManager.UpdateViews;
using LicenseHeaderManager.Utils;
using System;
using System.ComponentModel;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace LicenseHeaderManager.ButtonHandler
{
  class FolderProjectButtonHandler
  {
    private readonly ILicenseHeaderExtension _licenseHeaderExtension;
    private readonly ButtonOperation _operation;

    private FolderProjectUpdateDialog _dialog;
    
    public FolderProjectButtonHandler(ILicenseHeaderExtension licenseHeaderExtension, ButtonOperation operation)
    {
      _licenseHeaderExtension = licenseHeaderExtension;
      _operation = operation;
    }

    public void HandleButton(object sender, EventArgs e)
    {
      var folderProjectUpdateViewModel = new FolderProjectUpdateViewModel();
      IButtonCommand command;
      switch (_operation)
      {
        case ButtonOperation.Add:
          command = new AddLicenseHeaderToAllFilesInFolderProjectHelper(_licenseHeaderExtension, folderProjectUpdateViewModel);
          break;
        case ButtonOperation.Remove:
          command = new RemoveLicenseHeaderToAllFilesInFolderProjectHelper(_licenseHeaderExtension, folderProjectUpdateViewModel);
          break;
        default:
          throw new ArgumentOutOfRangeException(nameof(_operation), _operation, null);
      }

      _dialog = new FolderProjectUpdateDialog(folderProjectUpdateViewModel);
      _dialog.Closing += DialogOnClosing;

      Task.Run(() => HandleButtonInternalAsync(command)).FireAndForget();
      _dialog.ShowModal();
    }

    private async Task HandleButtonInternalAsync (IButtonCommand command)
    {
      try
      {
        await command.ExecuteAsync(null, _dialog);
      }
      catch (Exception exception)
      {
        MessageBoxHelper.ShowInformation(
            $"The command '{command.GetCommandName()}' failed with the exception '{exception.Message}'. See Visual Studio Output Window for Details.");
        OutputWindowHandler.WriteMessage(exception.ToString());
      }

      await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
      _dialog.Close();
    }

    private void DialogOnClosing(object sender, CancelEventArgs e)
    {
      // TODO how to cancel Core operation?

    }
  }
}
