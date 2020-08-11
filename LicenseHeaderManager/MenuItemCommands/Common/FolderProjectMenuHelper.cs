// first line LicenseHeaderManager,LicenseHeaderManager.CommandsAsync.Common
// second line copyright123

// first line
// second line copyright456

using System;
using EnvDTE;
using LicenseHeaderManager.Headers;
using LicenseHeaderManager.Options;
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

      await serviceProvider.HandleLinkedFilesAndShowMessageBox (addLicenseHeaderToAllFilesReturn.LinkedItems);
      await serviceProvider.HandleAddLicenseHeaderToAllFilesInProjectReturnAsync (obj, addLicenseHeaderToAllFilesReturn);
    }

    public static void AddNewLicenseHeaderDefinitionFile (LicenseHeadersPackage serviceProvider)
    {
      ThreadHelper.ThrowIfNotOnUIThread();

      var page = (DefaultLicenseHeaderPage) serviceProvider.GetDialogPage (typeof (DefaultLicenseHeaderPage));
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
  }
}