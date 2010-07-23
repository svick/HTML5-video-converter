using System;
using System.IO;
using System.Reflection;
using System.Windows;
using Video_converter.Properties;

namespace Video_converter
{
	public partial class App : Application
	{
		public static string FfmpegLocation	{	get; private set;	}
		public static string StartupFile { get; private set; }

		public static Log Log = new Log();
		public static LogWindow LogWindow;

		public App()
		{
			System.Globalization.CultureInfo culture = System.Threading.Thread.CurrentThread.CurrentUICulture;
			if (culture.Name == "sk-SK")
				culture = System.Globalization.CultureInfo.CreateSpecificCulture("cs-CZ");
			WPFLocalizeExtension.Engine.LocalizeDictionary.Instance.Culture = culture;
			Log.Add("Nastaven jazyk: " + culture);

			Directory.SetCurrentDirectory(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

			try
			{
				locateFFmpegFile();
			}
			catch (ConverterException e)
			{
				ErrorMessageBox(e.Message);
				Environment.Exit(0);
			}

			InitializeComponent();
		}

		private void locateFFmpegFile()
		{
			if (Settings.Default.use64bitFfmpegIfIsSupported && Environment.Is64BitOperatingSystem && File.Exists(Settings.Default.ffmpegLocation64))
			{
				// use 64 bit version of ffmpeg
				FfmpegLocation = Settings.Default.ffmpegLocation64;
				Log.Add("Použita 64 bitová verze ffmpeg");
			}
			else if (File.Exists(Settings.Default.ffmpegLocation))
			{
				FfmpegLocation = Settings.Default.ffmpegLocation;
				Log.Add("Použita 32 bitová verze ffmpeg");
			}
			else
			{
				throw new ConverterException(GetLocalizedString("FileNotFound", Settings.Default.ffmpegLocation));
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

		public static void ErrorMessageBox(string message)
		{ 
			MessageBox.Show(message, "", MessageBoxButton.OK, MessageBoxImage.Error);
		}

		private void Application_Startup(object sender, StartupEventArgs e)
		{
			if (e.Args.Length > 0)
				StartupFile = e.Args[0];
		}
	}
}