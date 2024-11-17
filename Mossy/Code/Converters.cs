using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;

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
			Visibility hidden = Visibility.Collapsed;
			if (parameter is Visibility vis)
			{
				hidden = vis;
			}

			if (value == null)
			{
				return hidden;
			}
			if (value is string strValue && strValue.Length == 0)
			{
				return hidden;
			}
			if (value is ICollection collection && collection.Count == 0)
			{
				return hidden;
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

	[ValueConversion(typeof(object), typeof(Visibility))]
	public class IsNotLastItemVisibilityConverter : IMultiValueConverter
	{
		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			if (values.Length != 2)
				return Visibility.Collapsed;
			IList? list = values[0] as IList;
			if (list == null)
				return Visibility.Collapsed;
			object? elem = values[1];
			if (elem == null)
				return Visibility.Collapsed;
			bool isLastItem = list.IndexOf(elem) == (list.Count - 1);
			return isLastItem ? Visibility.Collapsed : Visibility.Visible;
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

	[ValueConversion(typeof(object), typeof(BitmapImage))]
	public class DocumentPathImageConverter : IMultiValueConverter
	{
		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			if (values.Length != 2)
				return DependencyProperty.UnsetValue;
			if (values[0] is not MossyDocument doc)
				return DependencyProperty.UnsetValue;
			if (values[1] is not IMossyDatabase db)
				return new Uri("about:blank");

			BitmapImage image = new();
			image.BeginInit();
			image.CacheOption = BitmapCacheOption.OnLoad;
			image.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
			image.UriSource = new Uri(db.GetAbsolutePath(doc), UriKind.Absolute);
			image.EndInit();
			return image;
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
