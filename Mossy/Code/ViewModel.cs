﻿using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Reflection.Metadata;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace Mossy;


internal class ViewModel : NotifyPropertyChangedBase
{
	public ViewModel()
	{
		Database = new MossySQLiteDatabase();
		Projects.Source = Database.Projects;
		Projects.Filter += (sender, eventArgs) =>
		{
			eventArgs.Accepted = eventArgs.Item is MossyProject project &&
				project.Name.Contains(SearchFilter);
		};
		Tags.Source = Database.Tags;
		Tags.Filter += (sender, eventArgs) =>
		{
			eventArgs.Accepted = eventArgs.Item is MossyTag tag &&
				tag.Name.Contains(SearchFilter);
		};

		MediaPlayer = new MediaPlayerViewModel();

		ExitCommand = new DelegateCommand(ExitHandler);
		AboutCommand = new DelegateCommand(AboutHandler);
		NewDatabaseCommand = new DelegateCommand(NewDatabaseHandler);
		OpenDatabaseCommand = new DelegateCommand(OpenDatabaseHandler);
		CloseDatabaseCommand = new DelegateCommand(CloseDatabaseHandler);

		AddProjectCommand = new DelegateCommand(AddProjectHandler);
		SetProjectNameCommand = new DelegateCommand(SetProjectNameHandler);
		DeleteProjectCommand = new DelegateCommand(DeleteProjectHandler);
		AddProjectAltNameCommand = new DelegateCommand(AddProjectAltNameHandler);
		DeleteProjectAltNameCommand = new DelegateCommand(DeleteProjectAltNameHandler);
		AddProjectTagCommand = new DelegateCommand(AddProjectTagHandler);
		DeleteProjectTagCommand = new DelegateCommand(DeleteProjectTagHandler);

		AddTagCommand = new DelegateCommand(AddTagHandler);
		DeleteTagCommand = new DelegateCommand(DeleteTagHandler);
		RenameTagCommand = new DelegateCommand(RenameTagHandler);
		SetTagCategoryCommand = new DelegateCommand(SetTagCategoryHandler);

		SetDocumentNameCommand = new DelegateCommand(SetDocumentNameHandler);
		DeleteDocumentCommand = new DelegateCommand(DeleteDocumentHandler);
	}


	public ICommand? ExitCommand { get; }
	private void ExitHandler(object? param)
	{
		Application.Current.Shutdown();
	}

	public ICommand? AboutCommand { get; }
	private void AboutHandler(object? param)
	{
		var about = new About();
		about.ShowInTaskbar = false;
		about.Owner = Application.Current.MainWindow;
		about.ShowDialog();
	}

	public ICommand? NewDatabaseCommand { get; }
	private void NewDatabaseHandler(object? param)
	{
		Database.InitNew();
	}

	public ICommand? OpenDatabaseCommand { get; }
	private void OpenDatabaseHandler(object? param)
	{
		Database.InitOpen();
	}

	public ICommand? CloseDatabaseCommand { get; }
	private void CloseDatabaseHandler(object? param)
	{
		SelectedProject = null;
		SelectedTag = null;
		Database.Deinit();
	}


	public ICommand? AddProjectCommand { get; }
	private void AddProjectHandler(object? param)
	{
		Debug.Assert(param == null);
		var dialog = new TextInputDialog("New Project", "Project Name", "");
		var result = dialog.ShowDialog();
		if (result.HasValue && result.Value)
		{
			Database.AddProject(dialog.Result1);
		}
	}

	public ICommand? SetProjectNameCommand { get; }
	private void SetProjectNameHandler(object? param)
	{
		if (param == null || param is not MossyProject)
		{
			Debug.Assert(false, "Invalid SetProjectName parameter!");
			return;
		}
		MossyProject project = (MossyProject)param;
		var dialog = new TextInputDialog("Rename", "Project Name", project.Name);
		var result = dialog.ShowDialog();
		if (result.HasValue && result.Value)
		{
			if (dialog.Result1 == project.Name)
			{
				// Name didn't change
				return;
			}
			Database.SetProjectName(project, dialog.Result1);
		}
	}

	public ICommand? DeleteProjectCommand { get; }
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

