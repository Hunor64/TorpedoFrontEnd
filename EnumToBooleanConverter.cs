using System;
using System.Globalization;
using System.Windows.Data;

namespace TorpedoFrontEnd
{
    public class EnumToBooleanConverter : IValueConverter
    {
        // Converts Enum to Boolean
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter == null)
                return false;

            string parameterString = parameter.ToString();
            if (parameterString == null)
                return false;

            return value.ToString().Equals(parameterString, StringComparison.InvariantCultureIgnoreCase);
        }

        // Converts Boolean back to Enum
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter == null)
                return null;

            string parameterString = parameter.ToString();
            if (parameterString == null)
                return null;

            return Enum.Parse(targetType, parameterString);
        }
    }
}