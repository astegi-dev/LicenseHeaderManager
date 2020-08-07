using System;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using EnvDTE;
using LicenseHeaderManager.Headers;
using LicenseHeaderManager.Options;
using LicenseHeaderManager.Utils;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using Task = System.Threading.Tasks.Task;

namespace LicenseHeaderManager.CommandsAsync.ProjectItemMenu
{
  /// <summary>
  /// Command handler
  /// </summary>
  internal sealed class AddHeaderToProjectItemCommandAsync
  {
    /// <summary>
    /// Command ID.
    /// </summary>
    public const int CommandId = 256;

    /// <summary>
    /// Command menu group (command set GUID).
    /// </summary>
    public static readonly Guid CommandSet = new Guid ("1a75d6da-3b30-4ec9-81ae-72b8b7eba1a0");

    private readonly OleMenuCommand _menuItem;

    /// <summary>
    /// Initializes a new instance of the <see cref="AddHeaderToProjectItemCommandAsync"/> class.
    /// Adds our command handlers for menu (commands must exist in the command table file)
    /// </summary>
    /// <param name="package">Owner package, not null.</param>
    /// <param name="commandService">Command service to add command to, not null.</param>
    private AddHeaderToProjectItemCommandAsync (AsyncPackage package, OleMenuCommandService commandService)
    {
      ServiceProvider = (LicenseHeadersPackage) package ?? throw new ArgumentNullException (nameof(package));
      commandService = commandService ?? throw new ArgumentNullException (nameof(commandService));

      var menuCommandID = new CommandID (CommandSet, CommandId);
      _menuItem = new OleMenuCommand (this.Execute, menuCommandID);
      _menuItem.BeforeQueryStatus += OnQueryProjectItemCommandStatus;
      commandService.AddCommand (_menuItem);
    }

    private void OnQueryProjectItemCommandStatus(object sender, EventArgs e)
    {
      var visible = false;

      if (ServiceProvider.GetSolutionExplorerItem() is ProjectItem item)
        visible = ServiceProvider.ShouldBeVisible(item);

      _menuItem.Visible = visible;
    }

    /// <summary>
    /// Gets the instance of the command.
    /// </summary>
    public static AddHeaderToProjectItemCommandAsync Instance { get; private set; }

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
      // Switch to the main thread - the call to AddCommand in AddHeaderToProjectItemCommandAsync's constructor requires
      // the UI thread.
      await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync (package.DisposalToken);

      OleMenuCommandService commandService = await package.GetServiceAsync (typeof (IMenuCommandService)) as OleMenuCommandService;
      Instance = new AddHeaderToProjectItemCommandAsync (package, commandService);
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
      ThreadHelper.ThrowIfNotOnUIThread ();

      if (!(e is OleMenuCmdEventArgs args))
        return;

      ExecuteInternalAsync (args).FireAndForget();
    }

    private async Task ExecuteInternalAsync (OleMenuCmdEventArgs args)
    {
      await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

      var item = args.InValue as ProjectItem ?? ServiceProvider.GetSolutionExplorerItem() as ProjectItem;

      if (item == null || !ProjectItemInspection.IsPhysicalFile (item) || ProjectItemInspection.IsLicenseHeader (item))
        return;

      var headers = LicenseHeaderFinder.GetHeaderDefinitionForItem (item);
      var keywords = ServiceProvider.OptionsPage.UseRequiredKeywords
          ? ServiceProvider.OptionsPage.RequiredKeywords.Split (new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select (k => k.Trim())
          : null;
      var replacer = new Core.LicenseHeaderReplacer (
          ServiceProvider.LanguagesPage.Languages.Select (
              x => new Core.Language
                   {
                       Extensions = x.Extensions, BeginComment = x.BeginComment, BeginRegion = x.BeginRegion, EndComment = x.EndComment, EndRegion = x.EndRegion,
                       LineComment = x.LineComment, SkipExpression = x.SkipExpression
                   }),
          keywords);

      var result = await replacer.RemoveOrReplaceHeader (item.Document.FullName, headers, true);
      if (!string.IsNullOrEmpty (result))
        MessageBox.Show ($"Error: {result}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }
  }
}