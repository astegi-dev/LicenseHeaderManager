/* Copyright (c) rubicon IT GmbH
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"),
 * to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
 * and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
 * FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE. 
 */

using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;

namespace LicenseHeaderManager.Options
{
  /// <summary>
  /// A provider for custom <see cref="DialogPage" /> implementations.
  /// </summary>
  public class OptionsPageProvider
  {
    public class General : BaseOptionPage<GeneralOptionsPageAsync>
    {
      protected override IWin32Window Window => new WpfHost (new WpfOptions ((IGeneralOptionsPage) _model));
    }

    public class DefaultLicenseHeader : BaseOptionPage<DefaultLicenseHeaderPageAsync>
    {
      protected override IWin32Window Window => new WpfHost (new WpfDefaultLicenseHeader ((IDefaultLicenseHeaderPage) _model));
    }

    public class Languages : BaseOptionPage<LanguagesPageAsync>
    {
      protected override IWin32Window Window
      {
        get {
          var host = new WpfHost(new WpfLanguages((ILanguagesPage)_model));
          return host;
        }
      }
    }
  }
}