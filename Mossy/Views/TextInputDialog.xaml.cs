using System;
using System.Windows;

namespace Mossy;


public partial class TextInputDialog : Window
{
	public TextInputDialog(string title = "", string defaultInput = "")
	{
		InitializeComponent();
		Title = title;
		InputTextBox.Text = defaultInput;
	}

	private void OKButton_Click(object sender, RoutedEventArgs e)
	{
		DialogResult = true;
	}

	private void Window_ContentRendered(object sender, EventArgs e)
	{
		InputTextBox.SelectAll();
		InputTextBox.Focus();
	}

	public string Result
	{
		get { return InputTextBox.Text; }
	}
}
