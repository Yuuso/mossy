using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Mossy;

public partial class DatePickerDialog : Window
{
	public DatePickerDialog(
		string title,
		string label)
	{
		InitializeComponent();
		Title = title;
		Label.Content = label;
	}

	private void OKButton_Click(object sender, RoutedEventArgs e)
	{
		DialogResult = true;
	}

	public DateTime? Result
	{
		get { return Calendar.SelectedDate; }
	}

	private void Calendar_GotMouseCapture(object sender, MouseEventArgs e)
	{
		UIElement? originalElement = e.OriginalSource as UIElement;
		if (originalElement is CalendarDayButton || originalElement is CalendarItem)
		{
			originalElement.ReleaseMouseCapture();
		}
	}
}
