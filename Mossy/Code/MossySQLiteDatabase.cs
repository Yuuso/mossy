using Microsoft.Data.Sqlite;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Reflection.Metadata;
using System.Windows;

namespace Mossy;


internal class MossySQLiteDatabase : NotifyPropertyChangedBase, IMossyDatabase
{
	public MossySQLiteDatabase()
	{
		if (UserSettings.Instance.AutoOpenLastDatabase && UserSettings.Instance.LastDatabaseFolderPath.Length > 0)
		{
			if (Directory.Exists(UserSettings.Instance.LastDatabaseFolderPath))
			{
				InitOpen(UserSettings.Instance.LastDatabaseFolderPath);
			}
		}
	}

	public void InitNew()
	{
		Debug.Assert(!Initialized);

		string? folder = config.InitNew();
		if (folder == null)
		{
			return;
		}

		databasePath = Path.Join(folder, databaseFilename);

		if (File.Exists(databasePath))
		{
			MessageBox.Show("Database already exists!", "Error", MessageBoxButton.OK);
			return;
		}

		if (!NewDatabase())
		{
			Deinit();
			return;
		}

		Initialized = true;
	}
	public void InitOpen()
	{
		InitOpen(null);
	}
	private void InitOpen(string? param)
	{
		Debug.Assert(!Initialized);

		string? folder = config.InitOpen(param);
		if (folder == null)
		{
			return;
		}

		databasePath = Path.Join(folder, databaseFilename);

		if (!File.Exists(databasePath))
		{
			MessageBox.Show("Database doesn't exist!", "Error", MessageBoxButton.OK);
			return;
		}

		if (!LoadDatabase())
		{
			Deinit();
			return;
		}

		Initialized = true;
	}
	public void Deinit()
	{
		Initialized = false;
		Tags.Clear();
		Projects.Clear();
		config.Deinit();
		databasePath = null;
	}

	private void OnDatabaseLoaded()
	{
		var folder = Path.GetDirectoryName(databasePath);
		if (folder != null)
		{
			UserSettings.Instance.LastDatabaseFolderPath = folder;
		}
	}
	private static bool ValidateProjectName(string projectName)
	{
		if (projectName.Length <= 0)
		{
			MessageBox.Show("Project name cannot be empty", "Invalid project name!", MessageBoxButton.OK);
			return false;
		}
		if (projectName.Contains(altNamesSeparator))
		{
			MessageBox.Show($"Project name cannot contain {altNamesSeparator}", "Invalid project name!", MessageBoxButton.OK);
			return false;
		}
		return true;
	}

