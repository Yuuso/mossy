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
	}
	public MossyDocumentPath(MossyDocumentPathType type, string path)
	{
		Type = type;
		Path = path;
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
	public DocumentID(long value) { Value = value; }
	public static implicit operator DocumentID(long value) { return new DocumentID(value); }
	public static implicit operator long(DocumentID id) { return id.Value; }
	public override string ToString() { return Value.ToString(); }
}
internal struct TagID
{
	public long Value { get; set; }
	public TagID(long value) { Value = value; }
	public static implicit operator TagID(long value) { return new TagID(value); }
	public static implicit operator long(TagID id) { return id.Value; }
	public override string ToString() { return Value.ToString(); }
}
internal struct ProjectID
{
	public long Value { get; set; }
	public ProjectID(long value) { Value = value; }
	public static implicit operator ProjectID(long value) { return new ProjectID(value); }
	public static implicit operator long(ProjectID id) { return id.Value; }
	public override string ToString() { return Value.ToString(); }
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

	public ObservableCollection<MossyDocument> Documents { get; set; } = new();
}
internal class MossyProject : NotifyPropertyChangedBase
{
	private ProjectID projectId;
	private string name = "";
	private TagFlags flags;
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
	public ObservableCollection<string> AltNames { get; set; } = new();
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

	public ObservableCollection<MossyTag> Tags { get; set; } = new();
	public ObservableCollection<MossyDocument> Documents { get; set; } = new();
}

internal interface IMossyDatabase
{
	public void InitNew();
	public void InitOpen();
	public void Deinit();
	public bool AddProject(string name);
	public bool AddDocumentFile(DragDropEffects operation, MossyProject project, string path);
	public bool AddDocumentString(MossyProject project, string data);
	public bool DeleteDocument(MossyDocument document, MossyProject project);
	public bool RenameDocument(MossyDocument document, string newName);
	public bool DeleteProject(MossyProject project);
	public bool RenameProject(MossyProject project, string newName);
	public string GetAbsolutePath(MossyDocument document);

	public bool Initialized { get; }
	public ObservableCollection<MossyTag> Tags { get; }
	public ObservableCollection<MossyProject> Projects { get; }
}
