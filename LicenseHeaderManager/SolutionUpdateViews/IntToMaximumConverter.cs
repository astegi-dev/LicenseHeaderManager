using System;
using System.Globalization;
using System.Windows.Data;

namespace LicenseHeaderManager.SolutionUpdateViews
{
  internal class IntToMaximumConverter : IValueConverter
  {
    public object Convert (object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (!(value is int intValue))
        return 0;

      return intValue == 0 ? 1 : intValue;
    }

    public object ConvertBack (object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}
