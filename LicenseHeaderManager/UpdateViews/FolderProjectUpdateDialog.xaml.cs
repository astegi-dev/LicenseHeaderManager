using LicenseHeaderManager.UpdateViewModels;
using Microsoft.VisualStudio.PlatformUI;

namespace LicenseHeaderManager.UpdateViews
{
  /// <summary>
  /// Interaction logic for FolderProjectUpdateDialog.xaml
  /// </summary>
  public partial class FolderProjectUpdateDialog : DialogWindow
  {
    public FolderProjectUpdateDialog(FolderProjectUpdateViewModel folderProjectUpdateViewModel)
    {
      InitializeComponent();
      DataContext = folderProjectUpdateViewModel;
    }
  }
}
