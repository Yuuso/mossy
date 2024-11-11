using Microsoft.Data.Sqlite;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
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
			Deinit();
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

		UserSettings.Instance.LastDatabaseFolderPath = "";
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
		if (string.IsNullOrWhiteSpace(projectName))
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
				project_id			INTEGER NOT NULL PRIMARY KEY,
				name				TEXT	NOT NULL,
				alt_names			TEXT,
				cover_document_id	INTEGER,
				flags				INTEGER,
				date_created		TEXT	NOT NULL
			);

			CREATE TABLE documents (
				document_id			INTEGER NOT NULL PRIMARY KEY,
				path				TEXT	NOT NULL,
				flags				INTEGER,
				date_created		TEXT	NOT NULL
			);

			CREATE TABLE tags (
				tag_id				INTEGER NOT NULL PRIMARY KEY,
				name				TEXT	NOT NULL,
				category			TEXT	NOT NULL,
				cover_document_id	INTEGER,
				color				TEXT,
				flags				INTEGER,
				date_created		TEXT	NOT NULL
			);

			CREATE TABLE project_tag (
				project_id			INTEGER NOT NULL,
				tag_id				INTEGER NOT NULL
			);

			CREATE TABLE project_document (
				project_id			INTEGER NOT NULL,
				document_id			INTEGER NOT NULL
			);

			CREATE TABLE tag_document (
				tag_id				INTEGER NOT NULL,
				document_id			INTEGER NOT NULL
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

				var nameOrd = reader.GetOrdinal("name");
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
			SELECT * FROM tags;
		";
		try
		{
			using var reader = command.ExecuteReader();
			while (reader.Read())
			{
				var tag = new MossyTag();

				var idOrd = reader.GetOrdinal("tag_id");
				Debug.Assert(!reader.IsDBNull(idOrd));
				tag.TagId = reader.GetInt64(idOrd);

				var nameOrd = reader.GetOrdinal("name");
				Debug.Assert(!reader.IsDBNull(nameOrd));
				tag.Name = reader.GetString(nameOrd);

				var categoryOrd = reader.GetOrdinal("category");
				Debug.Assert(!reader.IsDBNull(categoryOrd));
				tag.Category = reader.GetString(categoryOrd);

				var dateCreatedOrd = reader.GetOrdinal("date_created");
				Debug.Assert(!reader.IsDBNull(dateCreatedOrd));
				tag.DateCreated = reader.GetDateTimeOffset(dateCreatedOrd).DateTime;

				tag.Documents = new();

				Tags.Add(tag);
			}
		}
		catch (Exception e)
		{
			MessageBox.Show(e.Message, "Failed to load database!", MessageBoxButton.OK);
			return false;
		}

		command.CommandText =
		@"
			SELECT tag_id
			FROM project_tag
			WHERE project_id = $id;
		";
		for (int i = 0; i < Projects.Count; i++)
		{
			var project = Projects[i];
			command.Parameters.Clear();
			command.Parameters.AddWithValue("$id", project.ProjectId.Value);

			try
			{
				using var reader = command.ExecuteReader();
				while (reader.Read())
				{
					var idOrd = reader.GetOrdinal("tag_id");
					Debug.Assert(!reader.IsDBNull(idOrd));
					var tagId = reader.GetInt64(idOrd);

					bool found = false;
					foreach (var tag in Tags)
					{
						if (tag.TagId == tagId)
						{
							Debug.Assert(!found);
							project.Tags.Add(tag);
							tag.Projects.Add(project);
							found = true;
						}
					}
					Debug.Assert(found);
				}
			}
			catch (Exception e)
			{
				MessageBox.Show(e.Message, "Failed to load database!", MessageBoxButton.OK);
				return false;
			}
		}

		command.CommandText =
		@"
			SELECT document_id
			FROM tag_document
			WHERE tag_id = $id;
		";
		for (int i = 0; i < Tags.Count; i++)
		{
			var tag = Tags[i];
			command.Parameters.Clear();
			command.Parameters.AddWithValue("$id", tag.TagId.Value);

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

						tag.Documents.Add(document);
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

		command.CommandText =
		@"
			SELECT document_id
			FROM project_document
			WHERE project_id = $id;
		";
		for (int i = 0; i < Projects.Count; i++)
		{
			var project = Projects[i];
			command.Parameters.Clear();
			command.Parameters.AddWithValue("$id", project.ProjectId.Value);

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

		if (true)
		{
			// TODO: Validate
		}

		OnDatabaseLoaded();
		return true;
	}


	public bool AddProject(string projectName)
	{
		if (!ValidateProjectName(projectName))
		{
			return false;
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
			return false;
		}

		var projectDateCreated = DateTimeOffset.Now;
		long projectId;

		using var command = connection.CreateCommand();
		command.CommandText =
		@"
			INSERT INTO projects (name, date_created)
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
			return false;
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
		return true;
	}

	public bool DeleteProject(MossyProject project)
	{
		if (project.Documents.Count != 0)
		{
			MessageBox.Show(
				$"Project {project.Name} is not empty.",
				"Failed to delete project!", MessageBoxButton.OK);
			return false;
		}
		if (!Projects.Contains(project))
		{
			MessageBox.Show(
				$"Project (id={project.ProjectId}) not found.",
				"Failed to delete project!", MessageBoxButton.OK);
			return false;
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
			return false;
		}

		using var transaction = connection.BeginTransaction();
		using var command = connection.CreateCommand();


		command.CommandText =
		@"
			DELETE FROM projects
			WHERE project_id = $id;
		";
		command.Parameters.AddWithValue("$id", project.ProjectId.Value);
		try
		{
			var result = command.ExecuteNonQuery();
			Debug.Assert(result == 1);
		}
		catch (SqliteException e)
		{
			MessageBox.Show(e.Message, "Failed to delete project!", MessageBoxButton.OK);
			transaction.Rollback();
			return false;
		}


		command.CommandText =
		@"
			DELETE FROM project_tag
			WHERE project_id = $id;
		";
		command.Parameters.AddWithValue("$id", project.ProjectId.Value);
		try
		{
			var result = command.ExecuteNonQuery();
			Debug.Assert(result == 1);
		}
		catch (SqliteException e)
		{
			MessageBox.Show(e.Message, "Failed to delete tag from project!", MessageBoxButton.OK);
			transaction.Rollback();
			return false;
		}


		if (true)
		{
			// Validate that there are no connected documents.
			command.CommandText =
			@"
				SELECT COUNT(*)
				FROM project_document
				WHERE project_id = $id;
			";
			command.Parameters.Clear();
			command.Parameters.AddWithValue("$id", project.ProjectId.Value);
			try
			{
				var result = command.ExecuteScalar();
				Debug.Assert(result != null);
				Debug.Assert((long)result == 0);
			}
			catch (SqliteException e)
			{
				MessageBox.Show(e.Message, "Failed to validate project document database!", MessageBoxButton.OK);
				transaction.Rollback();
				return false;
			}
		}


		var removed = Projects.Remove(project);
		Debug.Assert(removed);

		transaction.Commit();
		return true;
	}

	public bool SetProjectName(MossyProject project, string newName)
	{
		if (newName.Length == 0)
		{
			MessageBox.Show("Name cannot be empty.", "Failed to rename project!", MessageBoxButton.OK);
			return false;
		}
		if (!Projects.Contains(project))
		{
			MessageBox.Show(
				$"Project (id={project.ProjectId}) not found.",
				"Failed to set project name!", MessageBoxButton.OK);
			return false;
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
			return false;
		}

		using var transaction = connection.BeginTransaction();
		using var command = connection.CreateCommand();

		command.CommandText =
		@"
			UPDATE projects
			SET name = $name
			WHERE project_id = $id;
		";
		command.Parameters.AddWithValue("$name", newName);
		command.Parameters.AddWithValue("$id", project.ProjectId.Value);

		try
		{
			var result = command.ExecuteNonQuery();
			Debug.Assert(result == 1);
		}
		catch (SqliteException e)
		{
			MessageBox.Show(e.Message, "Failed to rename project!", MessageBoxButton.OK);
			transaction.Rollback();
			return false;
		}

		project.Name = newName;

		transaction.Commit();
		return true;
	}

	public bool AddProjectAltName(MossyProject project, string altName)
	{
		if (!ValidateProjectName(altName))
		{
			return false;
		}
		if (!Projects.Contains(project))
		{
			MessageBox.Show(
				$"Project (id={project.ProjectId}) not found.",
				"Failed to add project alt name!", MessageBoxButton.OK);
			return false;
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
			return false;
		}

		string altNames = altName;
		foreach (string name in project.AltNames)
		{
			altNames = altNames + altNamesSeparator + name;
		}

		using var transaction = connection.BeginTransaction();

		using var command = connection.CreateCommand();
		command.CommandText =
		@"
			UPDATE projects
			SET alt_names = $names
			WHERE project_id = $id;
		";
		command.Parameters.AddWithValue("$names", altNames);
		command.Parameters.AddWithValue("$id", project.ProjectId.Value);

		try
		{
			var result = command.ExecuteNonQuery();
			Debug.Assert(result == 1);
		}
		catch (SqliteException e)
		{
			MessageBox.Show(e.Message, "Failed to add a name!", MessageBoxButton.OK);
			transaction.Rollback();
			return false;
		}

		transaction.Commit();

		project.AltNames.Add(altName);
		return true;
	}

	public bool DeleteProjectAltName(MossyProject project, string altName)
	{
		if (!project.AltNames.Contains(altName))
		{
			MessageBox.Show(
				$"Name {altName} not found in the project.",
				"Failed to delete project alt name", MessageBoxButton.OK);
			return false;
		}
		if (!Projects.Contains(project))
		{
			MessageBox.Show(
				$"Project (id={project.ProjectId}) not found.",
				"Failed to delete project alt name", MessageBoxButton.OK);
			return false;
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
			return false;
		}

		string altNames = "";
		foreach (string name in project.AltNames)
		{
			if (name == altName)
				continue;

			if (altNames.Length > 0)
			{
				altNames += altNamesSeparator + name;
			}
			else
			{
				altNames = name;
			}
		}

		using var transaction = connection.BeginTransaction();

		using var command = connection.CreateCommand();
		command.CommandText =
		@"
			UPDATE projects
			SET alt_names = $names
			WHERE project_id = $id;
		";
		command.Parameters.AddWithValue("$names", altNames);
		command.Parameters.AddWithValue("$id", project.ProjectId.Value);

		try
		{
			var result = command.ExecuteNonQuery();
			Debug.Assert(result == 1);
		}
		catch (SqliteException e)
		{
			MessageBox.Show(e.Message, "Failed to remove a name!", MessageBoxButton.OK);
			transaction.Rollback();
			return false;
		}

		transaction.Commit();

		var removed = project.AltNames.Remove(altName);
		Debug.Assert(removed);
		return true;
	}

	public bool AddProjectTag(MossyProject project, MossyTag tag)
	{
		if (!Projects.Contains(project))
		{
			MessageBox.Show(
				$"Project (id={project.ProjectId}) not found.",
				"Failed to add tag to project!", MessageBoxButton.OK);
			return false;
		}
		if (!Tags.Contains(tag))
		{
			MessageBox.Show(
				$"Tag (id={tag.TagId}) not found.",
				"Failed to add tag to project!", MessageBoxButton.OK);
			return false;
		}
		if (project.Tags.Contains(tag))
		{
			MessageBox.Show(
				$"Project is already tagged with {tag.Name}.",
				"Failed to add tag to project!", MessageBoxButton.OK);
			return false;
		}
		if (tag.Projects.Contains(project))
		{
			MessageBox.Show(
				$"Project already found in tag (id={tag.TagId})?!",
				"Failed to add tag to project!", MessageBoxButton.OK);
			return false;
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
			return false;
		}

		using var command = connection.CreateCommand();
		command.CommandText =
		@"
			INSERT INTO project_tag (project_id, tag_id)
			VALUES ($pid, $tid);
		";
		command.Parameters.AddWithValue("$pid", project.ProjectId.Value);
		command.Parameters.AddWithValue("$tid", tag.TagId.Value);
		try
		{
			command.ExecuteNonQuery();
		}
		catch (SqliteException e)
		{
			MessageBox.Show(e.Message, "Failed to link document to project!", MessageBoxButton.OK);
			return false;
		}

		project.Tags.Add(tag);
		tag.Projects.Add(project);

		return true;
	}

	public bool DeleteProjectTag(MossyProject project, MossyTag tag)
	{
		if (!Projects.Contains(project))
		{
			MessageBox.Show(
				$"Project (id={project.ProjectId}) not found.",
				"Failed to delete tag from project!", MessageBoxButton.OK);
			return false;
		}
		if (!Tags.Contains(tag))
		{
			MessageBox.Show(
				$"Tag (id={tag.TagId}) not found.",
				"Failed to delete tag from project!", MessageBoxButton.OK);
			return false;
		}
		if (!project.Tags.Contains(tag))
		{
			MessageBox.Show(
				$"Project is not tagged with {tag.Name}.",
				"Failed to delete tag from project!", MessageBoxButton.OK);
			return false;
		}
		if (!tag.Projects.Contains(project))
		{
			MessageBox.Show(
				$"Project not found in tag (id={tag.TagId})?!",
				"Failed to delete tag from project!", MessageBoxButton.OK);
			return false;
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
			return false;
		}

		using var command = connection.CreateCommand();
		command.CommandText =
		@"
			DELETE FROM project_tag
			WHERE project_id = $pid
			AND tag_id = $tid;
		";
		command.Parameters.AddWithValue("$pid", project.ProjectId.Value);
		command.Parameters.AddWithValue("$tid", tag.TagId.Value);

		try
		{
			var result = command.ExecuteNonQuery();
			Debug.Assert(result == 1);
		}
		catch (SqliteException e)
		{
			MessageBox.Show(e.Message, "Failed to remove tag!", MessageBoxButton.OK);
			return false;
		}

		bool removed;
		removed = tag.Projects.Remove(project);
		Debug.Assert(removed);
		removed = project.Tags.Remove(tag);
		Debug.Assert(removed);

		return true;
	}


	public bool AddTag(string name, string category)
	{
		if (name.Length <= 0)
		{
			MessageBox.Show(
				$"Invalid name '{name}'.",
				"Failed to add new tag!", MessageBoxButton.OK);
			return false;
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
			return false;
		}

		var tagDateCreated = DateTimeOffset.Now;
		long tagId;

		using var command = connection.CreateCommand();
		command.CommandText =
		@"
			INSERT INTO tags (name, category, date_created)
			VALUES ($name, $cat, $date);

			SELECT last_insert_rowid();
		";
		command.Parameters.AddWithValue("$name", name);
		command.Parameters.AddWithValue("$cat", category);
		command.Parameters.AddWithValue("$date", tagDateCreated);
		try
		{
			var result = command.ExecuteScalar();
			Debug.Assert(result != null);
			tagId = (long)result;
			Debug.Assert(tagId > 0);
		}
		catch (SqliteException e)
		{
			MessageBox.Show(e.Message, "Failed to add new tag!", MessageBoxButton.OK);
			return false;
		}

		var tag = new MossyTag
		{
			TagId = tagId,
			Name = name,
			Category = category,
			DateCreated = tagDateCreated.DateTime,
			Color = new(),
			Documents = new()
		};
		Tags.Add(tag);
		return true;
	}

	public bool DeleteTag(MossyTag tag)
	{
		if (tag.Documents.Count != 0)
		{
			MessageBox.Show(
				"Tag is not empty!",
				"Failed to delete tag!", MessageBoxButton.OK);
			return false;
		}
		if (!Tags.Contains(tag))
		{
			MessageBox.Show(
				$"Tag (id={tag.TagId}) not found.",
				"Failed to delete tag!", MessageBoxButton.OK);
			return false;
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
			return false;
		}

		using var transaction = connection.BeginTransaction();
		using var command = connection.CreateCommand();


		command.CommandText =
		@"
			DELETE FROM tags
			WHERE tag_id = $id;
		";
		command.Parameters.AddWithValue("$id", tag.TagId.Value);
		try
		{
			var result = command.ExecuteNonQuery();
			Debug.Assert(result == 1);
		}
		catch (SqliteException e)
		{
			MessageBox.Show(e.Message, "Failed to delete tag!", MessageBoxButton.OK);
			transaction.Rollback();
			return false;
		}


		command.CommandText =
		@"
			DELETE FROM project_tag
			WHERE tag_id = $id;
		";
		command.Parameters.Clear();
		command.Parameters.AddWithValue("$id", tag.TagId.Value);
		try
		{
			_ = command.ExecuteNonQuery();
		}
		catch (SqliteException e)
		{
			MessageBox.Show(e.Message, "Failed to delete tag!", MessageBoxButton.OK);
			transaction.Rollback();
			return false;
		}


		if (true)
		{
			// Validate that there are no connected documents.
			command.CommandText =
			@"
				SELECT COUNT(*)
				FROM tag_document
				WHERE tag_id = $id;
			";
			command.Parameters.Clear();
			command.Parameters.AddWithValue("$id", tag.TagId.Value);
			try
			{
				var result = command.ExecuteScalar();
				Debug.Assert(result != null);
				Debug.Assert((long)result == 0);
			}
			catch (SqliteException e)
			{
				MessageBox.Show(e.Message, "Failed to validate tag document database!", MessageBoxButton.OK);
				transaction.Rollback();
				return false;
			}
		}


		transaction.Commit();

		foreach (var project in Projects)
		{
			if (project.Tags.Contains(tag))
			{
				project.Tags.Remove(tag);
			}
		}

		var removed = Tags.Remove(tag);
		Debug.Assert(removed);

		return true;
	}

	public bool SetTagName(MossyTag tag, string newName)
	{
		if (tag.Name == newName)
		{
			MessageBox.Show(
				$"Tag name is already '{newName}'.",
				"Failed to set tag name!", MessageBoxButton.OK);
			return false;
		}
		if (newName.Length <= 0)
		{
			MessageBox.Show(
				$"Invalid name '{newName}'.",
				"Failed to set tag name!", MessageBoxButton.OK);
			return false;
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
			return false;
		}

		using var command = connection.CreateCommand();
		command.CommandText =
		@"
			UPDATE tags
			SET name = $name
			WHERE tag_id = $id;
		";
		command.Parameters.AddWithValue("$name", newName);
		command.Parameters.AddWithValue("$id", tag.TagId.Value);

		try
		{
			var result = command.ExecuteNonQuery();
			Debug.Assert(result == 1);
		}
		catch (SqliteException e)
		{
			MessageBox.Show(e.Message, "Failed to rename tag!", MessageBoxButton.OK);
			return false;
		}

		tag.Name = newName;
		return true;
	}

	public bool SetTagCategory(MossyTag tag, string newCategory)
	{
		if (tag.Category == newCategory)
		{
			MessageBox.Show(
				$"Tag category is already '{newCategory}'.",
				"Failed to set tag category!", MessageBoxButton.OK);
			return false;
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
			return false;
		}

		using var command = connection.CreateCommand();
		command.CommandText =
		@"
			UPDATE tags
			SET category = $cat
			WHERE tag_id = $id;
		";
		command.Parameters.AddWithValue("$cat", newCategory);
		command.Parameters.AddWithValue("$id", tag.TagId.Value);

		try
		{
			var result = command.ExecuteNonQuery();
			Debug.Assert(result == 1);
		}
		catch (SqliteException e)
		{
			MessageBox.Show(e.Message, "Failed to recategorize tag!", MessageBoxButton.OK);
			return false;
		}

		tag.Category = newCategory;
		return true;
	}


	private bool AddDocument(MossyDocumentPath documentPath, MossyProject project)
	{
		foreach (MossyDocument doc in project.Documents)
		{
			if (doc.Path.RawPath == documentPath.RawPath)
			{
				MessageBox.Show(
					"Document already found in project.",
					"Failed to add document!", MessageBoxButton.OK);
				return false;
			}
		}

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
			MessageBox.Show(e.Message, "Failed to add new document!", MessageBoxButton.OK);
			transaction.Rollback();
			return false;
		}

		command.CommandText =
		@"
			INSERT INTO project_document (project_id, document_id)
			VALUES ($projectId, $documentId);
		";
		command.Parameters.Clear();
		command.Parameters.AddWithValue("$projectId", project.ProjectId.Value);
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

	private bool AddDocument(MossyDocumentPath documentPath, MossyTag tag)
	{
		foreach (MossyDocument doc in tag.Documents)
		{
			if (doc.Path.RawPath == documentPath.RawPath)
			{
				MessageBox.Show(
					"Document already found in tag.",
					"Failed to add document!", MessageBoxButton.OK);
				return false;
			}
		}
		if (documentPath.Type == MossyDocumentPathType.Unknown)
		{
			MessageBox.Show(
				"Unknown document type.",
				"Failed to add document!", MessageBoxButton.OK);
			return false;
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
			return false;
		}

		var documentDateCreated = DateTimeOffset.Now;
		long documentId;

		using var transaction = connection.BeginTransaction();
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
			MessageBox.Show(e.Message, "Failed to add new document!", MessageBoxButton.OK);
			transaction.Rollback();
			return false;
		}


		command.CommandText =
		@"
			INSERT INTO tag_document (tag_id, document_id)
			VALUES ($tid, $did);
		";
		command.Parameters.Clear();
		command.Parameters.AddWithValue("$tid", tag.TagId.Value);
		command.Parameters.AddWithValue("$did", documentId);
		try
		{
			command.ExecuteNonQuery();
		}
		catch (SqliteException e)
		{
			MessageBox.Show(e.Message, "Failed to link document to tag!", MessageBoxButton.OK);
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
		tag.Documents.Add(document);
		return true;
	}

	private bool AddDocument(DragDropEffects operation, string path, MossyProject? project, MossyTag? tag)
	{
		if (project == null && tag == null)
		{
			MessageBox.Show(
				"No valid project or tag provided.",
				"Failed to add document!", MessageBoxButton.OK);
			return false;
		}
		if (project != null && tag != null)
		{
			MessageBox.Show(
				"Both a valid project AND a tag provided.",
				"Failed to add document!", MessageBoxButton.OK);
			return false;
		}
		Debug.Assert(!(project != null && tag != null));

		if (Directory.Exists(path))
		{
			if (project != null)
			{
				AddDocument(new(MossyDocumentPathType.Link, path), project);
			}
			else
			{
				Debug.Assert(tag != null);
				AddDocument(new(MossyDocumentPathType.Link, path), tag);
			}
		}
		else if (File.Exists(path))
		{
			var extension = Path.GetExtension(path);
			if (extension == null || extension?.Length == 0)
			{
				MessageBox.Show("Invalid file extension.", "Failed to add document!", MessageBoxButton.OK);
				return false;
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
					return false;
				}

				string[] splitContent = contents.Split("URL=", StringSplitOptions.TrimEntries);
				if (splitContent.Length == 2 && splitContent[0].Equals("[InternetShortcut]") && MossyUtils.IsValidUrl(splitContent[1]))
				{
					if (project != null)
					{
						AddDocument(new(MossyDocumentPathType.Url, splitContent[1]), project);
					}
					else
					{
						Debug.Assert(tag != null);
						AddDocument(new(MossyDocumentPathType.Url, splitContent[1]), tag);
					}
				}
				else
				{
					MessageBox.Show("Invalid .url file contents.", "Failed to add document!", MessageBoxButton.OK);
					return false;
				}
			}
			else if (extension == ".lnk")
			{
				MessageBox.Show("Shortcut (.lnk) file type is not supported.", "Failed to add document!", MessageBoxButton.OK);
				return false;
			}
			else
			{
				if (operation == DragDropEffects.Link)
				{
					if (project != null)
					{
						AddDocument(new(MossyDocumentPathType.Link, path), project);
					}
					else
					{
						Debug.Assert(tag != null);
						AddDocument(new(MossyDocumentPathType.Link, path), tag);
					}
				}
				else if (operation == DragDropEffects.Copy)
				{
					string? databaseDir = Path.GetDirectoryName(databasePath);
					Debug.Assert(databaseDir != null);
					Debug.Assert(Path.IsPathRooted(databaseDir));
					if (path.StartsWith(databaseDir))
					{
						MessageBox.Show("Can't copy files in the database directory.",
							"Failed to add document!", MessageBoxButton.OK);
						return false;
					}

					string relativeTargetDir;
					if (project != null)
					{
						relativeTargetDir = Path.Combine(dataFolder, projectFolderPrefix + project.ProjectId);
					}
					else
					{
						Debug.Assert(tag != null);
						relativeTargetDir = Path.Combine(dataFolder, tagFolderPrefix + tag.TagId);
					}

					try
					{
						Directory.CreateDirectory(Path.Combine(databaseDir, relativeTargetDir));
					}
					catch (Exception e)
					{
						MessageBox.Show(e.Message, "Failed to copy document!", MessageBoxButton.OK);
						return false;
					}

					string relativeDocumentPath = Path.Combine(relativeTargetDir, Path.GetFileName(path));
					string destinationPath = Path.Combine(databaseDir, relativeDocumentPath);
					int copyNumber = 1;
					while (File.Exists(destinationPath))
					{
						Debug.Assert(extension != null);
						relativeDocumentPath = Path.Combine(relativeTargetDir, Path.GetFileNameWithoutExtension(path) + $"_({copyNumber++})" + extension);
						destinationPath = Path.Combine(databaseDir, relativeDocumentPath);
					}

					try
					{
						File.Copy(path, destinationPath, false);
					}
					catch (Exception e)
					{
						MessageBox.Show(e.Message, "Failed to copy document!", MessageBoxButton.OK);
						return false;
					}

					bool success;
					if (project != null)
					{
						success = AddDocument(new(MossyDocumentPathType.File, relativeDocumentPath), project);
					}
					else
					{
						Debug.Assert(tag != null);
						success = AddDocument(new(MossyDocumentPathType.File, relativeDocumentPath), tag);
					}

					if (!success)
					{
						try
						{
							File.Delete(destinationPath);
						}
						catch { }
						return false;
					}
				}
				else
				{
					MessageBox.Show("Invalid DragDropEffects operation.",
						"Failed to add document!", MessageBoxButton.OK);
					return false;
				}
			}
		}
		else
		{
			MessageBox.Show($"{path} doesn't exist, or is missing permissions.",
				"Failed to add document!", MessageBoxButton.OK);
			return false;
		}
		return true;
	}

	public bool AddDocument(DragDropEffects operation, MossyProject project, string path)
	{
		return AddDocument(operation, path, project, null);
	}

	public bool AddDocument(DragDropEffects operation, MossyTag tag, string path)
	{
		return AddDocument(operation, path, null, tag);
	}

	public bool AddDocument(MossyProject project, string data)
	{
		if (Directory.Exists(data) || File.Exists(data))
		{
			return AddDocument(DragDropEffects.Link, project, data);
		}
		else if (MossyUtils.IsValidUrl(data))
		{
			return AddDocument(new(MossyDocumentPathType.Url, data), project);
		}
		else
		{
			MessageBox.Show($"Invalid string", "Failed to add document!", MessageBoxButton.OK);
		}
		return false;
	}

	public bool AddDocument(MossyTag tag, string data)
	{
		if (Directory.Exists(data) || File.Exists(data))
		{
			return AddDocument(DragDropEffects.Link, tag, data);
		}
		else if (MossyUtils.IsValidUrl(data))
		{
			return AddDocument(new(MossyDocumentPathType.Url, data), tag);
		}
		else
		{
			MessageBox.Show($"Invalid string", "Failed to add document!", MessageBoxButton.OK);
		}
		return false;
	}

	public bool DeleteDocument(MossyDocument document, MossyProject project)
	{
		if (!project.Documents.Contains(document))
		{
			MessageBox.Show(
				$"Document (id={document.DocumentId}) not found in project {project.Name}.",
				"Failed to delete document!", MessageBoxButton.OK);
			return false;
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
			return false;
		}

		using var transaction = connection.BeginTransaction();
		using var command = connection.CreateCommand();


		command.CommandText =
		@"
			DELETE FROM documents
			WHERE document_id = $id;
		";
		command.Parameters.AddWithValue("$id", document.DocumentId.Value);
		try
		{
			var result = command.ExecuteNonQuery();
			Debug.Assert(result == 1);
		}
		catch (SqliteException e)
		{
			MessageBox.Show(e.Message, "Failed to delete document!", MessageBoxButton.OK);
			transaction.Rollback();
			return false;
		}


		command.CommandText =
		@"
			DELETE FROM project_document
			WHERE document_id = $id;
		";
		command.Parameters.Clear();
		command.Parameters.AddWithValue("$id", document.DocumentId.Value);
		try
		{
			var result = command.ExecuteNonQuery();
			// Every document is expected to be in only one project or tag!
			Debug.Assert(result == 1);
		}
		catch (SqliteException e)
		{
			MessageBox.Show(e.Message, "Failed to delete document!", MessageBoxButton.OK);
			transaction.Rollback();
			return false;
		}


		if (true)
		{
			// Sanity check, document should only be in a project of a tag.
			command.CommandText =
			@"
				SELECT COUNT(*)
				FROM tag_document
				WHERE document_id = $id;
			";
			command.Parameters.Clear();
			command.Parameters.AddWithValue("$id", document.DocumentId.Value);
			try
			{
				var result = command.ExecuteScalar();
				Debug.Assert(result != null);
				Debug.Assert((long)result == 0);
			}
			catch (SqliteException e)
			{
				MessageBox.Show(e.Message, "Failed to validate tag document database!", MessageBoxButton.OK);
				transaction.Rollback();
				return false;
			}
		}


		if (document.Path.Type == MossyDocumentPathType.File)
		{
			string path = GetAbsolutePath(document);
			Debug.Assert(File.Exists(path));
			try
			{
				FileSystem.DeleteFile(path, UIOption.AllDialogs, RecycleOption.SendToRecycleBin, UICancelOption.ThrowException);
			}
			catch (Exception e)
			{
				MessageBox.Show(e.Message, "Failed to delete document!", MessageBoxButton.OK);
				transaction.Rollback();
				return false;
			}
		}


		transaction.Commit();

		var removed = project.Documents.Remove(document);
		Debug.Assert(removed);

		return true;
	}

	public bool DeleteDocument(MossyDocument document, MossyTag tag)
	{
		if (!tag.Documents.Contains(document))
		{
			MessageBox.Show(
				$"Document (id={document.DocumentId}) not found in tag {tag.Name}.",
				"Failed to delete document!", MessageBoxButton.OK);
			return false;
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
			return false;
		}

		using var transaction = connection.BeginTransaction();
		using var command = connection.CreateCommand();


		command.CommandText =
		@"
			DELETE FROM documents
			WHERE document_id = $id;
		";
		command.Parameters.AddWithValue("$id", document.DocumentId.Value);
		try
		{
			var result = command.ExecuteNonQuery();
			Debug.Assert(result == 1);
		}
		catch (SqliteException e)
		{
			MessageBox.Show(e.Message, "Failed to delete document!", MessageBoxButton.OK);
			transaction.Rollback();
			return false;
		}


		command.CommandText =
		@"
			DELETE FROM tag_document
			WHERE document_id = $id;
		";
		command.Parameters.Clear();
		command.Parameters.AddWithValue("$id", document.DocumentId.Value);
		try
		{
			var result = command.ExecuteNonQuery();
			// Every document is expected to be in only one project or tag!
			Debug.Assert(result == 1);
		}
		catch (SqliteException e)
		{
			MessageBox.Show(e.Message, "Failed to delete document!", MessageBoxButton.OK);
			transaction.Rollback();
			return false;
		}


		if (true)
		{
			// Sanity check, document should only be in a project of a tag.
			command.CommandText =
			@"
				SELECT COUNT(*)
				FROM project_document
				WHERE document_id = $id;
			";
			command.Parameters.Clear();
			command.Parameters.AddWithValue("$id", document.DocumentId.Value);
			try
			{
				var result = command.ExecuteScalar();
				Debug.Assert(result != null);
				Debug.Assert((long)result == 0);
			}
			catch (SqliteException e)
			{
				MessageBox.Show(e.Message, "Failed to validate project document database!", MessageBoxButton.OK);
				transaction.Rollback();
				return false;
			}
		}


		if (document.Path.Type == MossyDocumentPathType.File)
		{
			string path = GetAbsolutePath(document);
			Debug.Assert(File.Exists(path));
			try
			{
				FileSystem.DeleteFile(path, UIOption.AllDialogs, RecycleOption.SendToRecycleBin, UICancelOption.ThrowException);
			}
			catch (Exception e)
			{
				MessageBox.Show(e.Message, "Failed to delete document!", MessageBoxButton.OK);
				transaction.Rollback();
				return false;
			}
		}


		transaction.Commit();

		var removed = tag.Documents.Remove(document);
		Debug.Assert(removed);

		return true;
	}

	public bool SetDocumentName(MossyDocument document, string newName)
	{
		if (document.Path.Type != MossyDocumentPathType.File)
		{
			MessageBox.Show("Cannot rename this type of document.", "Failed to rename document!", MessageBoxButton.OK);
			return false;
		}
		if (newName.Length == 0)
		{
			MessageBox.Show("File name cannot be empty.", "Failed to rename document!", MessageBoxButton.OK);
			return false;
		}
		string oldAbsolutePath = GetAbsolutePath(document);
		if (!File.Exists(oldAbsolutePath))
		{
			MessageBox.Show("File doesn't exist.", "Failed to rename document!", MessageBoxButton.OK);
			return false;
		}

		var oldDri = Path.GetDirectoryName(document.Path.Path);
		Debug.Assert(oldDri != null);
		string newPath = Path.Combine(oldDri, newName);
		string newAbsolutePath = GetAbsolutePath(newPath);
		if (File.Exists(newAbsolutePath))
		{
			MessageBox.Show($"File {newName} already exists!", "Failed to rename document!", MessageBoxButton.OK);
			return false;
		}

		var newMossyPath = new MossyDocumentPath(MossyDocumentPathType.File, newPath);

		Debug.Assert(databasePath != null);
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

		using var command = connection.CreateCommand();
		command.CommandText =
		@"
			UPDATE documents
			SET path = $path
			WHERE document_id = $id;
		";
		command.Parameters.AddWithValue("$path", newMossyPath.RawPath);
		command.Parameters.AddWithValue("$id", document.DocumentId.Value);

		try
		{
			var result = command.ExecuteNonQuery();
			Debug.Assert(result == 1);
		}
		catch (SqliteException e)
		{
			MessageBox.Show(e.Message, "Failed to rename document!", MessageBoxButton.OK);
			transaction.Rollback();
			return false;
		}

		try
		{
			File.Move(oldAbsolutePath, newAbsolutePath);
			Debug.Assert(File.Exists(newAbsolutePath));
			Debug.Assert(!File.Exists(oldAbsolutePath));
		}
		catch (Exception e)
		{
			MessageBox.Show(e.Message, "Failed to rename document!", MessageBoxButton.OK);
			transaction.Rollback();
			return false;
		}

		document.Path = newMossyPath;

		transaction.Commit();
		return true;
	}


	private string GetAbsolutePath(string path)
	{
		string? databaseDir = Path.GetDirectoryName(databasePath);
		Debug.Assert(databaseDir != null);
		return Path.Combine(databaseDir, path);
	}

	public string GetAbsolutePath(MossyDocument document)
	{
		Debug.Assert(document.Path.Type == MossyDocumentPathType.File);
		return GetAbsolutePath(document.Path.Path);
	}


	#region IMossyDatabase Properties
	private bool initialized = false;
	public bool Initialized
	{
		get { return initialized; }
		private set { initialized = value; OnPropertyChanged(); }
	}
	public ObservableCollection<MossyTag> Tags			{ get; } = new();
	public ObservableCollection<MossyProject> Projects	{ get; } = new();
	#endregion

	private MossyConfig config = new();
	private string? databasePath;
	private const string databaseFilename       = "mossy_database.db";
	private const string dataFolder             = "data";
	private const string projectFolderPrefix    = "PRJ_";
	private const string tagFolderPrefix        = "TAG_";
	private const string altNamesSeparator      = ";";
}
