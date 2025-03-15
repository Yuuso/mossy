using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Mossy;

public partial class MossyBrowser : UserControl
{
	public MossyBrowser()
	{
		InitializeComponent();
	}


	private void Inspector_PreviewDragEnter(object _, DragEventArgs e)
	{
		var vm = DataContext as ViewModel;
		vm?.PreviewDocumentDragDrops(e);
	}
	private void Inspector_PreviewDragLeave(object _, DragEventArgs e)
	{
		var vm = DataContext as ViewModel;
		vm?.PreviewDocumentDragDrops(e);
	}
	private void Inspector_PreviewDragOver(object _, DragEventArgs e)
	{
		var vm = DataContext as ViewModel;
		vm?.PreviewDocumentDragDrops(e);
	}
	private void Inspector_PreviewDrop(object _, DragEventArgs e)
	{
		var vm = DataContext as ViewModel;
		vm?.PreviewDocumentDragDrops(e);
	}
	private void Inspector_DragOver(object _, DragEventArgs e)
	{
		var vm = DataContext as ViewModel;
		vm?.DocumentDragOver(e);
	}
	private void Inspector_Drop(object _, DragEventArgs e)
	{
		var vm = DataContext as ViewModel;
		vm?.DocumentDrop(e);
	}
	private void Document_MouseMove(object sender, MouseEventArgs e)
	{
		if (e.LeftButton == MouseButtonState.Pressed &&
			sender is FrameworkElement border &&
			border.DataContext is MossyDocument doc)
		{
			var vm = DataContext as ViewModel;
			vm?.DocumentDoDragDrop(doc, this);
		}
	}

	private void Document_MouseDown(object sender, MouseButtonEventArgs e)
	{
		if (e.ClickCount == 2 &&
			e.LeftButton == MouseButtonState.Pressed &&
			sender is FrameworkElement border &&
			border.DataContext is MossyDocument doc)
		{
			var vm = DataContext as ViewModel;
			vm?.DocumentDoubleClick(doc);
			e.Handled = true;
		}
	}
}
