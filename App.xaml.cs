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
			System.Globalization.CultureInfo culture = System.Threading.Thread.CurrentThread.CurrentUICulture;
			if (culture.Name == "sk-SK")
				culture = System.Globalization.CultureInfo.CreateSpecificCulture("cs-CZ");
			WPFLocalizeExtension.Engine.LocalizeDictionary.Instance.Culture = culture;
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
				throw new FileNotFoundException(GetLocalizedString("FileNotFound", Settings.Default.ffmpegLocation), Settings.Default.ffmpegLocation);
			}
		}

		public static string GetLocalizedString(string key, string formatSegment1 = null)
		{
			string result;
			new WPFLocalizeExtension.Extensions.LocTextExtension
			{
				Key = key,
				Assembly = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name,
				FormatSegment1 = formatSegment1
			}.ResolveLocalizedValue(out result);
			return result;
		}
	}
}