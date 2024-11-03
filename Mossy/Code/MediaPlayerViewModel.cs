using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Mossy;


internal class MediaPlayerViewModel : NotifyPropertyChangedBase
{
	private MediaPlayer mediaPlayer;

	public MediaPlayerViewModel()
	{
		mediaPlayer = new MediaPlayer();
		PauseResumeCommand = new DelegateCommand(PauseResumeCommandHandler);

		mediaPlayer.MediaOpened += OnMediaOpened;
		mediaPlayer.MediaFailed += OnMediaFailed;
		mediaPlayer.MediaEnded += OnMediaEnded;

		DispatcherTimer timer = new()
		{
			Interval = TimeSpan.FromMilliseconds(100)
		};
		timer.Tick += TickUpdate;
		timer.Start();
	}

	public void LoadMedia(Uri uri)
	{
		if (uri == mediaPlayer.Source)
		{
			mediaPlayer.Stop();
			OnMediaOpened(this, EventArgs.Empty);
			return;
		}
		mediaPlayer.Open(uri);
	}

	private void TickUpdate(object? sender, EventArgs e)
	{
		if (mediaPlayer.Source == null)
		{
			Debug.Assert(!IsLoaded);
			Debug.Assert(!IsPlaying);
			Debug.Assert(Position == null);
			Debug.Assert(Duration == null);
			return;
		}

		if (IsPlaying)
		{
			OnPropertyChanged(nameof(Position));
		}
	}

	private void OnMediaOpened(object? sender, EventArgs e)
	{
		mediaPlayer.Play();
		IsPlaying = true;

		OnPropertyChanged(nameof(IsLoaded));
		OnPropertyChanged(nameof(IsPlaying));
		OnPropertyChanged(nameof(Position));
		OnPropertyChanged(nameof(Duration));
	}

	private void OnMediaFailed(object? sender, ExceptionEventArgs e)
	{
		MessageBox.Show(
			$"Failed to open media file: {mediaPlayer.Source}, \n{e.ErrorException.Message}",
			"Error", MessageBoxButton.OK);

		mediaPlayer.Close();
		IsPlaying = false;

		OnPropertyChanged(nameof(IsLoaded));
		OnPropertyChanged(nameof(IsPlaying));
		OnPropertyChanged(nameof(Position));
		OnPropertyChanged(nameof(Duration));
	}

	private void OnMediaEnded(object? sender, EventArgs e)
	{
		IsPlaying = false;

		OnPropertyChanged(nameof(IsLoaded));
		OnPropertyChanged(nameof(IsPlaying));
		OnPropertyChanged(nameof(Position));
		OnPropertyChanged(nameof(Duration));
	}

	private void PauseResumeCommandHandler(object? param)
	{
		if (mediaPlayer.Source == null)
		{
			Debug.Assert(false, "Can't play or pause, media doesn't exist!");
			return;
		}

		if (IsPlaying)
		{
			mediaPlayer.Pause();
			IsPlaying = false;
		}
		else
		{
			if (mediaPlayer.Position == mediaPlayer.NaturalDuration)
			{
				mediaPlayer.Stop();
			}
			mediaPlayer.Play();
			IsPlaying = true;
		}
		OnPropertyChanged(nameof(IsPlaying));
	}


	public ICommand? PauseResumeCommand { get; }

	public bool IsPlaying { get; private set; } = false;
	public bool IsLoaded
	{
		get
		{
			return mediaPlayer.Source != null;
		}
	}
	public TimeSpan? Position
	{
		get
		{
			if (mediaPlayer.Source != null)
			{
				return mediaPlayer.Position;
			}
			return null;
		}
	}
	public TimeSpan? Duration
	{
		get
		{
			if (mediaPlayer.Source != null)
			{
				return mediaPlayer.NaturalDuration.TimeSpan;
			}
			return null;
		}
	}
}
