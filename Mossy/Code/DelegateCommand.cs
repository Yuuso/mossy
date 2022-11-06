using System;
using System.Windows.Input;

namespace Mossy;

public class DelegateCommand : ICommand
{
	public DelegateCommand(Action<object?> execute)
	{
		this.execute = execute ?? throw new ArgumentNullException(nameof(execute));
		canExecute = (object? _) => true;
	}
	public DelegateCommand(Func<object?, bool> canExecute, Action<object?> execute)
	{
		this.canExecute = canExecute ?? throw new ArgumentNullException(nameof(canExecute));
		this.execute = execute ?? throw new ArgumentNullException(nameof(execute));
	}

	public bool CanExecute(object? parameter)
	{
		return canExecute(parameter);
	}
	public void Execute(object? parameter)
	{
		execute(parameter);
	}

	public event EventHandler? CanExecuteChanged;
	public void RaiseCanExecuteChanged()
	{
		CanExecuteChanged?.Invoke(this, EventArgs.Empty);
	}

	private readonly Func<object?, bool> canExecute;
	private readonly Action<object?> execute;
}
