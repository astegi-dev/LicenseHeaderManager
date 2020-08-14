using System;
using System.Collections.Generic;
using EnvDTE;
using LicenseHeaderManager.Interfaces;
using LicenseHeaderManager.Utils;
using NUnit.Framework;
using Rhino.Mocks;

namespace LicenseHeaderManager.Test
{
  [TestFixture]
  internal class LinkedFileHandlerTest
  {
    [Test]
    public void TestNoLicenseHeaderFile ()
    {
      var extension = MockRepository.GenerateStub<ILicenseHeaderExtension>();
      var projectItem = MockRepository.GenerateMock<ProjectItem>();
      projectItem.Expect (x => x.Name).Return ("projectItem.cs");

      var linkedFileFilter = MockRepository.GenerateMock<ILinkedFileFilter>();
      //LicenseHeaderReplacer licenseHeaderReplacer = MockRepository.GenerateStrictMock<LicenseHeaderReplacer> (extension);

      linkedFileFilter.Expect (x => x.NoLicenseHeaderFile).Return (new List<ProjectItem> { projectItem });
      linkedFileFilter.Expect (x => x.ToBeProgressed).Return (new List<ProjectItem>());
      linkedFileFilter.Expect (x => x.NotInSolution).Return (new List<ProjectItem>());


      var linkedFileHandler = new LinkedFileHandler (null);
      //linkedFileHandler.HandleAsync(licenseHeaderReplacer, linkedFileFilter);

      //string expectedMessage = string.Format(Resources.LinkedFileUpdateInformation, "projectItem.cs")
      //    .Replace(@"\n", "\n");

      //Assert.AreEqual(expectedMessage, linkedFileHandler.Message);
    }

    [Test]
    public void TestNoProjectItems ()
    {
      var solution = MockRepository.GenerateStub<Solution>();
      var extension = MockRepository.GenerateStub<ILicenseHeaderExtension>();

      var linkedFileFilter = MockRepository.GenerateStrictMock<LinkedFileFilter> (solution);
      //LicenseHeaderReplacer licenseHeaderReplacer = MockRepository.GenerateStrictMock<LicenseHeaderReplacer> (extension);

      //LinkedFileHandler linkedFileHandler = new LinkedFileHandler(null);
      //linkedFileHandler.HandleAsync(licenseHeaderReplacer, linkedFileFilter);

      //Assert.AreEqual(string.Empty, linkedFileHandler.Message);
    }
  }
}