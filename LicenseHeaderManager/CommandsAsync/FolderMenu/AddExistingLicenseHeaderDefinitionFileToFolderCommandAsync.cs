using System;
using System.ComponentModel.Design;
using System.Globalization;
using EnvDTE;
using LicenseHeaderManager.PackageCommands;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace LicenseHeaderManager.CommandsAsync.FolderMenu
{
  /// <summary>
  /// Command handler
  /// </summary>
  internal sealed class AddExistingLicenseHeaderDefinitionFileToFolderCommandAsync
  {
    /// <summary>
    /// Command ID.
    /// </summary>
    public const int CommandId = 4142;

    /// <summary>
    /// Command menu group (command set GUID).
    /// </summary>
    public static readonly Guid CommandSet = new Guid ("1a75d6da-3b30-4ec9-81ae-72b8b7eba1a0");

    /// <summary>
    /// Initializes a new instance of the <see cref="AddExistingLicenseHeaderDefinitionFileToFolderCommandAsync"/> class.
    /// Adds our command handlers for menu (commands must exist in the command table file)
    /// </summary>
    /// <param name="package">Owner package, not null.</param>
    /// <param name="commandService">Command service to add command to, not null.</param>
    private AddExistingLicenseHeaderDefinitionFileToFolderCommandAsync (AsyncPackage package, OleMenuCommandService commandService)
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
    public static AddExistingLicenseHeaderDefinitionFileToFolderCommandAsync Instance { get; private set; }

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
      // Switch to the main thread - the call to AddCommand in AddExistingLicenseHeaderDefinitionFileToFolderCommandAsync's constructor requires
      // the UI thread.
      await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync (package.DisposalToken);

      var commandService = await package.GetServiceAsync (typeof (IMenuCommandService)) as OleMenuCommandService;
      Instance = new AddExistingLicenseHeaderDefinitionFileToFolderCommandAsync (package, commandService);
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

      var project = ServiceProvider.GetSolutionExplorerItem() as Project;
      var projectItem = ServiceProvider.GetSolutionExplorerItem() as ProjectItem;

      string fileName;

      if (project != null)
      {
        fileName = project.FileName;
      }
      else if (projectItem != null)
      {
        fileName = projectItem.Name;
      }
      else
      {
        return;
      }

      ProjectItems projectItems = null;

      if (project != null)
      {
        projectItems = project.ProjectItems;
      }
      else if (projectItem != null)
      {
        projectItems = projectItem.ProjectItems;
      }

      new AddExistingLicenseHeaderDefinitionFileToProjectCommand().AddDefinitionFileToOneProject (fileName, projectItems);
    }
  }
}