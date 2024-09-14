using Ookii.Dialogs.Wpf;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace Mossy;


internal struct MossyConfigData
{
	public int MossyVersion { get; set; }
	public string ConfigId { get; set; }
	public DateTimeOffset DateCreated { get; set; }
}

internal class MossyConfig
{
	public const int CurrentMossyVersion = 1;

	public string? InitNew()
	{
		Debug.Assert(!initialized);

		VistaFolderBrowserDialog dialog = new()
		{
			ShowNewFolderButton = true,
		};

		bool? success = dialog.ShowDialog();
		if (!success.HasValue || !success.Value)
		{
			Trace.WriteLine("Failed to select folder.");
			return null;
		}

		if (!Directory.Exists(dialog.SelectedPath))
		{
			MessageBox.Show("Directory doesn't exist!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			return null;
		}

		string path = Path.Join(dialog.SelectedPath, configFilename);
		if (File.Exists(path))
		{
			MessageBox.Show("Config already exists!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			return null;
		}

		data = new MossyConfigData
		{
			MossyVersion = CurrentMossyVersion,
			ConfigId = Guid.NewGuid().ToString(),
			DateCreated = DateTimeOffset.Now,
		};
		configPath = path;
		if (!SaveData())
		{
			configPath = null;
			return null;
		}

		initialized = true;
		Trace.WriteLine(string.Format($"MossyConfig InitNew, ConfigId {data.ConfigId}"));

		return dialog.SelectedPath;
	}

	public string? InitOpen(string? path)
	{
		Debug.Assert(!initialized);

		if (path != null)
		{
			path = Path.Join(path, configFilename);
			if (!File.Exists(path))
			{
				Trace.WriteLine("MossyConfig InitOpen: Config doesn't exist.");
				return null;
			}
		}

		if (path == null)
		{
			VistaOpenFileDialog dialog = new()
			{
				CheckFileExists = true,
				Multiselect = false,
				ShowReadOnly = false,
				Filter = "Mossy Config File|" + configFilename
			};

			bool? success = dialog.ShowDialog();
			if (!success.HasValue || !success.Value)
			{
				Trace.WriteLine("MossyConfig InitOpen: Failed to select file.");
				return null;
			}

			path = dialog.FileName;
		}

		if (!path.EndsWith(configFilename))
		{
			MessageBox.Show("Selected file is not a Mossy Config file!", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
			return null;
		}

		configPath = path;
		if (!LoadData())
		{
			configPath = null;
			return null;
		}

		initialized = true;
		Trace.WriteLine(string.Format($"MossyConfig InitOpen, ConfigId {data.ConfigId}"));

		return Path.GetDirectoryName(configPath);
	}

	public void Deinit()
	{
		Debug.Assert(initialized);
		configPath = null;
		initialized = false;
	}

	private bool SaveData()
	{
		Debug.Assert(configPath != null);
		string jsonString = JsonSerializer.Serialize(data);

		try
		{
			File.WriteAllText(configPath, jsonString);
		}
		catch (PathTooLongException e)
		{
			MessageBox.Show(e.Message, "Path Too Long!", MessageBoxButton.OK, MessageBoxImage.Error);
			return false;
		}
		catch (IOException e)
		{
			MessageBox.Show(e.Message, "IO Exception!", MessageBoxButton.OK, MessageBoxImage.Error);
			return false;
		}
		catch (UnauthorizedAccessException e)
		{
			MessageBox.Show(e.Message, "Unauthorized Access!", MessageBoxButton.OK, MessageBoxImage.Error);
			return false;
		}
		catch (Exception e)
		{
			MessageBox.Show(e.Message, "Unknown Error!", MessageBoxButton.OK, MessageBoxImage.Error);
			return false;
		}

		return true;
	}

	private bool LoadData()
	{
		Debug.Assert(configPath != null);
		string jsonString;

		try
		{
			jsonString = File.ReadAllText(configPath);
		}
		catch (Exception e)
		{
			MessageBox.Show(e.Message, "Unknown Error!", MessageBoxButton.OK, MessageBoxImage.Error);
			return false;
		}

		try
		{
			data = JsonSerializer.Deserialize<MossyConfigData>(jsonString);
		}
		catch (JsonException e)
		{
			MessageBox.Show(e.Message, "Failed to read config!", MessageBoxButton.OK, MessageBoxImage.Error);
			return false;
		}
		catch (Exception e)
		{
			MessageBox.Show(e.Message, "Unknown Error!", MessageBoxButton.OK, MessageBoxImage.Error);
			return false;
		}

		Debug.Assert(data.MossyVersion == CurrentMossyVersion);

		return true;
	}

	private MossyConfigData data;
	private bool initialized = false;
	private string? configPath;
	private const string configFilename = "mossy_config.json";
}
