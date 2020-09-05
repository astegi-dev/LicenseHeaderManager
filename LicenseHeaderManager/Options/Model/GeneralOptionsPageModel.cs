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
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using Core.Options;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;

namespace LicenseHeaderManager.Options.Model
{
  public class GeneralOptionsPageModel : BaseOptionModel<GeneralOptionsPageModel>, IGeneralOptionsPageModel
  {
    private ObservableCollection<LinkedCommand> _linkedCommands;

    public GeneralOptionsPageModel ()
    {
      LinkedCommands = new ObservableCollection<LinkedCommand>();
    }

    public bool UseRequiredKeywords { get; set; }

    public string RequiredKeywords { get; set; }

    public ObservableCollection<LinkedCommand> LinkedCommands
    {
      get => _linkedCommands;
      set
      {
        if (_linkedCommands != null)
        {
          _linkedCommands.CollectionChanged -= InvokeLinkedCommandsChanged;
          InvokeLinkedCommandsChanged (_linkedCommands, new NotifyCollectionChangedEventArgs (NotifyCollectionChangedAction.Remove, _linkedCommands));
        }

        _linkedCommands = value;
        if (_linkedCommands == null)
          return;

        _linkedCommands.CollectionChanged += InvokeLinkedCommandsChanged;
        InvokeLinkedCommandsChanged (_linkedCommands, new NotifyCollectionChangedEventArgs (NotifyCollectionChangedAction.Add, _linkedCommands));
      }
    }

    public bool InsertInNewFiles { get; set; }

    private DTE2 Dte
    {
      get
      {
        ThreadHelper.ThrowIfNotOnUIThread();
        return ServiceProvider.GlobalProvider.GetService (typeof (DTE)) as DTE2;
      }
    }

    [DesignerSerializationVisibility (DesignerSerializationVisibility.Hidden)]
    public Commands Commands
    {
      get
      {
        ThreadHelper.ThrowIfNotOnUIThread();
        return Dte.Commands;
      }
    }

    public event NotifyCollectionChangedEventHandler LinkedCommandsChanged;

    public void Reset ()
    {
      UseRequiredKeywords = CoreOptions.c_defaultUseRequiredKeywords;
      RequiredKeywords = CoreOptions.c_defaultRequiredKeywords;
      LinkedCommands = VisualStudioOptions.s_defaultLinkedCommands;
      InsertInNewFiles = VisualStudioOptions.c_defaultInsertInNewFiles;
    }

    protected virtual void InvokeLinkedCommandsChanged (object sender, NotifyCollectionChangedEventArgs e)
    {
      LinkedCommandsChanged?.Invoke (sender, e);
    }
  }
}