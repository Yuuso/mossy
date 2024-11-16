using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace Mossy
{
	[ValueConversion(typeof(MossyDocument), typeof(Visibility))]
	public class RenameVisibilityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == null) return Visibility.Visible;
			MossyDocument doc = (MossyDocument)value;
			if (doc.Path.Type == MossyDocumentPathType.File)
			{
				return Visibility.Visible;
			}
			return Visibility.Collapsed;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

	[ValueConversion(typeof(object), typeof(Visibility))]
	public class VisibilityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == null)
			{
				return Visibility.Collapsed;
			}
			if (value is string strValue && strValue.Length == 0)
			{
				return Visibility.Collapsed;
			}
			return Visibility.Visible;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

	[ValueConversion(typeof(object), typeof(bool))]
	public class NotNullConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return value != null;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
