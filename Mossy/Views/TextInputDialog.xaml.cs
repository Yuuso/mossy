using System;
using System.Windows;

namespace Mossy;


public partial class TextInputDialog : Window
{
	public TextInputDialog(
		string title,
		string label,
		string input)
	{
		InitializeComponent();
		Title = title;
		Label1.Content = label;
		Input1.Text = input;
		Label2.Visibility = Visibility.Collapsed;
		Input2.Visibility = Visibility.Collapsed;
	}
	public TextInputDialog(
		string title,
		string label1,
		string input1,
		string label2,
		string input2)
	{
		InitializeComponent();
		Title = title;
		Label1.Content = label1;
		Input1.Text = input1;
		Label2.Content = label2;
		Input2.Text = input2;
	}

	private void OKButton_Click(object sender, RoutedEventArgs e)
	{
		DialogResult = true;
	}

	private void Window_ContentRendered(object sender, EventArgs e)
	{
		Input1.SelectAll();
		Input1.Focus();
	}

	public string Result1
	{
		get { return Input1.Text; }
	}
	public string Result2
	{
		get { return Input2.Text; }
	}
}
