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
using Core;

namespace LicenseHeaderManager.Options
{
  internal interface IOptionsStore
  {
    /// <summary>
    ///   Gets or sets whether license headers are automatically inserted into new files.
    /// </summary>
    bool InsertHeaderIntoNewFiles { get; set; }

    /// <summary>
    ///   Gets or sets whether license header comments should be removed only if they contain at least one of the keywords
    ///   specified by <see cref="RequiredKeywords" />.
    /// </summary>
    bool UseRequiredKeywords { get; set; }

    /// <summary>
    ///   If <see cref="UseRequiredKeywords" /> is <see langword="true" />, only license header comments that contain at
    ///   least one of the words specified by this property (separated by "," and possibly whitespaces) are removed.
    /// </summary>
    string RequiredKeywords { get; set; }

    /// <summary>
    ///   Gets or sets commands provided by Visual Studio before or after which the "Add License Header" command should be
    ///   automatically executed.
    /// </summary>
    IEnumerable<LinkedCommand> LinkedCommands { get; set; }

    /// <summary>
    ///   Gets or sets the text for new license header definition files.
    /// </summary>
    string DefaultLicenseHeaderFileText { get; set; }

    /// <summary>
    ///   Gets or sets a collection of <see cref="Core.Language" /> objects that represents the
    ///   languages for which the <see cref="Core.LicenseHeaderReplacer" /> is configured to use.
    /// </summary>
    IEnumerable<Language> Languages { get; set; }

    /// <summary>
    ///   Creates a deep copy of the current <see cref="IOptionsStore" /> instance.
    /// </summary>
    /// <returns></returns>
    IOptionsStore Clone ();
  }
}