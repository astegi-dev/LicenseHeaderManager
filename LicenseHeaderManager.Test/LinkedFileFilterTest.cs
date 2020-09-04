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

      // resolve NullReferenceException:
      // mock LicenseHeadersPackage.Instance.LicenseHeaderExtractor to return a (new) instance of LicenseHeaderExtractor
      // => no NRE in LicenseHeaderFinder.ExtractHeaderDefinitions (headerFile) any more
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