	public ICommand? AddProjectAltNameCommand { get; }
	private void AddProjectAltNameHandler(object? param)
	{
		Debug.Assert(param == null);
		if (SelectedProject == null)
		{
			Debug.Assert(false, "No project selected!?");
			return;
		}
		var dialog = new TextInputDialog("Add Name", "Name", "");
		var result = dialog.ShowDialog();
		if (result.HasValue && result.Value)
		{
			Database.AddProjectAltName(SelectedProject, dialog.Result1);
		}
	}

	public ICommand? DeleteProjectAltNameCommand { get; }
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

	public ICommand? AddProjectTagCommand { get; }
	private void AddProjectTagHandler(object? param)
	{
		Debug.Assert(SelectedProject != null);
		if (param == null || param is not MossyTag)
		{
			Debug.Assert(false, "Invalid AddProjectTag parameter!");
			return;
		}
		MossyTag tag = (MossyTag)param;

		Database.AddProjectTag(SelectedProject, tag);
	}

	public ICommand? DeleteProjectTagCommand { get; }
	private void DeleteProjectTagHandler(object? param)
	{
		Debug.Assert(SelectedProject != null);
		if (param == null || param is not MossyTag)
		{
			Debug.Assert(false, "Invalid DeleteProjectTag parameter!");
			return;
		}
		MossyTag tag = (MossyTag)param;

		Database.DeleteProjectTag(SelectedProject, tag);
	}


	public ICommand? AddTagCommand { get; }
	private void AddTagHandler(object? param)
	{
		Debug.Assert(param == null);
		var dialog = new TextInputDialog("New Tag",
			"Tag Name", "", "Tag Category", "");
		var result = dialog.ShowDialog();
		if (result.HasValue && result.Value)
		{
			if (dialog.Result1.Length == 0)
			{
				MessageBox.Show(
					"Tag must have a name!",
					"Failed to create new tag",
					MessageBoxButton.OK, MessageBoxImage.Information);
				return;
			}
			Database.AddTag(dialog.Result1, dialog.Result2);
		}
	}

	public ICommand? DeleteTagCommand { get; }
	private void DeleteTagHandler(object? param)
	{
		if (param == null || param is not MossyTag)
		{
			Debug.Assert(false, "Invalid DeleteTag parameter!");
			return;
		}
		MossyTag tag = (MossyTag)param;
		Database.DeleteTag(tag);
	}

	public ICommand? RenameTagCommand { get; }
	private void RenameTagHandler(object? param)
	{
		if (param == null || param is not MossyTag)
		{
			Debug.Assert(false, "Invalid RenameTag parameter!");
			return;
		}
		MossyTag tag = (MossyTag)param;

		var dialog = new TextInputDialog("Rename", "Tag Name", tag.Name);
		var result = dialog.ShowDialog();
		if (result.HasValue && result.Value)
		{
			if (dialog.Result1 == tag.Name)
			{
				return;
			}
			Database.SetTagName(tag, dialog.Result1);
		}
	}

	public ICommand? SetTagCategoryCommand { get; }
	private void SetTagCategoryHandler(object? param)
	{
		if (param == null || param is not MossyTag)
		{
			Debug.Assert(false, "Invalid SetTagCategory parameter!");
			return;
		}
		MossyTag tag = (MossyTag)param;

		var dialog = new TextInputDialog("Set Category", "Tag Category", tag.Category);
		var result = dialog.ShowDialog();
		if (result.HasValue && result.Value)
		{
			if (dialog.Result1 == tag.Category)
			{
				return;
			}
			Database.SetTagCategory(tag, dialog.Result1);
		}
	}


	public ICommand? SetDocumentNameCommand { get; }
	private void SetDocumentNameHandler(object? param)
	{
		if (param == null || param is not MossyDocument)
		{
			Debug.Assert(false, "Invalid SetDocumentName parameter!");
			return;
		}
		MossyDocument document = (MossyDocument)param;

		if (document.Path.Type != MossyDocumentPathType.File)
		{
			MessageBox.Show("Cannot rename this type of document.", "Error!", MessageBoxButton.OK, MessageBoxImage.Exclamation);
			return;
		}

		string filename = Path.GetFileName(document.Path.Path);
		var dialog = new TextInputDialog("Rename", "File Name", filename);
		var result = dialog.ShowDialog();
		if (result.HasValue && result.Value)
		{
			if (dialog.Result1 == document.Path.Path)
			{
				// Name didn't change
				return;
			}
			bool success = Database.SetDocumentName(document, dialog.Result1);
			if (success)
			{
				string oldFileExt = Path.GetExtension(filename);
				string newFileExt = Path.GetExtension(dialog.Result1);
				if (oldFileExt != newFileExt)
				{
					MessageBox.Show(
						"File extension changed!" + Environment.NewLine + $"'{oldFileExt}' -> '{newFileExt}'",
						"Rename", MessageBoxButton.OK, MessageBoxImage.Warning);
				}
			}
		}
	}

