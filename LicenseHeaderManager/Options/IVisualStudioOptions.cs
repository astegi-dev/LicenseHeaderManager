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
using System.Collections.Specialized;
using Core;

namespace LicenseHeaderManager.Options
{
  internal interface IVisualStudioOptions
  {
    /// <summary>
    ///   Gets or sets whether license headers are automatically inserted into new files.
    /// </summary>
    bool InsertHeaderIntoNewFiles { get; set; }

    /// <summary>
    ///   Gets or sets commands provided by Visual Studio before or after which the "Add License Header" command should be
    ///   automatically executed.
    /// </summary>
    /// <remarks>Note that upon setter invocation, a copy of the supplied <see cref="ICollection{T}"/> is created. Hence, future updates to this
    /// initial collection are not reflected in this property.</remarks>
    ICollection<LinkedCommand> LinkedCommands { get; set; }

    /// <summary>
    ///   Creates a deep copy of the current <see cref="IVisualStudioOptions" /> instance.
    /// </summary>
    /// <returns></returns>
    IVisualStudioOptions Clone ();

    /// <summary>
    /// Is triggered when the contents of the collection held by <see cref="LinkedCommands"/> has changed.
    /// </summary>
    event EventHandler<NotifyCollectionChangedEventArgs> LinkedCommandsChanged;
  }
}