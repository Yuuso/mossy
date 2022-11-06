using System.Windows;

namespace Mossy;

public partial class App : Application
{
	protected override void OnStartup(StartupEventArgs e)
	{
		base.OnStartup(e);

		MainWindow window = new()
		{
			DataContext = new ViewModel()
		};
		window.Show();
	}
}
