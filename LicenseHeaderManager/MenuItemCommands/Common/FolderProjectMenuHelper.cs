// first line LicenseHeaderManager,LicenseHeaderManager.CommandsAsync.Common
// second line copyright123

// first line
// second line copyright456

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using EnvDTE;
using LicenseHeaderManager.Headers;
using LicenseHeaderManager.Interfaces;
using LicenseHeaderManager.MenuItemCommands.SolutionMenu;
using LicenseHeaderManager.ResultObjects;
using LicenseHeaderManager.Utils;
using Microsoft;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Constants = EnvDTE.Constants;
using Task = System.Threading.Tasks.Task;

namespace LicenseHeaderManager.MenuItemCommands.Common
{
  internal static class FolderProjectMenuHelper
  {
    public static void AddExistingLicenseHeaderDefinitionFile (LicenseHeadersPackage serviceProvider)
    {
      ThreadHelper.ThrowIfNotOnUIThread();

      var project = serviceProvider.GetSolutionExplorerItem() as Project;
      var projectItem = serviceProvider.GetSolutionExplorerItem() as ProjectItem;

      string fileName;
      if (project != null)
        fileName = project.FileName;
      else if (projectItem != null)
        fileName = projectItem.Name;
      else
        return;

      ProjectItems projectItems = null;
      if (project != null)
        projectItems = project.ProjectItems;
      else if (projectItem != null)
        projectItems = projectItem.ProjectItems;

      ExistingLicenseHeaderDefinitionFileAdder.AddDefinitionFileToOneProject (fileName, projectItems);
    }

    public static async Task AddLicenseHeaderToAllFilesAsync (LicenseHeadersPackage serviceProvider)
    {
      await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
      var obj = serviceProvider.GetSolutionExplorerItem();
      var addLicenseHeaderToAllFilesCommand = new AddLicenseHeaderToAllFilesInProjectHelper (serviceProvider.LicenseHeaderReplacer);

      var statusBar = (IVsStatusbar) await serviceProvider.GetServiceAsync (typeof (SVsStatusbar));
      Assumes.Present (statusBar);

      statusBar.SetText (Resources.UpdatingFiles);
      var addLicenseHeaderToAllFilesReturn = await addLicenseHeaderToAllFilesCommand.ExecuteAsync (obj);
      statusBar.SetText (string.Empty);

      await HandleLinkedFilesAndShowMessageBoxAsync (serviceProvider, addLicenseHeaderToAllFilesReturn.LinkedItems);
      await HandleAddLicenseHeaderToAllFilesInProjectResultAsync (serviceProvider, obj, addLicenseHeaderToAllFilesReturn);
    }

    public static void AddNewLicenseHeaderDefinitionFile (LicenseHeadersPackage serviceProvider)
    {
      ThreadHelper.ThrowIfNotOnUIThread();

      var page = serviceProvider.DefaultLicenseHeaderPage;
      var solutionItem = serviceProvider.GetSolutionExplorerItem();
      var project = solutionItem as Project;
      if (project == null)
        if (solutionItem is ProjectItem projectItem)
          LicenseHeader.AddLicenseHeaderDefinitionFile (projectItem, page);

      if (project == null)
        return;

      var licenseHeaderDefinitionFile = LicenseHeader.AddHeaderDefinitionFile (project, page);
      licenseHeaderDefinitionFile.Open (Constants.vsViewKindCode).Activate();
    }

    public static async Task RemoveLicenseHeadersFromAllFilesAsync (LicenseHeadersPackage serviceProvider)
    {
      await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

      var obj = serviceProvider.GetSolutionExplorerItem();
      var removeAllLicenseHeadersCommand = new RemoveLicenseHeaderFromAllFilesInProjectHelper (serviceProvider.LicenseHeaderReplacer);

      var statusBar = (IVsStatusbar) await serviceProvider.GetServiceAsync (typeof (SVsStatusbar));
      Assumes.Present (statusBar);
      statusBar.SetText (Resources.UpdatingFiles);

      await removeAllLicenseHeadersCommand.ExecuteAsync (obj);

      statusBar.SetText (string.Empty);
    }

    private static async Task HandleAddLicenseHeaderToAllFilesInProjectResultAsync (
        LicenseHeadersPackage serviceProvider,
        object obj,
        AddLicenseHeaderToAllFilesResult addResult)
    {
      var project = obj as Project;
      var projectItem = obj as ProjectItem;
      if (project == null && projectItem == null)
        return;
      var currentProject = project;

      if (projectItem != null)
        currentProject = projectItem.ContainingProject;

      if (addResult.NoHeaderFound)
      {
        // No license header found...
        var solutionSearcher = new AllSolutionProjectsSearcher();
        var projects = solutionSearcher.GetAllProjects (serviceProvider.Dte2.Solution);

        if (projects.Any (projectInSolution => LicenseHeaderFinder.GetHeaderDefinitionForProjectWithoutFallback (projectInSolution) != null))
        {
          //TODO owner window as parameter
          // If another project has a license header, offer to add a link to the existing one.
          if (await MessageBoxHelper.AskYesNoAsync (Resources.Question_AddExistingDefinitionFileToProject, null).ConfigureAwait(true))
          {
            ExistingLicenseHeaderDefinitionFileAdder.AddDefinitionFileToOneProject (currentProject.FileName, currentProject.ProjectItems);
            await AddLicenseHeaderToAllFilesAsync (serviceProvider);
          }
        }
        else
        {
          // If no project has a license header, offer to add one for the solution.
          if (await MessageBoxHelper.AskYesNoAsync (Resources.Question_AddNewLicenseHeaderDefinitionForSolution, null).ConfigureAwait(true))
            AddNewSolutionLicenseHeaderDefinitionFileCommand.Instance.Invoke (serviceProvider.Dte2.Solution);
        }
      }
    }

    private static async Task HandleLinkedFilesAndShowMessageBoxAsync (ILicenseHeaderExtension serviceProvider, List<ProjectItem> linkedItems)
    {
      var linkedFileFilter = new LinkedFileFilter (serviceProvider.Dte2.Solution);
      linkedFileFilter.Filter (linkedItems);

      var linkedFileHandler = new LinkedFileHandler (serviceProvider);
      await linkedFileHandler.HandleAsync (linkedFileFilter);

      if (linkedFileHandler.Message != string.Empty)
        MessageBox.Show (linkedFileHandler.Message, Resources.NameOfThisExtension, MessageBoxButton.OK, MessageBoxImage.Information);
    }
  }
}