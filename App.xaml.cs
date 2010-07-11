using System;
using System.IO;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Windows;
using Video_converter.Properties;

namespace Video_converter
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		public string FfmpegLocation
		{
			get;
			private set;
		}

		public App()
		{
			locateFFmpegFile();
		}

		private void locateFFmpegFile()
		{
			if (Settings.Default.use64bitFfmpegIfIsSupported && Environment.Is64BitOperatingSystem && File.Exists(Settings.Default.ffmpegLocation64))
			{
				// use 64 bit version of ffmpeg
				FfmpegLocation = Settings.Default.ffmpegLocation64;
			}
			else if (File.Exists(Settings.Default.ffmpegLocation))
			{
				FfmpegLocation = Settings.Default.ffmpegLocation;
			}
			else
			{
				throw new Exception("Soubor " + FfmpegLocation + " nebyl nalezen");
			}
		}

		private void Application_Exit(object sender, ExitEventArgs e)
		{
			return;
		}

	}
}
