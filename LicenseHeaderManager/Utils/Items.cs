using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;

namespace LicenseHeaderManager.Utils
{
  class Items
  {
    private ProjectItem _item;
    private IDictionary<string, string[]> _headers;

    public Items(ProjectItem item, IDictionary<string, string[]> headers)
    {
      _item = item;
      _headers = headers;
    }
  }
}
