using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Windows;
using System.Windows.Media;

namespace Mossy;


internal enum MossyDocumentPathType
{
	Unknown, File, Link, Url
}

internal class MossyDocumentPath
{
	public MossyDocumentPath()
	{
		Type = MossyDocumentPathType.Unknown;
		Path = "";
		RawPath = "";
		DisplayPath = "";
		UpdateDisplayPath();
	}

	public MossyDocumentPath(MossyDocumentPathType type, string path)
	{
		Type = type;
		Path = path;
		RawPath = Type.ToString() + separator + Path;
		UpdateDisplayPath();
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
			UpdateDisplayPath();
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
		UpdateDisplayPath();
	}

	private void UpdateDisplayPath()
	{
		switch (Type)
		{
			default:
			case MossyDocumentPathType.Unknown:
				DisplayPath = Path;
				break;

			case MossyDocumentPathType.File:
			{
				string? fileName = System.IO.Path.GetFileName(Path);
				Debug.Assert(fileName != null);
				DisplayPath = fileName ?? Path;
				break;
			}

			case MossyDocumentPathType.Link:
			{
				string? fileName = System.IO.Path.GetFileName(Path);
				string? pathDir = System.IO.Path.GetDirectoryName(Path);
				Debug.Assert(fileName != null);
				Debug.Assert(pathDir != null);
				if (fileName != null && pathDir != null)
				{
					string dirName = new System.IO.DirectoryInfo(pathDir).Name;
					Debug.Assert(!string.IsNullOrEmpty(dirName));
					DisplayPath = System.IO.Path.Join(dirName, fileName);
				}
				else
				{
					DisplayPath = fileName ?? Path;
				}
				break;
			}

			case MossyDocumentPathType.Url:
			{
				DisplayPath = Path;
				if (DisplayPath.StartsWith("http://"))
				{
					DisplayPath = DisplayPath.Substring("http://".Length);
				}
				if (DisplayPath.StartsWith("https://"))
				{
					DisplayPath = DisplayPath.Substring("https://".Length);
				}
				DisplayPath = DisplayPath.Split('/', 2)[0];
				break;
			}
		}
	}

	private const char separator = ':';
	public string					RawPath		{ get; private set; }
	public MossyDocumentPathType	Type		{ get; private set; }
	public string					Path		{ get; private set; }
	public string					DisplayPath	{ get; private set; } = "";
}

[Flags] internal enum DocumentFlags : UInt64
{
	None = 0,
}

[Flags] internal enum TagFlags : UInt64
{
	None = 0,
}

[Flags] internal enum ProjectFlags : UInt64
{
	None = 0,
}

internal struct DocumentID
{
	public long Value { get; set; }
	public DocumentID(long value)							{ Value = value; }
	public static implicit operator DocumentID(long value)	{ return new DocumentID(value); }
	public static implicit operator long(DocumentID id)		{ return id.Value; }
	public override string ToString()						{ return Value.ToString(); }
}

internal struct TagID
{
	public long Value { get; set; }
	public TagID(long value)								{ Value = value; }
	public static implicit operator TagID(long value)		{ return new TagID(value); }
	public static implicit operator long(TagID id)			{ return id.Value; }
	public override string ToString()						{ return Value.ToString(); }
}

internal struct ProjectID
{
	public long Value { get; set; }
	public ProjectID(long value)							{ Value = value; }
	public static implicit operator ProjectID(long value)	{ return new ProjectID(value); }
	public static implicit operator long(ProjectID id)		{ return id.Value; }
	public override string ToString()						{ return Value.ToString(); }
}

internal class MossyDocument : NotifyPropertyChangedBase
{
	private DocumentID documentId;
	private MossyDocumentPath path = new();
	private DocumentFlags flags;
	private DateTime dateCreated;

	public DocumentID DocumentId
	{
		get => documentId;
		set { documentId = value; OnPropertyChanged(); }
	}

	public MossyDocumentPath Path
	{
		get => path;
		set { path = value; OnPropertyChanged(); }
	}

	public DocumentFlags Flags
	{
		get => flags;
		set { flags = value; OnPropertyChanged(); }
	}

