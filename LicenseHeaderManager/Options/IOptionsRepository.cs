using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LicenseHeaderManager.Options
{
  interface IOptionsRepository
  {
    IEnumerable<Language> Languages { get; }

    void Save();

    void Load();
  }
}
