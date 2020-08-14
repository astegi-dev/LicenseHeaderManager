#region copyright

// Copyright (c) rubicon IT GmbH

// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
// FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE. 

#endregion

using System;
using System.Threading.Tasks;
using Core;
using EnvDTE;
using LicenseHeaderManager.Interfaces;
using LicenseHeaderManager.SolutionUpdateViewModels;
using LicenseHeaderManager.Utils;

namespace LicenseHeaderManager.MenuItemCommands.Common
{
  public class RemoveLicenseHeaderFromAllFilesInSolutionHelper : ISolutionLevelCommand
  {
    private const string c_commandName = "Remove LicenseHeader from all Projects";
    private readonly LicenseHeaderReplacer _licenseHeaderReplacer;

    private readonly SolutionUpdateViewModel _viewModel;

    public RemoveLicenseHeaderFromAllFilesInSolutionHelper (SolutionUpdateViewModel viewModel, LicenseHeaderReplacer licenseHeaderReplacer)
    {
      _viewModel = viewModel;
      _licenseHeaderReplacer = licenseHeaderReplacer;
    }

    public string GetCommandName ()
    {
      return c_commandName;
    }

    public async Task ExecuteAsync (Solution solution)
    {
      if (solution == null) return;

      var allSolutionProjectsSearcher = new AllSolutionProjectsSearcher();
      var projectsInSolution = allSolutionProjectsSearcher.GetAllProjects (solution);

      _viewModel.ProjectCount = projectsInSolution.Count;
      var removeAllLicenseHeadersCommand = new RemoveLicenseHeaderFromAllFilesInProjectHelper (_licenseHeaderReplacer);

      foreach (var project in projectsInSolution)
      {
        _viewModel.CurrentProject = project.Name;
        await removeAllLicenseHeadersCommand.ExecuteAsync (project);
        _viewModel.ProcessedProjectCount++;
      }
    }
  }
}