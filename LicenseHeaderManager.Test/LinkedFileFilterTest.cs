using System;
using System.Collections.Generic;
using System.IO;
using EnvDTE;
using LicenseHeaderManager.Utils;
using NUnit.Framework;
using Rhino.Mocks;


namespace LicenseHeaderManager.Test
{
  [TestFixture]
  internal class LinkedFileFilterTest
  {
    [Test]
    public void TestEmptyList ()
    {
      var solution = MockRepository.GenerateMock<Solution>();
      var linkedFileFilter = new LinkedFileFilter (solution);

      linkedFileFilter.Filter (new List<ProjectItem>());

      Assert.IsEmpty (linkedFileFilter.ToBeProgressed);
      Assert.IsEmpty (linkedFileFilter.NoLicenseHeaderFile);
      Assert.IsEmpty (linkedFileFilter.NotInSolution);
    }

    [Test]
    public void TestProjectItemNotInSolution ()
    {
      var solution = MockRepository.GenerateMock<Solution>();
      var linkedFile = MockRepository.GenerateMock<ProjectItem>();
      solution.Expect (x => x.FindProjectItem ("linkedFile.cs")).Return (null);
      linkedFile.Expect (x => x.Name).Return ("linkedFile.cs");

      var linkedFileFilter = new LinkedFileFilter (solution);
      linkedFileFilter.Filter (new List<ProjectItem> { linkedFile });

      Assert.IsEmpty (linkedFileFilter.ToBeProgressed);
      Assert.IsEmpty (linkedFileFilter.NoLicenseHeaderFile);
      Assert.IsNotEmpty (linkedFileFilter.NotInSolution);
    }

    [Test]
    public void TestProjectItemWithLicenseHeaderFile ()
    {
      var licenseHeaderFileName = "test.licenseheader";

      var solution = MockRepository.GenerateMock<Solution>();
      var linkedFile = MockRepository.GenerateMock<ProjectItem>();

      var licenseHeaderFile = MockRepository.GenerateStub<ProjectItem>();
      licenseHeaderFile.Expect (x => x.FileCount).Return (1);
      licenseHeaderFile.Expect (x => x.FileNames[0]).Return (licenseHeaderFileName);

      using (var writer = new StreamWriter (licenseHeaderFileName))
      {
        writer.WriteLine ("extension: .cs");
        writer.WriteLine ("//test");
      }

      var projectItems = MockRepository.GenerateStub<ProjectItems>();
      projectItems.Stub (x => x.GetEnumerator())
          .Return (null)
          .WhenCalled (
              x => x.ReturnValue =
                  new List<ProjectItem> { licenseHeaderFile }.GetEnumerator()
              );

      linkedFile.Expect (x => x.ProjectItems).Return (projectItems);
      linkedFile.Expect (x => x.Name).Return ("linkedFile.cs");
      solution.Expect (x => x.FindProjectItem ("linkedFile.cs")).Return (linkedFile);


      var linkedFileFilter = new LinkedFileFilter (solution);
      linkedFileFilter.Filter (new List<ProjectItem> { linkedFile });

      Assert.IsNotEmpty (linkedFileFilter.ToBeProgressed);
      Assert.IsEmpty (linkedFileFilter.NoLicenseHeaderFile);
      Assert.IsEmpty (linkedFileFilter.NotInSolution);

      //Cleanup
      File.Delete (licenseHeaderFileName);
    }

    [Test]
    public void TestProjectItemWithoutLicenseHeaderFile ()
    {
      var solution = MockRepository.GenerateMock<Solution>();
      solution.Expect (x => x.FullName).Return (@"d:\projects\Stuff.sln");

      var dte = MockRepository.GenerateMock<DTE>();
      dte.Expect (x => x.Solution).Return (solution);

      var projectItems = MockRepository.GenerateMock<ProjectItems>();

      var linkedFile = MockRepository.GenerateMock<ProjectItem>();
      linkedFile.Expect (x => x.DTE).Return (dte);
      projectItems.Expect (x => x.Parent).Return (new object());
      linkedFile.Expect (x => x.Collection).Return (projectItems);

      solution.Expect (x => x.FindProjectItem ("linkedFile.cs")).Return (linkedFile);


      linkedFile.Expect (x => x.Name).Return ("linkedFile.cs");
      linkedFile.Expect (x => x.Properties).Return (null);


      var linkedFileFilter = new LinkedFileFilter (solution);
      linkedFileFilter.Filter (new List<ProjectItem> { linkedFile });

      Assert.IsEmpty (linkedFileFilter.ToBeProgressed);
      Assert.IsNotEmpty (linkedFileFilter.NoLicenseHeaderFile);
      Assert.IsEmpty (linkedFileFilter.NotInSolution);
    }
  }
}