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

using System;
using System.ComponentModel;
using System.Windows.Input;
using Microsoft.VisualStudio.PlatformUI;

namespace LicenseHeaderManager.SolutionUpdateViewModels
{
  public class SolutionUpdateViewModel : INotifyPropertyChanged
  {
    private string _currentProject = "Preparing update...";
    private int _processedFilesCountCurrentProject;
    private int _fileCountCurrentProject;
    private int _projectCount;
    private int _processedProjectCount;

    public SolutionUpdateViewModel ()
    {
      // set progressbar maximum values to something other than 0 in order to prevent progressbar from being "full" at dialog startup
      ProjectCount = 1;
      FileCountCurrentProject = 1;
    }

    public string CurrentProject
    {
      get => _currentProject;
      set
      {
        _currentProject = value;
        NotifyPropertyChanged (nameof(CurrentProject));
      }
    }

    public int ProjectCount
    {
      get => _projectCount;
      set
      {
        _projectCount = value;
        NotifyPropertyChanged(nameof(ProjectCount));
      }
    }

    public int ProcessedProjectCount
    {
      get => _processedProjectCount;
      set
      {
        _processedProjectCount = value;
        NotifyPropertyChanged(nameof(ProcessedProjectCount));
      }
    }

    public int FileCountCurrentProject
    {
      get => _fileCountCurrentProject;
      set
      {
        _fileCountCurrentProject = value;
        NotifyPropertyChanged (nameof(FileCountCurrentProject));
      }
    }

    public int ProcessedFilesCountCurrentProject
    {
      get => _processedFilesCountCurrentProject;
      set
      {
        _processedFilesCountCurrentProject = value;
        NotifyPropertyChanged (nameof(ProcessedFilesCountCurrentProject));
      }
    }

    public ICommand CloseCommand
    {
      get { return new RelayCommand (o => ((DialogWindow) o).Close()); }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    private void NotifyPropertyChanged (string propertyName = "")
    {
      PropertyChanged?.Invoke (this, new PropertyChangedEventArgs (propertyName));
    }
  }
}