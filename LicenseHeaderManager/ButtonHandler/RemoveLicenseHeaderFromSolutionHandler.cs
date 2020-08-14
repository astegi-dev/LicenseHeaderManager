using System;
using System.ComponentModel;
using Core;
using EnvDTE;
using EnvDTE80;
using LicenseHeaderManager.Interfaces;
using LicenseHeaderManager.MenuItemCommands.Common;
using LicenseHeaderManager.SolutionUpdateViewModels;
using LicenseHeaderManager.Utils;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace LicenseHeaderManager.ButtonHandler
{
  internal class RemoveLicenseHeaderFromSolutionHandler
  {
    private readonly DTE2 _dte2;
    private readonly LicenseHeaderReplacer _licenseHeaderReplacer;

    private SolutionUpdateDialog _dialog;
    private bool _resharperSuspended;

    public RemoveLicenseHeaderFromSolutionHandler (LicenseHeaderReplacer licenseHeaderReplacer, DTE2 dte2)
    {
      _licenseHeaderReplacer = licenseHeaderReplacer;
      _dte2 = dte2;
    }

    public void HandleButton (object sender, EventArgs e)
    {
      var solutionUpdateViewModel = new SolutionUpdateViewModel();
      var removeHeaderFromAllProjectsCommand = new RemoveLicenseHeaderFromAllFilesInSolutionHelper (solutionUpdateViewModel, _licenseHeaderReplacer);

      _dialog = new SolutionUpdateDialog (solutionUpdateViewModel);
      _dialog.Closing += DialogOnClosing;
      _resharperSuspended = CommandUtility.ExecuteCommandIfExists ("ReSharper_Suspend", _dte2);

      Task.Run (() => HandleButtonInternalAsync (_dte2.Solution, removeHeaderFromAllProjectsCommand)).FireAndForget();
      _dialog.ShowModal();
    }

    private async Task HandleButtonInternalAsync (object solutionObject, ISolutionLevelCommand command)
    {
      if (!(solutionObject is Solution solution))
        return;

      try
      {
        await command.ExecuteAsync (solution);
      }
      catch (Exception exception)
      {
        MessageBoxHelper.Information (
            $"The command '{command.GetCommandName()}' failed with the exception '{exception.Message}'. See Visual Studio Output Window for Details.");
        OutputWindowHandler.WriteMessage (exception.ToString());
      }

      await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
      _dialog.Close();

      ResumeReSharper();
    }

    private void DialogOnClosing (object sender, CancelEventArgs e)
    {
      // TODO how to cancel Core operation?

      ResumeReSharper();
    }

    private void ResumeReSharper ()
    {
      if (_resharperSuspended)
        CommandUtility.ExecuteCommand ("ReSharper_Resume", _dte2);
    }
  }
}