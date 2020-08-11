using System;
using System.ComponentModel.Design;
using LicenseHeaderManager.Options;
using Microsoft.VisualStudio.Shell;
using Microsoft.Win32;
using Task = System.Threading.Tasks.Task;

namespace LicenseHeaderManager.MenuItemCommands.Temp
{
  /// <summary>
  ///   Command handler
  /// </summary>
  internal sealed class LoadSettingsCommand
  {
    /// <summary>
    ///   Command ID.
    /// </summary>
    public const int CommandId = 4147;

    /// <summary>
    ///   Command menu group (command set GUID).
    /// </summary>
    public static readonly Guid CommandSet = new Guid ("1a75d6da-3b30-4ec9-81ae-72b8b7eba1a0");

    /// <summary>
    ///   VS Package that provides this command, not null.
    /// </summary>
    private readonly AsyncPackage package;

    /// <summary>
    ///   Initializes a new instance of the <see cref="LoadSettingsCommand" /> class.
    ///   Adds our command handlers for menu (commands must exist in the command table file)
    /// </summary>
    /// <param name="package">Owner package, not null.</param>
    /// <param name="commandService">Command service to add command to, not null.</param>
    private LoadSettingsCommand (AsyncPackage package, OleMenuCommandService commandService)
    {
      this.package = package ?? throw new ArgumentNullException (nameof(package));
      commandService = commandService ?? throw new ArgumentNullException (nameof(commandService));

      var menuCommandID = new CommandID (CommandSet, CommandId);
      var menuItem = new MenuCommand (Execute, menuCommandID);
      commandService.AddCommand (menuItem);
    }

    /// <summary>
    ///   Gets the instance of the command.
    /// </summary>
    public static LoadSettingsCommand Instance { get; private set; }

    /// <summary>
    ///   Gets the service provider from the owner package.
    /// </summary>
    private IAsyncServiceProvider ServiceProvider => package;

    /// <summary>
    ///   Initializes the singleton instance of the command.
    /// </summary>
    /// <param name="package">Owner package, not null.</param>
    public static async Task InitializeAsync (AsyncPackage package)
    {
      // Switch to the main thread - the call to AddCommand in LoadSettingsCommand's constructor requires
      // the UI thread.
      await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync (package.DisposalToken);

      var commandService = await package.GetServiceAsync (typeof (IMenuCommandService)) as OleMenuCommandService;
      Instance = new LoadSettingsCommand (package, commandService);
    }

    /// <summary>
    ///   This function is the callback used to execute the command when the menu item is clicked.
    ///   See the constructor to see how the menu item is associated with this function using
    ///   OleMenuCommandService service and MenuCommand class.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Event args.</param>
    private void Execute (object sender, EventArgs e)
    {
      ThreadHelper.ThrowIfNotOnUIThread();

      var dlg = new OpenFileDialog
                {
                    Title = "Select config file to open...",
                    Filter = "LHM Config Files (*.json)|*.json"
                };
      if (dlg.ShowDialog() != true)
        return;

      OptionsStore.CurrentConfig = OptionsStore.Load (dlg.FileName);
    }
  }
}