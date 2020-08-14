﻿/* Copyright (c) rubicon IT GmbH
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"),
 * to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
 * and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
 * FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using EnvDTE;
using LicenseHeaderManager.Headers;
using LicenseHeaderManager.Interfaces;
using LicenseHeaderManager.MenuItemCommands.SolutionMenu;
using LicenseHeaderManager.ResultObjects;
using LicenseHeaderManager.UpdateViewModels;
using LicenseHeaderManager.Utils;
using Microsoft.VisualStudio.Shell;
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

    public static async Task AddLicenseHeaderToAllFilesAsync (LicenseHeadersPackage serviceProvider, FolderProjectUpdateViewModel folderProjectUpdateViewModel)
    {
      await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
      var obj = serviceProvider.GetSolutionExplorerItem();
      var addLicenseHeaderToAllFilesCommand = new AddLicenseHeaderToAllFilesInProjectHelper (serviceProvider.LicenseHeaderReplacer, folderProjectUpdateViewModel);

      var addLicenseHeaderToAllFilesReturn = await addLicenseHeaderToAllFilesCommand.ExecuteAsync (obj);

      await HandleLinkedFilesAndShowMessageBoxAsync (serviceProvider, addLicenseHeaderToAllFilesReturn.LinkedItems);
      await HandleAddLicenseHeaderToAllFilesInProjectResultAsync (serviceProvider, obj, addLicenseHeaderToAllFilesReturn, folderProjectUpdateViewModel);
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

    public static async Task RemoveLicenseHeaderFromAllFilesAsync (LicenseHeadersPackage serviceProvider, FolderProjectUpdateViewModel folderProjectUpdateViewModel)
    {
      await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

      var obj = serviceProvider.GetSolutionExplorerItem();
      var removeAllLicenseHeadersCommand = new RemoveLicenseHeaderFromAllFilesInProjectHelper (serviceProvider.LicenseHeaderReplacer, folderProjectUpdateViewModel);

      await removeAllLicenseHeadersCommand.ExecuteAsync (obj);
    }

    private static async Task HandleAddLicenseHeaderToAllFilesInProjectResultAsync (
        LicenseHeadersPackage serviceProvider,
        object obj,
        AddLicenseHeaderToAllFilesResult addResult,
        FolderProjectUpdateViewModel baseUpdateViewModel)
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
          if (await MessageBoxHelper.AskYesNoAsync (Resources.Question_AddExistingDefinitionFileToProject, null).ConfigureAwait (true))
          {
            ExistingLicenseHeaderDefinitionFileAdder.AddDefinitionFileToOneProject (currentProject.FileName, currentProject.ProjectItems);
            await AddLicenseHeaderToAllFilesAsync (serviceProvider, baseUpdateViewModel);
          }
        }
        else
        {
          // If no project has a license header, offer to add one for the solution.
          if (await MessageBoxHelper.AskYesNoAsync (Resources.Question_AddNewLicenseHeaderDefinitionForSolution, null).ConfigureAwait (true))
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