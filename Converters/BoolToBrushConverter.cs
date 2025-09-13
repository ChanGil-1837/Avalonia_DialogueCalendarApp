using Avalonia.Data.Converters;
using Avalonia.Controls;
using Avalonia.Media;
using System;
using System.Globalization;
using System.Collections.Generic;

namespace DialogueCalendarApp.Converters
{
    public static class BoolToBrushConverter
    {
        public static readonly IValueConverter CurrentMonthDayColor =
            new FuncValueConverter<bool, IBrush>(isCurrentMonth =>
                isCurrentMonth ? Brushes.White : Brushes.Gray);
    }

    public class ZeroToBorderThickness : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int day && day == 0)
                return new Avalonia.Thickness(0); // 굵기 0
            return new Avalonia.Thickness(0.6);   // 기본 굵기
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class ZeroToEmptyStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int day && day == 0)
                return ""; // 0이면 빈 문자열 반환
            return value?.ToString() ?? "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string s && int.TryParse(s, out var result))
                return result;
            return 0;
        }
    }

    public class ZeroToVisibilityConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int day && day == 0)
                return false;
            return true;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class TupleConverter : IMultiValueConverter
    {

        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            // 첫 번째 값은 int, 두 번째 값은 Window입니다.
            if (values.Count >= 2 && values[0] is int day && values[1] is Window window)
            {
                // C# 7.0 튜플 문법으로 반환
                return (day, window);
            }
            return null;
        }
    }
}
