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

using System;
using System.Collections.Generic;
using LicenseHeaderManager.Options.DialogPageControls;
using LicenseHeaderManager.Options.Model;
using System.Windows.Forms;
using Core.Options;

namespace LicenseHeaderManager.Options.DialogPages
{
  public class GeneralOptionsPage : BaseOptionPage<GeneralOptionsPageModel>
  {
    public GeneralOptionsPage ()
    {
    }

    public override void ResetSettings ()
    {
      ((IGeneralOptionsPageModel) Model).Reset();
    }

    protected override IWin32Window Window => new WpfHost (new WpfOptions ((IGeneralOptionsPageModel) Model));

    protected override IEnumerable<UpdateStep> GetVersionUpdateSteps ()
    {
      yield return new UpdateStep (new Version (3, 0, 1), MigrateStorageLocation_3_0_1);
    }

    private void MigrateStorageLocation_3_0_1 ()
    {
      if (!System.Version.TryParse (Version, out var version) || version < new Version (3, 0, 0))
      {
        LoadRegistryValuesBefore_3_0_0();
      }
      else
      {
        var migratedOptionsPage = new GeneralOptionsPageModel();
        LoadRegistryValuesBefore_3_0_0 (migratedOptionsPage);

        OptionsFacade.CurrentOptions.InsertHeaderIntoNewFiles = ThreeWaySelectionForMigration (
            OptionsFacade.CurrentOptions.InsertHeaderIntoNewFiles,
            migratedOptionsPage.InsertHeaderIntoNewFiles,
            VisualStudioOptions.c_defaultInsertHeaderIntoNewFiles);
        OptionsFacade.CurrentOptions.UseRequiredKeywords = ThreeWaySelectionForMigration (
            OptionsFacade.CurrentOptions.UseRequiredKeywords,
            migratedOptionsPage.UseRequiredKeywords,
            CoreOptions.c_defaultUseRequiredKeywords);
        OptionsFacade.CurrentOptions.RequiredKeywords = ThreeWaySelectionForMigration (
            OptionsFacade.CurrentOptions.RequiredKeywords,
            migratedOptionsPage.RequiredKeywords,
            CoreOptions.c_defaultRequiredKeywords);
        OptionsFacade.CurrentOptions.LinkedCommands = migratedOptionsPage.LinkedCommands;
      }
    }
  }
}