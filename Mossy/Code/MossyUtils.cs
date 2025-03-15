using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Shapes;
using System.Xaml;

namespace Mossy;


internal static class MossyUtils
{
	public static bool IsValidUrl(string path)
	{
		return Uri.TryCreate(path, UriKind.Absolute, out Uri? result) &&
			(result.Scheme == Uri.UriSchemeHttps || result.Scheme == Uri.UriSchemeHttp);
	}

	public static void OpenUrl(string url)
	{
		var info = new ProcessStartInfo
		{
			UseShellExecute = true,
			FileName= url
		};
		try
		{
			Process.Start(info);
		}
		catch (Exception e)
		{
			MessageBox.Show(e.Message, "Can't open URL!", MessageBoxButton.OK);
		}
	}

	public static void ShowFileInExplorer(string path)
	{
		if (!File.Exists(path) && !Directory.Exists(path))
		{
			// TODO: prompt to fix moved paths
			MessageBox.Show($"\"{path}\" doesn't exist anymore.", "Can't open link!", MessageBoxButton.OK, MessageBoxImage.Error);
			return;
		}
		var info = new ProcessStartInfo
		{
			UseShellExecute = true,
			FileName = "explorer.exe",
			Arguments = $"/select,\"{path}\""
		};
		try
		{
			Process.Start(info);
		}
		catch (Exception e)
		{
			MessageBox.Show(e.Message, "Can't open Link!", MessageBoxButton.OK, MessageBoxImage.Error);
		}
	}

}
