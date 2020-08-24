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
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Core;
using Core.Options;

namespace LicenseHeaderManager.Options
{
  // TODO should facade implement both interfaces or duplicate members like below?
  public interface IOptionsFacade
  {
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
    ///   Gets or sets the text for new license header definition files.
    /// </summary>
    string DefaultLicenseHeaderFileText { get; set; }

    /// <summary>
    ///   Gets or sets a list of <see cref="Core.Language" /> objects that represents the
    ///   languages for which the <see cref="Core.LicenseHeaderReplacer" /> is configured to use.
    /// </summary>
    ObservableCollection<Language> Languages { get; set; }

    /// <summary>
    ///   Gets or sets whether license headers are automatically inserted into new files.
    /// </summary>
    bool InsertInNewFiles { get; set; }

    /// <summary>
    ///   Gets or sets commands provided by Visual Studio before or after which the "Add License Header" command should be
    ///   automatically executed.
    /// </summary>
    /// <remarks>Note that upon setter invocation, a copy of the supplied <see cref="ICollection{T}"/> is created. Hence, future updates to this
    /// initial collection are not reflected in this property.</remarks>
    ObservableCollection<LinkedCommand> LinkedCommands { get; set; }

    /// <summary>
    /// Is triggered when the contents of the collection held by <see cref="LinkedCommands"/> has changed.
    /// </summary>
    event EventHandler<NotifyCollectionChangedEventArgs> LinkedCommandsChanged;

    /// <summary>
    ///   Creates a deep copy of the current <see cref="IOptionsFacade" /> instance.
    /// </summary>
    /// <returns></returns>
    IOptionsFacade Clone ();

    public string Version { get; set; }
  }
}