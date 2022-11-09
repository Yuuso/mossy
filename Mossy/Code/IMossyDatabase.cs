using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;

namespace Mossy;


internal enum MossyDocumentPathType
{
	Unknown, File, Link, Url
}
internal class MossyDocumentPath
{
	public MossyDocumentPath(MossyDocumentPathType type, string absolutePath)
	{
		Type = type;
		Path = absolutePath;
		RawPath = Type.ToString() + separator + Path;
	}
	public MossyDocumentPath(string rawPath)
	{
		RawPath = rawPath;
		var splitPath = rawPath.Split(separator, 2);
		if (splitPath.Length != 2)
		{
			Debug.Assert(false, "MossyDocumentPath: Unable to parse path!");
			Path = rawPath;
			Type = MossyDocumentPathType.Unknown;
			return;
		}
		Path = splitPath[1];
		if (Enum.TryParse(splitPath[0], out MossyDocumentPathType type))
		{
			Type = type;
		}
		else
		{
			Debug.Assert(false, "MossyDocumentPath: Unable to parse type!");
			Type = MossyDocumentPathType.Unknown;
		}
	}

	private const char separator = ':';
	public string RawPath { get; private set; }
	public MossyDocumentPathType Type { get; private set; }
	public string Path { get; private set; }
}

internal struct MossyDocument
{
	public long DocumentId { get; set; }
	public MossyDocumentPath Path { get; set; }
	public DateTime DateCreated { get; set; }
}
internal struct MossyTag
{
	public long TagId { get; set; }
	public string Name { get; set; }
	public string Category { get; set; }
	public Color Color { get; set; }

	public List<MossyDocument> Documents { get; set; }
}
internal struct MossyProject
{
	public long ProjectId { get; set; }
	public string Name { get; set; }
	public List<string> AltNames { get; set; }
	public DateTime DateCreated { get; set; }

	public List<MossyTag> Tags { get; set; }
	public List<MossyDocument> Documents { get; set; }
}

internal interface IMossyDatabase
{
	public void InitNew();
	public void InitOpen();
	public void Deinit();
	public void AddProject(string name);
	public void AddDocumentFile(DragDropEffects operation, MossyProject project, string path);
	public void AddDocumentString(MossyProject project, string data);

	public bool Initialized { get; }
	public ObservableCollection<MossyTag> Tags { get; }
	public ObservableCollection<MossyProject> Projects { get; }
}
