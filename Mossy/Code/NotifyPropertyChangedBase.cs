﻿using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Mossy;

internal abstract class NotifyPropertyChangedBase : INotifyPropertyChanged
{
	public event PropertyChangedEventHandler? PropertyChanged;
	protected void OnPropertyChanged([CallerMemberName] string? name = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
	}
}
