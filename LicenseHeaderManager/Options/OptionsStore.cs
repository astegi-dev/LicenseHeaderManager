using System;
using System.Collections.Generic;
using Core;

namespace LicenseHeaderManager.Options
{
  internal class OptionsStore : IOptionsStore
  {
    public bool InsertHeaderIntoNewFiles { get; set; }

    public bool UseRequiredKeywords { get; set; }

    public string RequiredKeywords { get; set; }

    public IEnumerable<LinkedCommand> LinkedCommands { get; set; }

    public string DefaultLicenseHeaderFileText { get; set; }

    public IEnumerable<Language> Languages { get; set; }

    public void Save ()
    {
      throw new NotImplementedException();
    }

    public void Load ()
    {
      throw new NotImplementedException();
    }
  }
}