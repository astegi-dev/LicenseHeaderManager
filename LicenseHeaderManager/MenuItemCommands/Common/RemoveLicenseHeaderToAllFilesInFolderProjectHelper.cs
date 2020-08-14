using Core;
using EnvDTE;
using LicenseHeaderManager.Interfaces;
using LicenseHeaderManager.UpdateViewModels;
using System.Threading.Tasks;
using LicenseHeaderManager.Utils;
using Window = System.Windows.Window;

namespace LicenseHeaderManager.MenuItemCommands.Common
{
  internal class RemoveLicenseHeaderToAllFilesInFolderProjectHelper: IButtonCommand
  {
    private const string c_commandName = "Add LicenseHeader to all files in folder or project";

    private readonly ILicenseHeaderExtension _licenseHeaderExtension;
    private readonly FolderProjectUpdateViewModel _folderProjectUpdateViewModel;

    public RemoveLicenseHeaderToAllFilesInFolderProjectHelper (ILicenseHeaderExtension licenseHeaderExtension, FolderProjectUpdateViewModel folderProjectUpdateViewModel)
    {
      _licenseHeaderExtension = licenseHeaderExtension;
      _folderProjectUpdateViewModel = folderProjectUpdateViewModel;
    }

    public string GetCommandName ()
    {
      return c_commandName;
    }

    public async Task ExecuteAsync (Solution solutionObject, Window window)
    {
      await FolderProjectMenuHelper.RemoveLicenseHeaderFromAllFilesAsync((LicenseHeadersPackage)_licenseHeaderExtension, _folderProjectUpdateViewModel);
    }
  }
}
