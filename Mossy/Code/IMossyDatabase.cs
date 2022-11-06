using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Media;

namespace Mossy;

internal struct MossyDocument
{
	public long DocumentId { get; set; }
	public long ProjectId { get; set; }
	public string DocumentPath { get; set; }
	public DateTime DateCreated { get; set; }
	public bool External { get; set; }
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

	public bool Initialized { get; }
	public ObservableCollection<MossyTag> Tags { get; }
	public ObservableCollection<MossyProject> Projects { get; }
}
