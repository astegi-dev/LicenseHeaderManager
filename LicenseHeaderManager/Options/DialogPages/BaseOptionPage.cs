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
using LicenseHeaderManager.Options.Model;
using Microsoft.VisualStudio.Shell;

namespace LicenseHeaderManager.Options.DialogPages
{
  /// <summary>
  /// A base class for a DialogPage to show in Tools -> Options.
  /// </summary>
  public class BaseOptionPage<T> : DialogPage
      where T : BaseOptionModel<T>, new()
  {
    public readonly BaseOptionModel<T> _model;

    public BaseOptionPage ()
    {
#pragma warning disable VSTHRD104 // Offer async methods
      _model = ThreadHelper.JoinableTaskFactory.Run (BaseOptionModel<T>.CreateAsync);
#pragma warning restore VSTHRD104 // Offer async methods
    }

    public override object AutomationObject => _model;

    public override void LoadSettingsFromStorage ()
    {
      _model.Load();
    }

    public override void SaveSettingsToStorage ()
    {
      _model.Save();
    }
  }
}