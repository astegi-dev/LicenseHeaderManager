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
using EnvDTE;
using LicenseHeaderManager.Utils;
using NUnit.Framework;
using Rhino.Mocks;

namespace LicenseHeaderManager.Test
{
  [TestFixture]
  public class ProjectItemInspectionTest
  {
    [Test]
    public void TestIsLicenseHeader ()
    {
      var licenseHeader = MockRepository.GenerateMock<ProjectItem>();
      licenseHeader.Expect (x => x.Name).Return ("test.licenseheader");

      var notLicenseHeader = MockRepository.GenerateMock<ProjectItem>();
      notLicenseHeader.Expect (x => x.Name).Return ("test.cs");

      Assert.IsTrue (ProjectItemInspection.IsLicenseHeader (licenseHeader));
      Assert.IsFalse (ProjectItemInspection.IsLicenseHeader (notLicenseHeader));
    }

    [Test]
    [Ignore ("#68 changed access to IsLink which cant be mocked anymore.")]
    public void TestIsLink ()
    {
      var linkedProjectItem = MockRepository.GenerateMock<ProjectItem>();

      var propertyStub = MockRepository.GenerateStub<Property>();
      propertyStub.Value = true;

      linkedProjectItem.Stub (x => x.Properties).Return (MockRepository.GenerateStub<Properties>());
      linkedProjectItem.Properties.Stub (x => x.Item ("IsLink")).Return (propertyStub);

      Assert.IsTrue (ProjectItemInspection.IsLink (linkedProjectItem));
    }

    [Test]
    public void TestIsPhysicalFile ()
    {
      var physicalFile = MockRepository.GenerateMock<ProjectItem>();
      physicalFile.Expect (x => x.Kind).Return (Constants.vsProjectItemKindPhysicalFile);

      var virtualFolder = MockRepository.GenerateMock<ProjectItem>();
      virtualFolder.Expect (x => x.Kind).Return (Constants.vsProjectItemKindVirtualFolder);


      Assert.IsTrue (ProjectItemInspection.IsPhysicalFile (physicalFile));
      Assert.IsFalse (ProjectItemInspection.IsPhysicalFile (virtualFolder));
    }

    [Test]
    [Ignore ("#68 changed access to IsLink which cant be mocked anymore.")]
    public void TestProjectItem_WithoutIsLinkProperty ()
    {
      var linkedProjectItem = MockRepository.GenerateMock<ProjectItem>();

      linkedProjectItem.Stub (x => x.Properties).Return (MockRepository.GenerateStub<Properties>());
      linkedProjectItem.Properties.Stub (x => x.Item ("IsLink")).Throw (new ArgumentException());

      Assert.IsFalse (ProjectItemInspection.IsLink (linkedProjectItem));
    }
  }
}