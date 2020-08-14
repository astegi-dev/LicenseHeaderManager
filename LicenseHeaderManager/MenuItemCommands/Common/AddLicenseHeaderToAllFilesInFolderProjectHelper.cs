using Core;
using EnvDTE;
using LicenseHeaderManager.Interfaces;
using LicenseHeaderManager.UpdateViewModels;
using LicenseHeaderManager.Utils;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;
using Window = System.Windows.Window;

namespace LicenseHeaderManager.MenuItemCommands.Common
{
  internal class AddLicenseHeaderToAllFilesInFolderProjectHelper : IButtonCommand
  {
    private const string c_commandName = "Add LicenseHeader to all files in folder or project";

    private readonly ILicenseHeaderExtension _licenseHeaderExtension;
    private readonly FolderProjectUpdateViewModel _folderProjectUpdateViewModel;

    public AddLicenseHeaderToAllFilesInFolderProjectHelper(ILicenseHeaderExtension licenseHeaderExtension, FolderProjectUpdateViewModel folderProjectUpdateViewModel)
    {
      _licenseHeaderExtension = licenseHeaderExtension;
      _folderProjectUpdateViewModel = folderProjectUpdateViewModel;
    }

    public string GetCommandName()
    {
      return c_commandName;
    }

    public async Task ExecuteAsync(Solution solutionObject, Window window)
    {
      await FolderProjectMenuHelper.AddLicenseHeaderToAllFilesAsync((LicenseHeadersPackage)_licenseHeaderExtension, _folderProjectUpdateViewModel);
    }
  }
}
