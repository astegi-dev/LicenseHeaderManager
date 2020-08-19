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
using System.ComponentModel;

namespace LicenseHeaderManager.Options
{
  public class DefaultLicenseHeaderPageAsync : BaseOptionModel<DefaultLicenseHeaderPageAsync>
  {
    [Category("A category")]
    [DisplayName("Show message")]
    [Description("The description of the property")]
    [DefaultValue(true)]
    public bool ShowMessage { get; set; } = true;

    [Category("Another category")]
    [DisplayName("Favorite clothing")]
    [Description("The description of the property")]
    [DefaultValue(Clothing.Pants)]
    [TypeConverter(typeof(EnumConverter))] // This will make use of enums more resilient
    public Clothing ClothingChoice { get; set; } = Clothing.Pants;

    [Category("My category")]
    [DisplayName("This is a boolean")]
    [Description("The description of the property")]
    [DefaultValue(true)]
    [Browsable(false)] // This will hide it from the Tools -> Options page, but still work like normal
    public bool HiddenProperty { get; set; } = true;

  }
}