using System;
using System.ComponentModel.Design;
using System.IO;
using System.Text;
using EnvDTE;
using LicenseHeaderManager.Headers;
using LicenseHeaderManager.Utils;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace LicenseHeaderManager.MenuItemCommands.SolutionMenu
{
  /// <summary>
  ///   Command handler
  /// </summary>
  internal sealed class AddNewSolutionLicenseHeaderDefinitionFileCommand
  {
    /// <summary>
    ///   Command ID.
    /// </summary>
    public const int CommandId = 4132;

    /// <summary>
    ///   Command menu group (command set GUID).
    /// </summary>
    public static readonly Guid CommandSet = new Guid ("1a75d6da-3b30-4ec9-81ae-72b8b7eba1a0");

    private readonly Func<string> _defaultHeaderDefinitionFunc;

    private readonly OleMenuCommand _menuItem;
    private readonly Solution _solution;

    /// <summary>
    ///   Initializes a new instance of the <see cref="AddNewSolutionLicenseHeaderDefinitionFileCommand" /> class.
    ///   Adds our command handlers for menu (commands must exist in the command table file)
    /// </summary>
    /// <param name="package">Owner package, not null.</param>
    /// <param name="commandService">Command service to add command to, not null.</param>
    /// <param name="solution">The solution for which <paramref name="package" /> has been initialized</param>
    /// <param name="defaultHeaderDefinitionFunc">
    ///   Function that returns the currently configured default license header
    ///   definition as string
    /// </param>
    private AddNewSolutionLicenseHeaderDefinitionFileCommand (
        AsyncPackage package,
        OleMenuCommandService commandService,
        Solution solution,
        Func<string> defaultHeaderDefinitionFunc)
    {
      ServiceProvider = (LicenseHeadersPackage) package ?? throw new ArgumentNullException (nameof(package));
      commandService = commandService ?? throw new ArgumentNullException (nameof(commandService));

      _solution = solution;
      _defaultHeaderDefinitionFunc = defaultHeaderDefinitionFunc;

      var menuCommandID = new CommandID (CommandSet, CommandId);
      _menuItem = new OleMenuCommand (Execute, menuCommandID);
      _menuItem.BeforeQueryStatus += OnQuerySolutionCommandStatus;
      commandService.AddCommand (_menuItem);
    }

    /// <summary>
    ///   Gets the instance of the command.
    /// </summary>
    public static AddNewSolutionLicenseHeaderDefinitionFileCommand Instance { get; private set; }

    /// <summary>
    ///   Gets the service provider from the owner package.
    /// </summary>
    private LicenseHeadersPackage ServiceProvider { get; }

    private void OnQuerySolutionCommandStatus (object sender, EventArgs e)
    {
      _menuItem.Enabled = !ServiceProvider.SolutionHeaderDefinitionExists();
    }

    /// <summary>
    ///   Initializes the singleton instance of the command.
    /// </summary>
    /// <param name="package">Owner package, not null.</param>
    /// <param name="solution">The solution for which <paramref name="package" /> has been initialized</param>
    /// <param name="defaultHeaderDefinitionFunc">
    ///   Function that returns the currently configured default license header
    ///   definition as string
    /// </param>
    public static async Task InitializeAsync (AsyncPackage package, Solution solution, Func<string> defaultHeaderDefinitionFunc)
    {
      // Switch to the main thread - the call to AddCommand in AddNewSolutionLicenseHeaderDefinitionFileCommand's constructor requires
      // the UI thread.
      await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync (package.DisposalToken);

      var commandService = await package.GetServiceAsync (typeof (IMenuCommandService)) as OleMenuCommandService;
      Instance = new AddNewSolutionLicenseHeaderDefinitionFileCommand (package, commandService, solution, defaultHeaderDefinitionFunc);
    }

    public void Invoke (Solution solution)
    {
      ExecuteInternalAsync (solution).FireAndForget();
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

      ExecuteInternalAsync (_solution).FireAndForget();
    }

    private async Task ExecuteInternalAsync (Solution solution)
    {
      await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

      var solutionHeaderDefinitionFilePath = LicenseHeader.GetHeaderDefinitionFilePathForSolution (solution);

      // Add file
      var defaultLicenseHeaderFileText = _defaultHeaderDefinitionFunc();
      File.WriteAllText (solutionHeaderDefinitionFilePath, defaultLicenseHeaderFileText, Encoding.UTF8);
      solution.DTE.OpenFile (Constants.vsViewKindTextView, solutionHeaderDefinitionFilePath).Activate();
    }
  }
}