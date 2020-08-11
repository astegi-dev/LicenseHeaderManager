using System;
using System.ComponentModel.Design;
using LicenseHeaderManager.ButtonHandler;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace LicenseHeaderManager.MenuItemCommands.SolutionMenu
{
  /// <summary>
  ///   Command handler
  /// </summary>
  internal sealed class AddLicenseHeaderToAllFilesInSolutionCommand
  {
    /// <summary>
    ///   Command ID.
    /// </summary>
    public const int CommandId = 4130;

    /// <summary>
    ///   Command menu group (command set GUID).
    /// </summary>
    public static readonly Guid CommandSet = new Guid ("1a75d6da-3b30-4ec9-81ae-72b8b7eba1a0");

    private readonly ButtonHandlerFactory _buttonHandlerFactory;

    /// <summary>
    ///   Initializes a new instance of the <see cref="AddLicenseHeaderToAllFilesInSolutionCommand" /> class.
    ///   Adds our command handlers for menu (commands must exist in the command table file)
    /// </summary>
    /// <param name="package">Owner package, not null.</param>
    /// <param name="commandService">Command service to add command to, not null.</param>
    private AddLicenseHeaderToAllFilesInSolutionCommand (AsyncPackage package, OleMenuCommandService commandService)
    {
      ServiceProvider = (LicenseHeadersPackage) package ?? throw new ArgumentNullException (nameof(package));
      commandService = commandService ?? throw new ArgumentNullException (nameof(commandService));

      _buttonHandlerFactory = new ButtonHandlerFactory (ServiceProvider);

      var menuCommandID = new CommandID (CommandSet, CommandId);
      var menuItem = new OleMenuCommand (Execute, menuCommandID);
      commandService.AddCommand (menuItem);
    }

    /// <summary>
    ///   Gets the instance of the command.
    /// </summary>
    public static AddLicenseHeaderToAllFilesInSolutionCommand Instance { get; private set; }

    /// <summary>
    ///   Gets the service provider from the owner package.
    /// </summary>
    private LicenseHeadersPackage ServiceProvider { get; }

    /// <summary>
    ///   Initializes the singleton instance of the command.
    /// </summary>
    /// <param name="package">Owner package, not null.</param>
    public static async Task InitializeAsync (AsyncPackage package)
    {
      // Switch to the main thread - the call to AddCommand in AddLicenceHeaderToAllFilesInSolutionCommandAsync's constructor requires
      // the UI thread.
      await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync (package.DisposalToken);

      var commandService = await package.GetServiceAsync (typeof (IMenuCommandService)) as OleMenuCommandService;
      Instance = new AddLicenseHeaderToAllFilesInSolutionCommand (package, commandService);
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

      _buttonHandlerFactory.CreateAddLicenseHeaderToSolutionHandler().HandleButton (sender, e);
    }
  }
}