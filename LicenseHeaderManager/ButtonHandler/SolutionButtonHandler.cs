using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Core;
using EnvDTE;
using EnvDTE80;
using LicenseHeaderManager.Interfaces;
using LicenseHeaderManager.MenuItemCommands.Common;
using LicenseHeaderManager.UpdateViewModels;
using LicenseHeaderManager.UpdateViews;
using LicenseHeaderManager.Utils;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace LicenseHeaderManager.ButtonHandler
{
  internal class SolutionButtonHandler
  {
    private readonly DTE2 _dte2;
    private readonly ButtonOperation _operation;
    private readonly LicenseHeaderReplacer _licenseHeaderReplacer;

    private SolutionUpdateDialog _dialog;
    private bool _resharperSuspended;

    public SolutionButtonHandler (LicenseHeaderReplacer licenseHeaderReplacer, DTE2 dte2, ButtonOperation operation)
    {
      _licenseHeaderReplacer = licenseHeaderReplacer;
      _dte2 = dte2;
      _operation = operation;
    }

    public void HandleButton(object sender, EventArgs e)
    {
      var solutionUpdateViewModel = new SolutionUpdateViewModel();
      IButtonCommand command;
      switch (_operation)
      {
        case ButtonOperation.Add:
          command = new AddLicenseHeaderToAllFilesInSolutionHelper(_licenseHeaderReplacer, solutionUpdateViewModel);
          break;
        case ButtonOperation.Remove:
          command = new RemoveLicenseHeaderFromAllFilesInSolutionHelper(_licenseHeaderReplacer, solutionUpdateViewModel);
          break;
        default:
          throw new ArgumentOutOfRangeException(nameof(_operation), _operation, null);
      }

      _dialog = new SolutionUpdateDialog(solutionUpdateViewModel);
      _dialog.Closing += DialogOnClosing;
      _resharperSuspended = CommandUtility.ExecuteCommandIfExists("ReSharper_Suspend", _dte2);

      Task.Run(() => HandleButtonInternalAsync(_dte2.Solution, command)).FireAndForget();
      _dialog.ShowModal();
    }

    private async Task HandleButtonInternalAsync(object solutionObject, IButtonCommand command)
    {
      if (!(solutionObject is Solution solution))
        return;

      try
      {
        await command.ExecuteAsync(solution, _dialog);
      }
      catch (Exception exception)
      {
        MessageBoxHelper.ShowInformation(
            $"The command '{command.GetCommandName()}' failed with the exception '{exception.Message}'. See Visual Studio Output Window for Details.");
        OutputWindowHandler.WriteMessage(exception.ToString());
      }

      await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
      _dialog.Close();

      ResumeReSharper();
    }

    private void DialogOnClosing(object sender, CancelEventArgs e)
    {
      // TODO how to cancel Core operation?

      ResumeReSharper();
    }

    private void ResumeReSharper()
    {
      if (_resharperSuspended)
        CommandUtility.ExecuteCommand("ReSharper_Resume", _dte2);
    }
  }
}
