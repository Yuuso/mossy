using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace Mossy;

internal class ViewModel : NotifyPropertyChangedBase
{
	public ViewModel()
	{
		Database = new MossySQLiteDatabase();
		NewDatabaseCommand = new DelegateCommand(NewDatabaseHandler);
		OpenDatabaseCommand = new DelegateCommand(OpenDatabaseHandler);
		CloseDatabaseCommand = new DelegateCommand(CloseDatabaseHandler);
		NewProjectCommand = new DelegateCommand(NewProjectHandler);
		NewProjectPopupCommand = new DelegateCommand(NewProjectPopupCommandHandler);
	}
	private void NewDatabaseHandler(object? param)
	{
		Database.InitNew();
	}
	private void OpenDatabaseHandler(object? param)
	{
		Database.InitOpen();
	}
	private void CloseDatabaseHandler(object? param)
	{
		Database.Deinit();
	}
	private void NewProjectPopupCommandHandler(object? param)
	{
		NewProjectPopup = true;
		OnPropertyChanged(nameof(NewProjectPopup));
	}
	private void NewProjectHandler(object? param)
	{
		NewProjectPopup = false;
		OnPropertyChanged(nameof(NewProjectPopup));

		if (param == null || param is not string)
		{
			Debug.Assert(false, "Invalid NewProject parameter!");
			return;
		}
		string projectName = (string)param;
		if (projectName.Length == 0)
		{
			MessageBox.Show("Project name cannot be empty.", "Error!", MessageBoxButton.OK);
			return;
		}
		Database.AddProject(projectName);
	}

	public bool NewProjectPopup { get; set; } = false;

	public IMossyDatabase Database { get; }

	public ICommand? NewDatabaseCommand { get; private set; }
	public ICommand? OpenDatabaseCommand { get; private set; }
	public ICommand? CloseDatabaseCommand { get; private set; }
	public ICommand? NewProjectCommand { get; private set; }
	public ICommand? NewProjectPopupCommand { get; private set; }
}
