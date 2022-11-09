using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace Mossy;

public partial class MossyBrowser : UserControl
{
	public MossyBrowser()
	{
		InitializeComponent();
	}

	private void Project_DragOver(object _, DragEventArgs e)
	{
		var vm = DataContext as ViewModel;
		vm?.DocumentDragOver(e);
	}
	private void Project_Drop(object _, DragEventArgs e)
	{
		var vm = DataContext as ViewModel;
		vm?.DocumentDrop(e);
	}
}
