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

using Core;
using EnvDTE;
using System;
using System.Linq;

namespace LicenseHeaderManager.Utils
{
  public static class ProjectItemInspection
  {
    private const string guidItemTypePhysicalFile = "6bb5f8ee-4483-11d3-8bcf-00c04f8ec28c";

    public static bool IsPhysicalFile (ProjectItem projectItem)
    {
      return projectItem.Kind == Constants.vsProjectItemKindPhysicalFile || projectItem.Kind == "{" + guidItemTypePhysicalFile + "}";
    }

    public static bool IsLicenseHeader (ProjectItem projectItem)
    {
      return projectItem.Name.Contains (LicenseHeaderReplacer.HeaderDefinitionExtension);
    }

    public static bool IsLink (ProjectItem projectItem)
    {
      if (projectItem.Properties == null)
        return false;

      Property isLinkProperty;

      try
      {
        isLinkProperty = projectItem.Properties.Cast<Property>().FirstOrDefault (property => property.Name == "IsLink");
      }
      catch (ArgumentException)
      {
        return false;
      }

      return isLinkProperty != null && (bool) isLinkProperty.Value;
    }

    public static bool IsFolder (ProjectItem projectItem)
    {
      return string.Equals (projectItem.Kind, Constants.vsProjectItemKindPhysicalFolder, StringComparison.InvariantCultureIgnoreCase) ||
             string.Equals (projectItem.Kind, Constants.vsProjectItemKindVirtualFolder, StringComparison.InvariantCultureIgnoreCase);
    }
  }
}