	public ICommand? DeleteDocumentCommand { get; }
	private void DeleteDocumentHandler(object? param)
	{
		if (param == null || param is not MossyDocument)
		{
			Debug.Assert(false, "Invalid DeleteDocument parameter!");
			return;
		}
		MossyDocument document = (MossyDocument)param;
		Debug.Assert(SelectedProject != null || SelectedTag != null);

		var result = MessageBox.Show(
			$"Delete document '{document.Path.Path}'?" + Environment.NewLine + "This operation cannot be undone!",
			"Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
		if (result == MessageBoxResult.Yes)
		{
			if (SelectedProject != null)
			{
				Database.DeleteDocument(document, SelectedProject);
			}
			else
			{
				Debug.Assert(SelectedTag != null);
				Database.DeleteDocument(document, SelectedTag);
			}
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
		ObservableCollection<MossyDocument> documents;
		if (SelectedProject != null)
		{
			documents = SelectedProject.Documents;
		}
		else if (SelectedTag != null)
		{
			documents = SelectedTag.Documents;
		}
		else
		{
			return;
		}

		// Disallow drag and drop of files in current project
		if (e.Data.GetDataPresent(DataFormats.FileDrop))
		{
			string[] paths = (string[])e.Data.GetData(DataFormats.FileDrop);
			foreach (string path in paths)
			{
				foreach (var doc in documents)
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
		if (SelectedProject == null && SelectedTag != null)
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
		if (SelectedProject == null && SelectedTag == null)
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
			if (SelectedProject != null)
			{
				Database.AddDocument(SelectedProject, dataString);
			}
			else
			{
				Debug.Assert(SelectedTag != null);
				Database.AddDocument(SelectedTag, dataString);
			}
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
				if (SelectedProject != null)
				{
					Database.AddDocument(effect, SelectedProject, path);
				}
				else
				{
					Debug.Assert(SelectedTag != null);
					Database.AddDocument(effect, SelectedTag, path);
				}
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
					switch (extension)
					{

						default:
							MessageBox.Show(
								$"Handle {extension} file double click action.",
								"TODO!", MessageBoxButton.OK, MessageBoxImage.Information);
							break;

						case ".ogg":
							MessageBox.Show(
								$"File type {extension} not supported.",
								"Notice", MessageBoxButton.OK);
							break;

						case ".flac":
						case ".mp3":
						case ".wav":
							MediaPlayer.LoadMedia(new Uri(
								Database.GetAbsolutePath(doc),
								UriKind.Absolute));
							break;

					}
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
	public CollectionViewSource Projects { get; } = new();
	public CollectionViewSource Tags { get; } = new();

	private string searchFilter = "";
	public string SearchFilter
	{
		get => searchFilter;
		set
		{
			searchFilter = value;

			Projects.View.Refresh();
			Tags.View.Refresh();
		}
	}

	private MossyProject? selectedProject;
	public MossyProject? SelectedProject
	{
		get => selectedProject;
		set
		{
			selectedProject = value;
			OnPropertyChanged(nameof(SelectedProject));

			selectedTag = null;
			OnPropertyChanged(nameof(SelectedTag));
		}
	}

	private MossyTag? selectedTag;
	public MossyTag? SelectedTag
	{
		get => selectedTag;
		set
		{
			selectedTag = value;
			OnPropertyChanged(nameof(SelectedTag));

			selectedProject = null;
			OnPropertyChanged(nameof(SelectedProject));
		}
	}

	public bool AutoOpenLastDatabase
	{
		get => UserSettings.Instance.AutoOpenLastDatabase;
		set
		{
			UserSettings.Instance.AutoOpenLastDatabase = !UserSettings.Instance.AutoOpenLastDatabase;
			OnPropertyChanged();
		}
	}

	public MediaPlayerViewModel MediaPlayer { get; }
}
