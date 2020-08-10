// first line LicenseHeaderManager,LicenseHeaderManager.CommandsAsync.Common
// second line copyright123

// first line
// second line copyright456

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;
using LicenseHeaderManager.Headers;
using LicenseHeaderManager.Options;
using LicenseHeaderManager.Utils;
using Microsoft;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using Task = System.Threading.Tasks.Task;

namespace LicenseHeaderManager.CommandsAsync.Common
{
  internal class FolderProjectMenuHelper
  {
    public void AddExistingLicenseHeaderDefinitionFile (LicenseHeadersPackage serviceProvider)
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

    public async System.Threading.Tasks.Task AddLicenseHeaderToAllFilesAsync (LicenseHeadersPackage serviceProvider)
    {
      await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
      var obj = serviceProvider.GetSolutionExplorerItem();
      var addLicenseHeaderToAllFilesCommand = new AddLicenseHeaderToAllFilesInProjectHelper (serviceProvider._licenseReplacer);

      var statusBar = (IVsStatusbar) await serviceProvider.GetServiceAsync (typeof (SVsStatusbar));
      Assumes.Present (statusBar);

      statusBar.SetText (Resources.UpdatingFiles);
      var addLicenseHeaderToAllFilesReturn = addLicenseHeaderToAllFilesCommand.Execute (obj);
      statusBar.SetText (string.Empty);

      serviceProvider.HandleLinkedFilesAndShowMessageBox (addLicenseHeaderToAllFilesReturn.LinkedItems);
      serviceProvider.HandleAddLicenseHeaderToAllFilesInProjectReturn (obj, addLicenseHeaderToAllFilesReturn);
    }

    public void AddNewLicenseHeaderDefinitionFile (LicenseHeadersPackage serviceProvider)
    {
      ThreadHelper.ThrowIfNotOnUIThread();

      var page = (DefaultLicenseHeaderPage) serviceProvider.GetDialogPage (typeof (DefaultLicenseHeaderPage));
      var solutionItem = serviceProvider.GetSolutionExplorerItem();
      var project = solutionItem as Project;
      if (project == null)
      {
        if (solutionItem is ProjectItem projectItem)
          LicenseHeader.AddLicenseHeaderDefinitionFile (projectItem, page);
      }

      if (project == null)
        return;

      var licenseHeaderDefinitionFile = LicenseHeader.AddHeaderDefinitionFile (project, page);
      licenseHeaderDefinitionFile.Open (EnvDTE.Constants.vsViewKindCode).Activate();
    }

    public async Task RemoveLicenseHeadersFromAllFilesAsync(LicenseHeadersPackage serviceProvider)
    {
      await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

      var obj = serviceProvider.GetSolutionExplorerItem();
      var removeAllLicenseHeadersCommand = new RemoveLicenseHeaderFromAllFilesInProjectHelper(serviceProvider._licenseReplacer);

      var statusBar = (IVsStatusbar)await serviceProvider.GetServiceAsync(typeof(SVsStatusbar));
      Assumes.Present(statusBar);
      statusBar.SetText(Resources.UpdatingFiles);

      removeAllLicenseHeadersCommand.Execute(obj);

      statusBar.SetText(string.Empty);
    }
  }
}