using System;
using System.Collections.Generic;
using System.Linq;
using EnvDTE;
using EnvDTE80;
using LicenseHeaderManager.Utils;
using NUnit.Framework;
using Rhino.Mocks;

namespace LicenseHeaderManager.Test
{
  [TestFixture]
  internal class AllSolutionProjectsSearcherTest
  {
    [Test]
    public void TestGetAllProjects_DoesOnlyReturnProjects ()
    {
      var solution = MockRepository.GenerateStub<Solution>();

      var legitProject1 = MockRepository.GenerateStub<Project>();
      var solutionFolder = MockRepository.GenerateStub<Project>();

      var projectList = new List<Project> { legitProject1, solutionFolder };

      solutionFolder.Stub (x => x.Kind).Return (ProjectKinds.vsProjectKindSolutionFolder);
      solutionFolder.Stub (x => x.ProjectItems.Count).Return (0);

      solution.Stub (x => x.GetEnumerator()).Return (projectList.GetEnumerator());


      var allSolutionProjectsSearcher = new AllSolutionProjectsSearcher();
      var returnedProjects = allSolutionProjectsSearcher.GetAllProjects (solution);


      Assert.AreEqual (legitProject1, returnedProjects.First());
    }

    [Test]
    public void TestGetAllProjects_FindsNestedProject ()
    {
      var solution = MockRepository.GenerateStub<Solution>();

      var legitProject1 = MockRepository.GenerateStub<Project>();
      var solutionFolder = MockRepository.GenerateStub<Project>();
      var projectInSolutionFolder = MockRepository.GenerateStub<Project>();

      var projectList = new List<Project> { legitProject1, solutionFolder };

      solutionFolder.Stub (x => x.Kind).Return (ProjectKinds.vsProjectKindSolutionFolder);
      solutionFolder.Stub (x => x.ProjectItems.Count).Return (1);
      solutionFolder.Stub (x => x.ProjectItems.Item (1).SubProject).Return (projectInSolutionFolder);

      solution.Stub (x => x.GetEnumerator()).Return (projectList.GetEnumerator());


      var allSolutionProjectsSearcher = new AllSolutionProjectsSearcher();
      var returnedProjects = allSolutionProjectsSearcher.GetAllProjects (solution);


      Assert.Contains (projectInSolutionFolder, returnedProjects);
    }

    [Test]
    public void TestGetAllProjects_ShouldReturnListOfProjects ()
    {
      var solution = MockRepository.GenerateStub<Solution>();

      var legitProject1 = MockRepository.GenerateStub<Project>();
      var legitProject2 = MockRepository.GenerateStub<Project>();

      var projectList = new List<Project> { legitProject1, legitProject2 };

      solution.Stub (x => x.GetEnumerator()).Return (projectList.GetEnumerator());


      var allSolutionProjectsSearcher = new AllSolutionProjectsSearcher();
      var returnedProjects = allSolutionProjectsSearcher.GetAllProjects (solution);


      Assert.AreEqual (2, returnedProjects.Count);
    }
  }
}