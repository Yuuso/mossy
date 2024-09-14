using System;
using System.Diagnostics;
using System.IO;
using System.Reflection.Metadata;
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
		RenameProjectCommand = new DelegateCommand(RenameProjectHandler);
		DeleteProjectCommand = new DelegateCommand(DeleteProjectHandler);
		AddProjectAltNameCommand = new DelegateCommand(AddProjectAltNameHandler);
		DeleteProjectAltNameCommand = new DelegateCommand(DeleteProjectAltNameHandler);
		RenameDocumentCommand = new DelegateCommand(RenameDocumentHandler);
		DeleteDocumentCommand = new DelegateCommand(DeleteDocumentHandler);
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


	private void NewProjectHandler(object? param)
	{
		Debug.Assert(param == null);
		var dialog = new TextInputDialog("New Project");
		var result = dialog.ShowDialog();
		if (result.HasValue && result.Value)
		{
			Database.AddProject(dialog.Result);
		}
	}

	private void RenameProjectHandler(object? param)
	{
		if (param == null || param is not MossyProject)
		{
			Debug.Assert(false, "Invalid RenameProject parameter!");
			return;
		}
		MossyProject project = (MossyProject)param;
		var dialog = new TextInputDialog("Rename", project.Name);
		var result = dialog.ShowDialog();
		if (result.HasValue && result.Value)
		{
			if (dialog.Result == project.Name)
			{
				// Name didn't change
				return;
			}
			Database.RenameProject(project, dialog.Result);
		}
	}

	private void DeleteProjectHandler(object? param)
	{
		if (param == null || param is not MossyProject)
		{
			Debug.Assert(false, "Invalid DeleteProject parameter!");
			return;
		}
		MossyProject project = (MossyProject)param;
		var result = MessageBox.Show(
			$"Delete project '{project.Name}'?" + Environment.NewLine + "This operation cannot be undone!",
			"Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
		if (result == MessageBoxResult.Yes)
		{
			if (SelectedProject == project)
			{
				SelectedProject = null;
			}
			Database.DeleteProject(project);
		}
	}

	private void AddProjectAltNameHandler(object? param)
	{
		Debug.Assert(param == null);
		if (SelectedProject == null)
		{
			Debug.Assert(false, "No project selected!?");
			return;
		}
		var dialog = new TextInputDialog("Add Alt Name");
		var result = dialog.ShowDialog();
		if (result.HasValue && result.Value)
		{
			Database.AddProjectAltName(SelectedProject, dialog.Result);
		}
	}

	private void DeleteProjectAltNameHandler(object? param)
	{
		if (param == null || param is not string)
		{
			Debug.Assert(false, "Invalid DeleteProjectAltName parameter!");
			return;
		}
		if (SelectedProject == null)
		{
			Debug.Assert(false, "No project selected!?");
			return;
		}
		Database.DeleteProjectAltName(SelectedProject, (string)param);
	}


	private void RenameDocumentHandler(object? param)
	{
		if (param == null || param is not MossyDocument)
		{
			Debug.Assert(false, "Invalid RenameDocument parameter!");
			return;
		}
		Debug.Assert(SelectedProject != null);
		MossyDocument document = (MossyDocument)param;
		if (document.Path.Type != MossyDocumentPathType.File)
		{
			MessageBox.Show("Cannot rename this type of document.", "Error!", MessageBoxButton.OK, MessageBoxImage.Exclamation);
			return;
		}
		string filename = Path.GetFileName(document.Path.Path);
		var dialog = new TextInputDialog("Rename", filename);
		var result = dialog.ShowDialog();
		if (result.HasValue && result.Value)
		{
			if (dialog.Result == document.Path.Path)
			{
				// Name didn't change
				return;
			}
			bool success = Database.RenameDocument(document, dialog.Result);
			if (success)
			{
				string oldFileExt = Path.GetExtension(filename);
				string newFileExt = Path.GetExtension(dialog.Result);
				if (oldFileExt != newFileExt)
				{
					MessageBox.Show(
						"File extension changed!" + Environment.NewLine + $"'{oldFileExt}' -> '{newFileExt}'",
						"Rename", MessageBoxButton.OK, MessageBoxImage.Warning);
				}
			}
		}
	}

	private void DeleteDocumentHandler(object? param)
	{
		if (param == null || param is not MossyDocument)
		{
			Debug.Assert(false, "Invalid DeleteDocument parameter!");
			return;
		}
		Debug.Assert(SelectedProject != null);
		MossyDocument document = (MossyDocument)param;
		var result = MessageBox.Show(
			$"Delete document '{document.Path.Path}'?" + Environment.NewLine + "This operation cannot be undone!",
			"Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
		if (result == MessageBoxResult.Yes)
		{
			Database.DeleteDocument(document, SelectedProject);
		}
	}


	private static DragDropEffects GetDragEffect(DragDropEffects allowed)
	{
		if (Keyboard.IsKeyDown(Key.LeftAlt) ||
			Keyboard.IsKeyDown(Key.RightAlt))
		{
			if (allowed.HasFlag(DragDropEffects.Link))
			{
				return DragDropEffects.Link;
			}
		}
		if (allowed.HasFlag(DragDropEffects.Copy))
		{
			return DragDropEffects.Copy;
		}
		return DragDropEffects.None;
	}

	public void PreviewDocumentDragDrops(DragEventArgs e)
	{
		// Disallow drag and drop of files in current project
		if (SelectedProject == null)
		{
			return;
		}
		if (e.Data.GetDataPresent(DataFormats.FileDrop))
		{
			string[] paths = (string[])e.Data.GetData(DataFormats.FileDrop);
			foreach (string path in paths)
			{
				foreach (var doc in SelectedProject.Documents)
				{
					if (doc.Path.Type == MossyDocumentPathType.File &&
						path == Database.GetAbsolutePath(doc))
					{
						e.Effects = DragDropEffects.None;
						e.Handled = true;
						return;
					}
				}
			}
		}
	}

	public void DocumentDragOver(DragEventArgs e)
	{
		if (SelectedProject == null)
		{
			return;
		}

		e.Effects = DragDropEffects.None;
		if (e.Data.GetDataPresent(DataFormats.StringFormat))
		{
			if (e.AllowedEffects.HasFlag(DragDropEffects.Link))
			{
				e.Effects = DragDropEffects.Link;
			}
		}
		else if (e.Data.GetDataPresent(DataFormats.FileDrop))
		{
			e.Effects = GetDragEffect(e.AllowedEffects);
		}
	}

	public void DocumentDrop(DragEventArgs e)
	{
		if (SelectedProject == null)
		{
			return;
		}

		if (e.Data.GetDataPresent(DataFormats.StringFormat))
		{
			if (!e.AllowedEffects.HasFlag(DragDropEffects.Link))
			{
				return;
			}

			string dataString = (string)e.Data.GetData(DataFormats.StringFormat);
			Database.AddDocumentString(SelectedProject, dataString);
		}
		else if (e.Data.GetDataPresent(DataFormats.FileDrop))
		{
			var effect = GetDragEffect(e.AllowedEffects);
			if (effect == DragDropEffects.None)
			{
				return;
			}

			string[] paths = (string[])e.Data.GetData(DataFormats.FileDrop);
			foreach (string path in paths)
			{
				Database.AddDocumentFile(effect, SelectedProject, path);
			}
		}
	}

	public void DocumentDoDragDrop(MossyDocument doc, DependencyObject source)
	{
		if (doc.Path.Type != MossyDocumentPathType.File)
		{
			return;
		}

		string[] paths = { Database.GetAbsolutePath(doc) };
		var dataObject = new DataObject(DataFormats.FileDrop, paths);
		DragDrop.DoDragDrop(source, dataObject, DragDropEffects.Copy);
	}

	public void DocumentDoubleClick(MossyDocument doc)
	{
		switch (doc.Path.Type)
		{
			default:
				Debug.Assert(false);
				break;

			case MossyDocumentPathType.File:
				{
					string? extension = Path.GetExtension(doc.Path.Path);
					Debug.Assert(extension != null);
					MessageBox.Show($"Handle {extension} file double click action.", "TODO!", MessageBoxButton.OK, MessageBoxImage.Information);
					//switch (extension)
					//{
					//	case ".":
					//		break;
					//}
				}
				break;
			case MossyDocumentPathType.Link:
				MossyUtils.ShowFileInExplorer(doc.Path.Path);
				break;
			case MossyDocumentPathType.Url:
				MossyUtils.OpenUrl(doc.Path.Path);
				break;
		}
	}


	public IMossyDatabase Database { get; }

	private MossyProject? selectedProject;
	public MossyProject? SelectedProject
	{
		get => selectedProject;
		set { selectedProject = value; OnPropertyChanged(); }
	}

	public ICommand? NewDatabaseCommand { get; private set; }
	public ICommand? OpenDatabaseCommand { get; private set; }
	public ICommand? CloseDatabaseCommand { get; private set; }
	public ICommand? NewProjectCommand { get; private set; }
	public ICommand? RenameProjectCommand { get; private set; }
	public ICommand? DeleteProjectCommand { get; private set; }
	public ICommand? AddProjectAltNameCommand { get; private set; }
	public ICommand? DeleteProjectAltNameCommand { get; private set; }
	public ICommand? RenameDocumentCommand { get; private set; }
	public ICommand? DeleteDocumentCommand { get; private set; }
}
