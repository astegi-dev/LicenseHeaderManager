using Microsoft.VisualStudio.PlatformUI;
using System.ComponentModel;
using System.Windows.Input;

namespace LicenseHeaderManager.UpdateViewModels
{
  public class BaseUpdateViewModel : INotifyPropertyChanged
  {
    private int _fileCountCurrentProject;
    private int _processedFilesCountCurrentProject;
    private int _processedProjectCount;

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
        NotifyPropertyChanged(nameof(FileCountCurrentProject));
      }
    }

    public int ProcessedFilesCountCurrentProject
    {
      get => _processedFilesCountCurrentProject;
      set
      {
        _processedFilesCountCurrentProject = value;
        NotifyPropertyChanged(nameof(ProcessedFilesCountCurrentProject));
      }
    }

    public ICommand CloseCommand
    {
      get { return new RelayCommand(o => ((DialogWindow)o).Close()); }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    internal void NotifyPropertyChanged(string propertyName = "")
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
  }
}
