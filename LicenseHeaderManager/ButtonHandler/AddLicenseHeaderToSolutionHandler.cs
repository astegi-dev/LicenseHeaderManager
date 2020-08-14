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
using Thread = EnvDTE.Thread;

namespace LicenseHeaderManager.ButtonHandler
{
  public class AddLicenseHeaderToSolutionHandler
  {
    private readonly DTE2 _dte2;
    private readonly LicenseHeaderReplacer _licenseHeaderReplacer;

    private SolutionUpdateDialog _dialog;
    private bool _resharperSuspended;

    public AddLicenseHeaderToSolutionHandler (LicenseHeaderReplacer licenseHeaderReplacer, DTE2 dte2)
    {
      _licenseHeaderReplacer = licenseHeaderReplacer;
      _dte2 = dte2;
    }

    public void HandleButton (object sender, EventArgs e)
    {
      var solutionUpdateViewModel = new SolutionUpdateViewModel();
      var addHeaderToAllProjectsCommand = new AddLicenseHeaderToAllFilesInSolutionHelper (_licenseHeaderReplacer, solutionUpdateViewModel);

      _dialog = new SolutionUpdateDialog (solutionUpdateViewModel);
      _dialog.Closing += DialogOnClosing;
      _resharperSuspended = CommandUtility.ExecuteCommandIfExists ("ReSharper_Suspend", _dte2);

      Task.Run (() => HandleButtonInternalAsync (_dte2.Solution, addHeaderToAllProjectsCommand)).FireAndForget();
      _dialog.ShowModal();
    }

    public async Task HandleButtonInternalAsync (object solutionObject, ISolutionLevelCommand command)
    {
      if (!(solutionObject is Solution solution))
        return;

      try
      {
        await command.ExecuteAsync (solution, _dialog);
      }
      catch (Exception exception)
      {
        MessageBoxHelper.ShowInformation (
            $"The command '{command.GetCommandName()}' failed with the exception '{exception.Message}'. See Visual Studio Output Window for Details.");
        OutputWindowHandler.WriteMessage (exception.ToString());
      }

      await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
      _dialog.Close();

      ResumeReSharper();
    }

    private void DialogOnClosing (object sender, CancelEventArgs cancelEventArgs)
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