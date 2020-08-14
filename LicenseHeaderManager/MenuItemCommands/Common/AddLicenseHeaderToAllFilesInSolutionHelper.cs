/* Copyright (c) rubicon IT GmbH
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
using System.Threading.Tasks;
using Core;
using EnvDTE;
using LicenseHeaderManager.Headers;
using LicenseHeaderManager.Interfaces;
using LicenseHeaderManager.MenuItemCommands.SolutionMenu;
using LicenseHeaderManager.UpdateViewModels;
using LicenseHeaderManager.Utils;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;
using Window = System.Windows.Window;

namespace LicenseHeaderManager.MenuItemCommands.Common
{
  public class AddLicenseHeaderToAllFilesInSolutionHelper : IMenuItemButtonHandler
  {
    private const string c_commandName = "Add LicenseHeader to all files in Solution";
    private const int MaxProjectsWithoutDefinitionFileShownInMessage = 5;
    private readonly LicenseHeaderReplacer _licenseHeaderReplacer;

    private readonly SolutionUpdateViewModel _solutionUpdateViewModel;

    public AddLicenseHeaderToAllFilesInSolutionHelper (
        LicenseHeaderReplacer licenseHeaderReplacer,
        SolutionUpdateViewModel solutionUpdateViewModel)
    {
      _licenseHeaderReplacer = licenseHeaderReplacer;
      _solutionUpdateViewModel = solutionUpdateViewModel;
    }

    public string Description => c_commandName;

    public async Task ExecuteAsync (Solution solution, Window window)
    {
      if (solution == null) return;

      var solutionHeaderDefinitions = LicenseHeaderFinder.GetHeaderDefinitionForSolution (solution);

      var allSolutionProjectsSearcher = new AllSolutionProjectsSearcher();
      var projectsInSolution = allSolutionProjectsSearcher.GetAllProjects (solution);

      var projectsWithoutLicenseHeaderFile = projectsInSolution
          .Where (project => LicenseHeaderFinder.GetHeaderDefinitionForProjectWithoutFallback (project) == null)
          .ToList();

      var projectsWithLicenseHeaderFile = projectsInSolution
          .Where (project => LicenseHeaderFinder.GetHeaderDefinitionForProjectWithoutFallback (project) != null)
          .ToList();

      if (solutionHeaderDefinitions != null || !projectsWithoutLicenseHeaderFile.Any())
      {
        // Every project is covered either by a solution or project level license header defintion, go ahead and add them.
        await AddLicenseHeaderToProjectsAsync (projectsInSolution);
      }
      else
      {
        // Some projects are not covered by a header.

        var someProjectsHaveDefinition = projectsWithLicenseHeaderFile.Count > 0;
        if (someProjectsHaveDefinition)
        {
          // Some projects have a header. Ask the user if they want to add an existing header to the uncovered projects.
          if (await DefinitionFilesShouldBeAddedAsync (projectsWithoutLicenseHeaderFile, window))
            ExistingLicenseHeaderDefinitionFileAdder.AddDefinitionFileToMultipleProjects (projectsWithoutLicenseHeaderFile);

          await AddLicenseHeaderToProjectsAsync (projectsInSolution);
        }
        else
        {
          // No projects have definition. Ask the user if they want to add a solution level header definition.
          if (await MessageBoxHelper.AskYesNoAsync (window, Resources.Question_AddNewLicenseHeaderDefinitionForSolution).ConfigureAwait (true))
          {
            AddNewSolutionLicenseHeaderDefinitionFileCommand.Instance.Invoke (solution);

            // They want to go ahead and apply without editing.
            if (!await MessageBoxHelper.AskYesNoAsync (window, Resources.Question_StopForConfiguringDefinitionFilesSingleFile).ConfigureAwait (true))
              await AddLicenseHeaderToProjectsAsync (projectsInSolution);
          }
        }
      }
    }

    private async Task<bool> DefinitionFilesShouldBeAddedAsync (List<Project> projectsWithoutLicenseHeaderFile, Window window)
    {
      if (!projectsWithoutLicenseHeaderFile.Any()) return false;

      var errorResourceString = Resources.Error_MultipleProjectsNoLicenseHeaderFile;
      string projects;

      if (projectsWithoutLicenseHeaderFile.Count > MaxProjectsWithoutDefinitionFileShownInMessage)
      {
        projects = string.Join (
            "\n",
            projectsWithoutLicenseHeaderFile
                .Select (x => x.Name)
                .Take (5)
                .ToList());

        projects += "\n...";
      }
      else
      {
        projects = string.Join (
            "\n",
            projectsWithoutLicenseHeaderFile
                .Select (x => x.Name)
                .ToList());
      }

      var message = string.Format (errorResourceString, projects).ReplaceNewLines();


      return await MessageBoxHelper.AskYesNoAsync (window, message).ConfigureAwait (true);
    }

    private async Task AddLicenseHeaderToProjectsAsync (ICollection<Project> projectsInSolution)
    {
      _solutionUpdateViewModel.ProcessedProjectCount = 0;
      _solutionUpdateViewModel.ProjectCount = projectsInSolution.Count;
      var addAllLicenseHeadersCommand = new AddLicenseHeaderToAllFilesInProjectHelper (_licenseHeaderReplacer, _solutionUpdateViewModel);

      foreach (var project in projectsInSolution)
      {
        await addAllLicenseHeadersCommand.ExecuteAsync (project);
        await IncrementProjectCountAsync (_solutionUpdateViewModel).ConfigureAwait (true);
      }
    }

    private async Task IncrementProjectCountAsync (SolutionUpdateViewModel viewModel)
    {
      await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
      viewModel.ProcessedProjectCount++;
    }
  }
}