	public DateTime DateCreated
	{
		get => dateCreated;
		set { dateCreated = value; OnPropertyChanged(); }
	}
}

internal class MossyTag : NotifyPropertyChangedBase
{
	private TagID tagId;
	private string name = "";
	private string category = "";
	private MossyDocument? coverDocument;
	private Color color;
	private TagFlags flags;
	private DateTime dateCreated;

	public TagID TagId
	{
		get => tagId;
		set { tagId = value; OnPropertyChanged(); }
	}

	public string Name
	{
		get => name;
		set { name = value; OnPropertyChanged(); }
	}

	public string Category
	{
		get => category;
		set { category = value; OnPropertyChanged(); }
	}

	public MossyDocument? CoverDocument
	{
		get => coverDocument;
		set { coverDocument = value; OnPropertyChanged(); }
	}

	public Color Color
	{
		get => color;
		set { color = value; OnPropertyChanged(); }
	}

	public TagFlags Flags
	{
		get => flags;
		set { flags = value; OnPropertyChanged(); }
	}

	public DateTime DateCreated
	{
		get => dateCreated;
		set { dateCreated = value; OnPropertyChanged(); }
	}

	public ObservableCollection<MossyProject> Projects { get; set; } = new();
	public ObservableCollection<MossyDocument> Documents { get; set; } = new();
}

internal class MossyProject : NotifyPropertyChangedBase
{
	private ProjectID projectId;
	private string name = "";
	private MossyDocument? coverDocument;
	private ProjectFlags flags;
	private DateTime dateCreated;

	public ProjectID ProjectId
	{
		get => projectId;
		set { projectId = value; OnPropertyChanged(); }
	}

	public string Name
	{
		get => name;
		set { name = value; OnPropertyChanged(); }
	}

	public MossyDocument? CoverDocument
	{
		get => coverDocument;
		set { coverDocument = value; OnPropertyChanged(); }
	}

	public ObservableCollection<string> AltNames { get; set; } = new();

	public ProjectFlags Flags
	{
		get => flags;
		set { flags = value; OnPropertyChanged(); }
	}

	public DateTime DateCreated
	{
		get => dateCreated;
		set { dateCreated = value; OnPropertyChanged(); }
	}

	public ObservableCollection<MossyTag> Tags { get; set; } = new();
	public ObservableCollection<MossyDocument> Documents { get; set; } = new();
}

internal interface IMossyDatabase
{
	public void InitNew();
	public void InitOpen();
	public void Deinit();

	public bool AddProject(string name);
	public bool DeleteProject(MossyProject project);
	public bool SetProjectName(MossyProject project, string newName);
	public bool AddProjectAltName(MossyProject project, string altName);
	public bool DeleteProjectAltName(MossyProject project, string altName);
	public bool AddProjectTag(MossyProject project, MossyTag tag);
	public bool DeleteProjectTag(MossyProject project, MossyTag tag);
	public bool SetProjectCoverDocument(MossyProject project, MossyDocument document);
	public bool SetProjectDateCreated(MossyProject project, DateTime date);

	public bool AddTag(string name, string category);
	public bool DeleteTag(MossyTag tag);
	public bool SetTagName(MossyTag tag, string newName);
	public bool SetTagCategory(MossyTag tag, string newCategory);
	public bool SetTagCoverDocument(MossyTag tag, MossyDocument document);
	public bool SetTagDateCreated(MossyTag tag, DateTime date);

	public bool AddDocument(DragDropEffects operation, MossyProject project, string path);
	public bool AddDocument(DragDropEffects operation, MossyTag tag, string path);
	public bool AddDocument(MossyProject project, string data);
	public bool AddDocument(MossyTag tag, string data);
	public bool DeleteDocument(MossyDocument document, MossyProject project);
	public bool DeleteDocument(MossyDocument document, MossyTag tag);
	public bool SetDocumentName(MossyDocument document, string newName);

	public string GetAbsolutePath(MossyDocument document);

	public bool Initialized { get; }
	public ObservableCollection<MossyTag> Tags { get; }
	public ObservableCollection<MossyProject> Projects { get; }
}
