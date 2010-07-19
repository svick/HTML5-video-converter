using System;
using System.IO;
using System.Windows;
using Video_converter.Properties;

namespace Video_converter
{
	public partial class App : Application
	{
		public static string FfmpegLocation
		{
			get;
			private set;
		}

		public static Log Log = new Log();

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
				throw new Exception("Soubor " + Settings.Default.ffmpegLocation + " nebyl nalezen");
			}
		}
	}
}