	private bool NewDatabase()
	{
		Debug.Assert(databasePath != null);
		using var connection = new SqliteConnection($"DataSource={databasePath};Mode=ReadWriteCreate");
		try
		{
			connection.Open();
		}
		catch (SqliteException e)
		{
			MessageBox.Show(e.Message, "Failed to open database!", MessageBoxButton.OK);
			return false;
		}

		using var command = connection.CreateCommand();
		command.CommandText =
		@"
			CREATE TABLE projects (
				project_id		INTEGER NOT NULL PRIMARY KEY,
				project_name	TEXT	NOT NULL,
				alt_names		TEXT,
				date_created	TEXT	NOT NULL
			);

			CREATE TABLE documents (
				document_id		INTEGER NOT NULL PRIMARY KEY,
				path			TEXT	NOT NULL,
				date_created	TEXT	NOT NULL
			);

			CREATE TABLE tags (
				tag_id			INTEGER NOT NULL PRIMARY KEY,
				name			TEXT	NOT NULL,
				category		TEXT	NOT NULL,
				date_created	TEXT	NOT NULL
			);

			CREATE TABLE project_tag (
				project_id		INTEGER NOT NULL,
				tag_id			INTEGER NOT NULL
			);

			CREATE TABLE project_document (
				project_id		INTEGER NOT NULL,
				document_id		INTEGER NOT NULL
			);

			CREATE TABLE tag_document (
				tag_id			INTEGER NOT NULL,
				document_id		INTEGER NOT NULL
			);
		";
		try
		{
			command.ExecuteNonQuery();
		}
		catch (SqliteException e)
		{
			MessageBox.Show(e.Message, "Failed to initialize database tables!", MessageBoxButton.OK);
			return false;
		}

		OnDatabaseLoaded();
		return true;
	}
	private bool LoadDatabase()
	{
		Debug.Assert(databasePath != null);
		using var connection = new SqliteConnection($"DataSource={databasePath};Mode=ReadOnly");
		try
		{
			connection.Open();
		}
		catch (SqliteException e)
		{
			MessageBox.Show(e.Message, "Failed to open database!", MessageBoxButton.OK);
			return false;
		}

		using var command = connection.CreateCommand();
		command.CommandText =
		@"
			SELECT * FROM projects;
		";

		try
		{
			using var reader = command.ExecuteReader();
			while (reader.Read())
			{
				var project = new MossyProject();

				var idOrd = reader.GetOrdinal("project_id");
				Debug.Assert(!reader.IsDBNull(idOrd));
				project.ProjectId = reader.GetInt64(idOrd);

				var nameOrd = reader.GetOrdinal("project_name");
				Debug.Assert(!reader.IsDBNull(nameOrd));
				project.Name = reader.GetString(nameOrd);

				var altNamesOrd = reader.GetOrdinal("alt_names");
				if (!reader.IsDBNull(altNamesOrd))
				{
					var altNames = reader.GetString(altNamesOrd);
					var altNamesArray = altNames.Split(altNamesSeparator);
					foreach (var altName in altNamesArray)
					{
						project.AltNames.Add(altName);
					}
				}

				var dateCreatedOrd = reader.GetOrdinal("date_created");
				Debug.Assert(!reader.IsDBNull(dateCreatedOrd));
				project.DateCreated = reader.GetDateTimeOffset(dateCreatedOrd).DateTime;

				project.Tags = new();
				project.Documents = new();

				Projects.Add(project);
			}
		}
		catch (Exception e)
		{
			MessageBox.Show(e.Message, "Failed to load database!", MessageBoxButton.OK);
			return false;
		}

		command.CommandText =
		@"
			SELECT document_id FROM project_document
			WHERE project_id = $projectId;
		";
		for (int i = 0; i < Projects.Count; i++)
		{
			var project = Projects[i];
			command.Parameters.Clear();
			command.Parameters.AddWithValue("$projectId", project.ProjectId);

			try
			{
				using var reader = command.ExecuteReader();
				while (reader.Read())
				{
					var idOrd = reader.GetOrdinal("document_id");
					Debug.Assert(!reader.IsDBNull(idOrd));
					var documentId = reader.GetInt64(idOrd);

					using var command2 = connection.CreateCommand();
					command2.CommandText =
					@"
						SELECT * FROM documents
						WHERE document_id = $documentId;
					";
					command2.Parameters.AddWithValue("$documentId", documentId);

					try
					{
						using var reader2 = command2.ExecuteReader();
						if (!reader2.Read())
						{
							MessageBox.Show($"Document {documentId} not found in database", "Error!", MessageBoxButton.OK);
							continue;
						}
						MossyDocument document = new()
						{
							DocumentId = documentId
						};

						var pathOrd = reader2.GetOrdinal("path");
						Debug.Assert(!reader2.IsDBNull(pathOrd));
						document.Path = new MossyDocumentPath(reader2.GetString(pathOrd));

						var dateCreatedOrd = reader2.GetOrdinal("date_created");
						Debug.Assert(!reader2.IsDBNull(dateCreatedOrd));
						document.DateCreated = reader2.GetDateTimeOffset(dateCreatedOrd).DateTime;

						project.Documents.Add(document);
						Debug.Assert(!reader2.Read());
					}
					catch (Exception e)
					{
						MessageBox.Show(e.Message, "Failed to load database!", MessageBoxButton.OK);
						return false;
					}
				}
			}
			catch (Exception e)
			{
				MessageBox.Show(e.Message, "Failed to load database!", MessageBoxButton.OK);
				return false;
			}
		}

		OnDatabaseLoaded();
		return true;
	}
	public void AddProject(string projectName)
	{
		if (!ValidateProjectName(projectName))
		{
			return;
		}
		Debug.Assert(databasePath != null);
		using var connection = new SqliteConnection($"DataSource={databasePath};Mode=ReadWrite");
		try
		{
			connection.Open();
		}
		catch (SqliteException e)
		{
			MessageBox.Show(e.Message, "Failed to connect to database!", MessageBoxButton.OK);
			return;
		}

		var projectDateCreated = DateTimeOffset.Now;
		long projectId;

		using var command = connection.CreateCommand();
		command.CommandText =
		@"
			INSERT INTO projects (project_name, date_created)
			VALUES ($name, $date);

			SELECT last_insert_rowid();
		";
		command.Parameters.AddWithValue("$name", projectName);
		command.Parameters.AddWithValue("$date", projectDateCreated);
		try
		{
			var result = command.ExecuteScalar();
			Debug.Assert(result != null);
			projectId = (long)result;
			Debug.Assert(projectId > 0);
		}
		catch (SqliteException e)
		{
			MessageBox.Show(e.Message, "Failed to add new project!", MessageBoxButton.OK);
			return;
		}

		var project = new MossyProject
		{
			ProjectId = projectId,
			Name = projectName,
			DateCreated = projectDateCreated.DateTime,
			Tags = new(),
			Documents = new()
		};
		Projects.Add(project);
	}
	private bool AddDocument(MossyDocumentPath documentPath, MossyProject project)
	{
		Debug.Assert(databasePath != null);
		if (documentPath.Type == MossyDocumentPathType.Unknown)
		{
			MessageBox.Show("Unknown document type.", "Failed to add document!", MessageBoxButton.OK);
			return false;
		}

		using var connection = new SqliteConnection($"DataSource={databasePath};Mode=ReadWrite");
		try
		{
			connection.Open();
		}
		catch (SqliteException e)
		{
			MessageBox.Show(e.Message, "Failed to connect to database!", MessageBoxButton.OK);
			return false;
		}

		using var transaction = connection.BeginTransaction();

		var documentDateCreated = DateTimeOffset.Now;
		long documentId;

		using var command = connection.CreateCommand();
		command.CommandText =
		@"
			INSERT INTO documents (path, date_created)
			VALUES ($path, $date);

			SELECT last_insert_rowid();
		";
		command.Parameters.AddWithValue("$path", documentPath.RawPath);
		command.Parameters.AddWithValue("$date", documentDateCreated);
		try
		{
			var result = command.ExecuteScalar();
			Debug.Assert(result != null);
			documentId = (long)result;
			Debug.Assert(documentId > 0);
		}
		catch (SqliteException e)
		{
			MessageBox.Show(e.Message, "Failed to add new dpcument!", MessageBoxButton.OK);
			transaction.Rollback();
			return false;
		}

		command.CommandText =
		@"
			INSERT INTO project_document (project_id, document_id)
			VALUES ($projectId, $documentId);
		";
		command.Parameters.Clear();
		command.Parameters.AddWithValue("$projectId", project.ProjectId);
		command.Parameters.AddWithValue("$documentId", documentId);
		try
		{
			command.ExecuteNonQuery();
		}
		catch (SqliteException e)
		{
			MessageBox.Show(e.Message, "Failed to link document to project!", MessageBoxButton.OK);
			transaction.Rollback();
			return false;
		}

		transaction.Commit();

		var document = new MossyDocument
		{
			DocumentId = documentId,
			Path = documentPath,
			DateCreated = documentDateCreated.DateTime
		};
		project.Documents.Add(document);
		return true;
	}
	public void AddDocumentFile(DragDropEffects operation, MossyProject project, string path)
	{
		if (Directory.Exists(path))
		{
			AddDocument(new(MossyDocumentPathType.Link, path), project);
		}
		else if (File.Exists(path))
		{
			var extension = Path.GetExtension(path);
			if (extension == null || extension?.Length == 0)
			{
				MessageBox.Show("Invalid file extension.", "Failed to add document!", MessageBoxButton.OK);
				return;
			}

			if (extension == ".url")
			{
				string contents;
				try
				{
					contents = File.ReadAllText(path);
				}
				catch (Exception e)
				{
					MessageBox.Show(e.Message, "Failed to add document!", MessageBoxButton.OK);
					return;
				}

				string[] splitContent = contents.Split("URL=", StringSplitOptions.TrimEntries);
				if (splitContent.Length == 2 && splitContent[0].Equals("[InternetShortcut]") && MossyUtils.IsValidUrl(splitContent[1]))
				{
					AddDocument(new(MossyDocumentPathType.Url, splitContent[1]), project);
				}
				else
				{
					MessageBox.Show("Invalid .url file contents.", "Failed to add document!", MessageBoxButton.OK);
					return;
				}
			}
			else if (extension == ".lnk")
			{
				MessageBox.Show("Shortcut (.lnk) file type is not supported.", "Failed to add document!", MessageBoxButton.OK);
			}
			else
			{
				if (operation == DragDropEffects.Link)
				{
					AddDocument(new(MossyDocumentPathType.Link, path), project);
				}
				else if (operation == DragDropEffects.Copy)
				{
					string? databaseDir = Path.GetDirectoryName(databasePath);
					Debug.Assert(databaseDir != null);
					Debug.Assert(Path.IsPathRooted(databaseDir));
					if (path.StartsWith(databaseDir))
					{
						MessageBox.Show("Can't copy files in the database directory.", "Failed to add document!", MessageBoxButton.OK);
						return;
					}

					string relativeProjectFolder = Path.Combine(dataFolder, projectFolderPrefix + project.ProjectId);
					try
					{
						Directory.CreateDirectory(Path.Combine(databaseDir, relativeProjectFolder));
					}
					catch (Exception e)
					{
						MessageBox.Show(e.Message, "Failed to copy document!", MessageBoxButton.OK);
						return;
					}

					string relativeDocumentPath = Path.Combine(relativeProjectFolder, Path.GetFileName(path));
					string destinationPath = Path.Combine(databaseDir, relativeDocumentPath);
					int copyNumber = 1;
					while (File.Exists(destinationPath))
					{
						Debug.Assert(extension != null);
						relativeDocumentPath = Path.Combine(relativeProjectFolder, Path.GetFileNameWithoutExtension(path) + $"_({copyNumber++})" + extension);
						destinationPath = Path.Combine(databaseDir, relativeDocumentPath);
					}

					try
					{
						File.Copy(path, destinationPath, false);
					}
					catch (Exception e)
					{
						MessageBox.Show(e.Message, "Failed to copy document!", MessageBoxButton.OK);
						return;
					}

					if (!AddDocument(new(MossyDocumentPathType.File, relativeDocumentPath), project))
					{
						try
						{
							File.Delete(destinationPath);
						}
						catch { }
					}
				}
				else
				{
					MessageBox.Show("Invalid DragDropEffects operation.", "Failed to add document!", MessageBoxButton.OK);
				}
			}
		}
		else
		{
			MessageBox.Show($"{path} doesn't exist, or is missing permissions.", "Failed to add document!", MessageBoxButton.OK);
		}
	}
	public void AddDocumentString(MossyProject project, string data)
	{
		if (Directory.Exists(data) || File.Exists(data))
		{
			AddDocumentFile(DragDropEffects.Link, project, data);
		}
		else if (MossyUtils.IsValidUrl(data))
		{
			AddDocument(new(MossyDocumentPathType.Url, data), project);
		}
		else
		{
			MessageBox.Show($"Invalid string", "Failed to add document!", MessageBoxButton.OK);
		}
	}

	#region IMossyDatabase Properties
	private bool initialized = false;
	public bool Initialized
	{
		get { return initialized; }
		private set { initialized = value; OnPropertyChanged(); }
	}
	public ObservableCollection<MossyTag> Tags { get; private set; } = new();
	public ObservableCollection<MossyProject> Projects { get; private set; } = new();
	#endregion

	private MossyConfig config = new();
	private string? databasePath;
	private const string databaseFilename = "mossy_database.db";
	private const string dataFolder = "data";
	private const string projectFolderPrefix = "PRJ_";
	private const string altNamesSeparator = ";";
}
