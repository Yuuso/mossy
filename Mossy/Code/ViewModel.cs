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
		Database.AddProject((string)param);
	}

	private static DragDropEffects GetDragEffect()
	{
		if (Keyboard.IsKeyDown(Key.LeftAlt) ||
			Keyboard.IsKeyDown(Key.RightAlt))
		{
			return DragDropEffects.Link;
		}
		return DragDropEffects.Copy;
	}
	public void DocumentDragOver(DragEventArgs e)
	{
		Debug.Assert(
			e.AllowedEffects.HasFlag(DragDropEffects.Copy) &&
			e.AllowedEffects.HasFlag(DragDropEffects.Link));
		e.Effects = DragDropEffects.None;
		if (SelectedProject != null)
		{
			if (e.Data.GetDataPresent(DataFormats.StringFormat))
			{
				e.Effects = DragDropEffects.Link;
			}
			else if (e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				e.Effects = GetDragEffect();
			}
		}
		e.Handled = true;
	}
	public void DocumentDrop(DragEventArgs e)
	{
		if (!SelectedProject.HasValue)
		{
			return;
		}
		if (e.Data.GetDataPresent(DataFormats.StringFormat))
		{
			string dataString = (string)e.Data.GetData(DataFormats.StringFormat);
			Database.AddDocumentString(SelectedProject.Value, dataString);
			e.Handled = true;
			OnPropertyChanged(nameof(SelectedProject));
		}
		else if (e.Data.GetDataPresent(DataFormats.FileDrop))
		{
			string[] paths = (string[])e.Data.GetData(DataFormats.FileDrop);
			foreach (string path in paths)
			{ Database.AddDocumentFile(GetDragEffect(), SelectedProject.Value, path); }
			e.Handled = true;
			OnPropertyChanged(nameof(SelectedProject));
		}
	}

	public bool NewProjectPopup { get; set; } = false;

	public IMossyDatabase Database { get; }

	private MossyProject? selectedProject;
	public MossyProject? SelectedProject
	{
		get { return selectedProject; }
		set { selectedProject = value; OnPropertyChanged(nameof(SelectedProject)); }
	}

	public ICommand? NewDatabaseCommand { get; private set; }
	public ICommand? OpenDatabaseCommand { get; private set; }
	public ICommand? CloseDatabaseCommand { get; private set; }
	public ICommand? NewProjectCommand { get; private set; }
	public ICommand? NewProjectPopupCommand { get; private set; }
}
