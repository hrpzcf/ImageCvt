using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace WebpCvt
{
    internal class FullPathToDirNameCvt : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string fullPath)
            {
                return Path.GetFileName(fullPath);
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    internal class PlaceHolderTextVisibilityCvt : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!string.IsNullOrEmpty(value as string))
            {
                return Visibility.Hidden;
            }
            else
            {
                return Visibility.Visible;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    internal class LogResultToStringCvt : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool result)
            {
                return result ? "成功" : "失败";
            }
            return default(string);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    internal class LogResultToBackgroundCvt : IValueConverter
    {
        private static readonly SolidColorBrush green = new SolidColorBrush(Colors.Green);
        private static readonly SolidColorBrush red = new SolidColorBrush(Colors.Red);
        private static readonly SolidColorBrush transparent = new SolidColorBrush(Colors.Transparent);

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool result)
            {
                return result ? green : red;
            }
            return transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    internal class DateTimeToStringCvt : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime dateTime)
            {
                return dateTime.ToString("yy/MM/dd HH:mm:ss");
            }
            return default(string);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    internal class GetFirstLineOfStringCvt : IValueConverter
    {
        private static readonly char[] lineBreaks = new char[] { '\r', '\n' };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string lines)
            {
                int index;
                if ((index = lines.IndexOfAny(lineBreaks)) != -1)
                {
                    StringBuilder stringBuilder = new StringBuilder(lines, 0, index, index + 5);
                    if (stringBuilder[stringBuilder.Length - 1] == '\n')
                    {
                        stringBuilder.Remove(stringBuilder.Length - 1, 1);
                    }
                    stringBuilder.Append("  ...");
                    return stringBuilder.ToString();
                }
                return lines;
            }
            return default(string);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    internal class GetDifferenceBetweenTwoNumbersCvt : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            Debug.Assert(values?.Length == 2);
            if (values[0] is int smaller && values[1] is int bigger)
            {
                return (bigger - smaller).ToString();
            }
            return "0";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    internal class BoolToTextBlockStrikeThroughCvt : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolean && boolean)
            {
                return TextDecorations.Strikethrough;
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    internal class OutTypePngToTureCvt : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is OutType outType && outType == OutType.PNG;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool boolean && boolean ? OutType.PNG : default(OutType);
        }
    }

    internal class OutTypeJpegToTureCvt : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is OutType outType && outType == OutType.JPG;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool boolean && boolean ? OutType.JPG : default(OutType);
        }
    }

    internal class OutTypeBmpToTureCvt : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is OutType outType && outType == OutType.BMP;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool boolean && boolean ? OutType.BMP : default(OutType);
        }
    }
}
