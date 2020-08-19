using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Options
{
  public interface ICoreOptions
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
    ///   Gets or sets a collection of <see cref="Core.Language" /> objects that represents the
    ///   languages for which the <see cref="Core.LicenseHeaderReplacer" /> is configured to use.
    /// </summary>
    ICollection<Language> Languages { get; set; }

    /// <summary>
    ///   Creates a deep copy of the current <see cref="ICoreOptions" /> instance.
    /// </summary>
    /// <returns></returns>
    public ICoreOptions Clone ();
  }
}