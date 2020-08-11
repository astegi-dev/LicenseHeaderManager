using System.Collections.Generic;

namespace LicenseHeaderManager.Options
{
  internal interface IOptionsStore
  {
    /// <summary>
    /// Gets or sets whether license headers are automatically inserted into new files.
    /// </summary>
    bool InsertHeaderIntoNewFiles { get; set; }

    /// <summary>
    /// Gets or sets whether license header comments should be removed only if they contain at least one of the keywords specified by <see cref="RequiredKeywords"/>.
    /// </summary>
    bool UseRequiredKeywords { get; set; }

    /// <summary>
    /// If <see cref="UseRequiredKeywords"/> is <see langword="true"/>, only license header comments that contain at 
    /// least one of the words specified by this property (separated by "," and possibly whitespaces) are removed.
    /// </summary>
    string RequiredKeywords { get; set; }

    /// <summary>
    /// Gets or sets commands provided by Visual Studio before or after which the "Add License Header" command should be automatically executed.
    /// </summary>
    IEnumerable<LinkedCommand> LinkedCommands { get; set; }

    /// <summary>
    /// Gets or sets the text for new license header definition files.
    /// </summary>
    public string DefaultLicenseHeaderFileText { get; set; }

    /// <summary>
    /// Gets or sets a collection of <see cref="Core.Language"/> objects that represents the 
    /// languages for which the <see cref="Core.LicenseHeaderReplacer"/> is configured to use.
    /// </summary>
    IEnumerable<Language> Languages { get; set; }

    /// <summary>
    /// Saves the current <see cref="IOptionsStore"/> instance.
    /// </summary>
    void Save ();

    /// <summary>
    /// Loads the current <see cref="IOptionsStore"/> instance.
    /// </summary>
    void Load ();
  }
}