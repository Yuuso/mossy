using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace Mossy;

internal struct UserSettingsData
{
	public UserSettingsData() { }
	public bool AutoOpenLastDatabase { get; set; } = true;
	public string LastDatabaseFolderPath { get; set; } = "";
}

internal sealed class UserSettings
{
	private static readonly Lazy<UserSettings> lazy = new(() => new UserSettings());
	public static UserSettings Instance { get { return lazy.Value; } }

	private UserSettings()
	{
		InitData();
	}

	private static string GetUserSettingsPath()
	{
		var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
		return Path.Combine(appDataPath, "Mossy", "user_settings.json");
	}
	private void InitData()
	{
		var userSettingsPath = GetUserSettingsPath();
		if (File.Exists(userSettingsPath))
		{
			LoadData();
		}
		else
		{
			var userSettingsDir = Path.GetDirectoryName(userSettingsPath);
			Debug.Assert(userSettingsDir != null);
			if (!Directory.Exists(userSettingsDir))
			{
				try
				{
					Directory.CreateDirectory(userSettingsDir);
				}
				catch (Exception e)
				{
					MessageBox.Show(e.Message, "Failed to initialize user settings directory!", MessageBoxButton.OK);
					return;
				}
			}

			try
			{
				using var stream = File.Create(GetUserSettingsPath());
			}
			catch (Exception e)
			{
				MessageBox.Show(e.Message, "Failed to initialize user settings!", MessageBoxButton.OK);
				return;
			}
			SaveData();
		}
	}
	private void ClearData()
	{
		try
		{
			File.Delete(GetUserSettingsPath());
		}
		catch (Exception e)
		{
			MessageBox.Show(e.Message, "Failed to clear user settings!", MessageBoxButton.OK);
			return;
		}
		InitData();
	}
	private void SaveData()
	{
		string jsonString = JsonSerializer.Serialize(data);
		try
		{
			File.WriteAllText(GetUserSettingsPath(), jsonString);
		}
		catch (Exception e)
		{
			MessageBox.Show(e.Message, "Failed to save user settings!", MessageBoxButton.OK);
		}
	}
	private void LoadData()
	{
		string jsonString;
		try
		{
			jsonString = File.ReadAllText(GetUserSettingsPath());
		}
		catch (Exception e)
		{
			MessageBox.Show(e.Message, "Failed to load user settings!", MessageBoxButton.OK);
			return;
		}

		try
		{
			data = JsonSerializer.Deserialize<UserSettingsData>(jsonString);
		}
		catch (Exception e)
		{
			MessageBox.Show(e.Message, "Failed to load user settings!", MessageBoxButton.OK);
			ClearData();
		}
	}

	private UserSettingsData data = new();
	public bool AutoOpenLastDatabase
	{
		get { return data.AutoOpenLastDatabase; }
		set { if (data.AutoOpenLastDatabase == value) return; data.AutoOpenLastDatabase = value; SaveData(); }
	}
	public string LastDatabaseFolderPath
	{
		get { return data.LastDatabaseFolderPath; }
		set { if (data.LastDatabaseFolderPath == value) return; data.LastDatabaseFolderPath = value; SaveData(); }
	}
}
