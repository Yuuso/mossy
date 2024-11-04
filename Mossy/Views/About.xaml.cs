using System;
using System.Windows;

namespace Mossy;


public partial class About : Window
{
	public About()
	{
		InitializeComponent();
	}

	private void License_Click(object sender, RoutedEventArgs e)
	{
		MossyUtils.OpenUrl("file://" + Environment.CurrentDirectory + "/License.txt");
	}

	private void ThirdParty_Click(object sender, RoutedEventArgs e)
	{
		MossyUtils.OpenUrl("file://" + Environment.CurrentDirectory + "/Third-Party-Notices.txt");
	}

	private void Source_Click(object sender, RoutedEventArgs e)
	{
		MossyUtils.OpenUrl("https://github.com/Yuuso/mossy");
	}
}
