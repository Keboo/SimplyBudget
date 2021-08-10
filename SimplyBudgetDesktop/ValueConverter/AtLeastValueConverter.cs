using System;
using System.Collections;
using System.Globalization;
using System.Windows.Data;

namespace SimplyBudget.ValueConverter
{
    public class AtLeastValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int compareValue = 0;
            if(parameter is not null)
            {
                compareValue = System.Convert.ToInt32(parameter);
            }
            if (value is ICollection collection)
            {
                return collection.Count >= compareValue;
            }
            if (value is int intVaue)
            {
                return intVaue >= compareValue;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
