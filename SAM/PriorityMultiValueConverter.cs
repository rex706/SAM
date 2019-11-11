using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace SAM
{
    class PriorityMultiValueConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return values.FirstOrDefault(o => o != null && !string.Empty.Equals(o));
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
