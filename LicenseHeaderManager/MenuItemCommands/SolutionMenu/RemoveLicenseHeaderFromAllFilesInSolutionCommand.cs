using System;
using System.ComponentModel.Design;
using LicenseHeaderManager.MenuItemCommands.Common;
using LicenseHeaderManager.Utils;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace LicenseHeaderManager.MenuItemCommands.SolutionMenu
{
  /// <summary>
  /// Command handler
  /// </summary>
  internal sealed class RemoveLicenseHeaderFromAllFilesInSolutionCommand
  {
    /// <summary>
    /// Command ID.
    /// </summary>
    public const int CommandId = 4131;

    /// <summary>
    /// Command menu group (command set GUID).
    /// </summary>
    public static readonly Guid CommandSet = new Guid ("1a75d6da-3b30-4ec9-81ae-72b8b7eba1a0");

    /// <summary>
    /// Initializes a new instance of the <see cref="RemoveLicenseHeaderFromAllFilesInSolutionCommand"/> class.
    /// Adds our command handlers for menu (commands must exist in the command table file)
    /// </summary>
    /// <param name="package">Owner package, not null.</param>
    /// <param name="commandService">Command service to add command to, not null.</param>
    private RemoveLicenseHeaderFromAllFilesInSolutionCommand (AsyncPackage package, OleMenuCommandService commandService)
    {
      ServiceProvider = (LicenseHeadersPackage) package ?? throw new ArgumentNullException (nameof(package));
      commandService = commandService ?? throw new ArgumentNullException (nameof(commandService));

      var menuCommandID = new CommandID (CommandSet, CommandId);
      var menuItem = new OleMenuCommand (this.Execute, menuCommandID);
      commandService.AddCommand (menuItem);
    }

    /// <summary>
    /// Gets the instance of the command.
    /// </summary>
    public static RemoveLicenseHeaderFromAllFilesInSolutionCommand Instance { get; private set; }

    /// <summary>
    /// Gets the service provider from the owner package.
    /// </summary>
    private LicenseHeadersPackage ServiceProvider { get; }

    /// <summary>
    /// Initializes the singleton instance of the command.
    /// </summary>
    /// <param name="package">Owner package, not null.</param>
    public static async Task InitializeAsync (AsyncPackage package)
    {
      // Switch to the main thread - the call to AddCommand in RemoveLicenseHeaderFromAllFilesInSolutionCommand's constructor requires
      // the UI thread.
      await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync (package.DisposalToken);

      var commandService = await package.GetServiceAsync (typeof (IMenuCommandService)) as OleMenuCommandService;
      Instance = new RemoveLicenseHeaderFromAllFilesInSolutionCommand (package, commandService);
    }

    /// <summary>
    /// This function is the callback used to execute the command when the menu item is clicked.
    /// See the constructor to see how the menu item is associated with this function using
    /// OleMenuCommandService service and MenuCommand class.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Event args.</param>
    private void Execute (object sender, EventArgs e)
    {
      ThreadHelper.ThrowIfNotOnUIThread();

      ExecuteInternalAsync().FireAndForget();
    }

    private async Task ExecuteInternalAsync ()
    {
      await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
      var solution = ServiceProvider._dte.Solution;
      var statusBar = (IVsStatusbar) await ServiceProvider.GetServiceAsync (typeof (SVsStatusbar));
      var removeLicenseHeaderFromAllProjects = new RemoveLicenseHeaderFromAllFilesInSolutionHelper (statusBar, ServiceProvider._licenseReplacer);
      var resharperSuspended = CommandUtility.ExecuteCommandIfExists ("ReSharper_Suspend", ServiceProvider._dte);

      try
      {
        removeLicenseHeaderFromAllProjects.Execute (solution);
      }
      catch (Exception exception)
      {
        MessageBoxHelper.Information (
            $"The command '{removeLicenseHeaderFromAllProjects.GetCommandName()}' failed with the exception '{exception.Message}'. See Visual Studio Output Window for Details.");
        OutputWindowHandler.WriteMessage (exception.ToString());
      }

      if (resharperSuspended)
      {
        CommandUtility.ExecuteCommand ("ReSharper_Resume", ServiceProvider._dte);
      }
    }
  }
}