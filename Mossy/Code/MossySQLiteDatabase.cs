using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;

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
			return;
		}

		Initialized = true;
	}
	public void Deinit()
	{
		Debug.Assert(Initialized);
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
				date_created	TEXT
			);

			CREATE TABLE documents (
				document_id		INTEGER NOT NULL PRIMARY KEY,
				uri				TEXT,
				date_created	TEXT
			);

			CREATE TABLE tags (
				tag_id			INTEGER NOT NULL PRIMARY KEY,
				name			TEXT,
				category		TEXT,
				date_created	TEXT
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
				project.ProjectId = reader.GetInt64(idOrd);

				var nameOrd = reader.GetOrdinal("project_name");
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
				if (!reader.IsDBNull(dateCreatedOrd))
				{
					project.DateCreated = reader.GetDateTimeOffset(dateCreatedOrd).DateTime;
				}

				Projects.Add(project);
			}
		}
		catch (Exception e)
		{
			MessageBox.Show(e.Message, "Failed to load database!", MessageBoxButton.OK);
			return false;
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
			DateCreated = projectDateCreated.Date
		};
		Projects.Add(project);
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
	private const string altNamesSeparator = ";";
}
