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

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EnvDTE;
using LicenseHeaderManager.Headers;
using LicenseHeaderManager.Interfaces;

namespace LicenseHeaderManager.Utils
{
  public class LinkedFileHandler
  {
    private readonly ILicenseHeaderExtension _licenseHeaderExtension;

    public string Message { get; private set; }

    public LinkedFileHandler (ILicenseHeaderExtension licenseHeaderExtension)
    {
      _licenseHeaderExtension = licenseHeaderExtension;
      Message = string.Empty;
    }

    public async Task HandleAsync (ILinkedFileFilter linkedFileFilter)
    {
      foreach (var projectItem in linkedFileFilter.ToBeProgressed)
      {
        // todo replace
        var headers = LicenseHeaderFinder.GetHeaderDefinitionForItem (projectItem);
        //licenseHeaderReplacer.RemoveOrReplaceHeader (projectItem, headers, true);
        var result = await _licenseHeaderExtension.GetLicenseHeaderReplacer().RemoveOrReplaceHeader (
            new Core.LicenseHeaderInput (projectItem.Document.FullName, headers, projectItem.GetAdditionalProperties()),
            true,
            CoreHelpers.NonCommentLicenseHeaderDefinitionInquiry,
            message => CoreHelpers.NoLicenseHeaderDefinitionFound (message, _licenseHeaderExtension));
      }

      if (linkedFileFilter.NoLicenseHeaderFile.Any() || linkedFileFilter.NotInSolution.Any())
      {
        var notProgressedItems =
            linkedFileFilter.NoLicenseHeaderFile.Concat (linkedFileFilter.NotInSolution).ToList();

        var notProgressedNames = notProgressedItems.Select (x => x.Name).ToList();

        Message +=
            string.Format (Resources.LinkedFileUpdateInformation, string.Join ("\n", notProgressedNames))
                .Replace (@"\n", "\n");
      }
    }
  }
}