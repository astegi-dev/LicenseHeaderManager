using System;
using System.ComponentModel.Design;
using System.Windows;
using Core;
using LicenseHeaderManager.Utils;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace LicenseHeaderManager.MenuItemCommands.EditorMenu
{
  /// <summary>
  /// Command handler
  /// </summary>
  internal sealed class RemoveLicenseHeaderEditorAdvancedMenuCommand
  {
    /// <summary>
    /// Command ID.
    /// </summary>
    public const int CommandId = 4145;

    /// <summary>
    /// Command menu group (command set GUID).
    /// </summary>
    public static readonly Guid CommandSet = new Guid ("1a75d6da-3b30-4ec9-81ae-72b8b7eba1a0");

    private readonly OleMenuCommand _menuItem;

    /// <summary>
    /// Initializes a new instance of the <see cref="RemoveLicenseHeaderEditorAdvancedMenuCommand"/> class.
    /// Adds our command handlers for menu (commands must exist in the command table file)
    /// </summary>
    /// <param name="package">Owner package, not null.</param>
    /// <param name="commandService">Command service to add command to, not null.</param>
    private RemoveLicenseHeaderEditorAdvancedMenuCommand (AsyncPackage package, OleMenuCommandService commandService)
    {
      ServiceProvider = (LicenseHeadersPackage) package ?? throw new ArgumentNullException (nameof(package));
      commandService = commandService ?? throw new ArgumentNullException (nameof(commandService));

      var menuCommandID = new CommandID (CommandSet, CommandId);
      _menuItem = new OleMenuCommand (Execute, menuCommandID);
      _menuItem.BeforeQueryStatus += OnQueryEditCommandStatus;
      commandService.AddCommand (_menuItem);
    }

    private void OnQueryEditCommandStatus (object sender, EventArgs e)
    {
      var visible = false;

      var item = ServiceProvider.GetActiveProjectItem();
      if (item != null)
      {
        visible = ServiceProvider.ShouldBeVisible (item);
      }

      _menuItem.Visible = visible;
    }

    /// <summary>
    /// Gets the instance of the command.
    /// </summary>
    public static RemoveLicenseHeaderEditorAdvancedMenuCommand Instance { get; private set; }

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
      // Switch to the main thread - the call to AddCommand in RemoveLicenseHeaderEditorAdvancedMenuCommand's constructor requires
      // the UI thread.
      await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync (package.DisposalToken);

      var commandService = await package.GetServiceAsync (typeof (IMenuCommandService)) as OleMenuCommandService;
      Instance = new RemoveLicenseHeaderEditorAdvancedMenuCommand (package, commandService);
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

      var item = ServiceProvider.GetActiveProjectItem();
      if (item == null)
        return;

      ExecuteInternalAsync (item.Document.FullName).FireAndForget();
    }

    private async Task ExecuteInternalAsync (string path)
    {
      var result = await ServiceProvider.LicenseHeaderReplacer.RemoveOrReplaceHeader (
          new LicenseHeaderInput (path, null),
          true,
          CoreHelpers.NonCommentLicenseHeaderDefinitionInquiry,
          message => CoreHelpers.NoLicenseHeaderDefinitionFound (message, ServiceProvider));
      if (!string.IsNullOrEmpty (result))
        MessageBox.Show ($"Error: {result}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }
  }
}