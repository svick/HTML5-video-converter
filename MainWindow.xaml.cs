using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shell;
using Microsoft.Win32;
using Video_converter.Properties;

namespace Video_converter
{
	public partial class MainWindow : Window
	{
		public Converter Converter 
		{
			get;
      protected set;
		} 

		string fileName;
		
		ProgressBar progressBar;
		ConvertDone convertDone;

		object originalContent;
		DateTime startTime;

		CheckBox[] resolutions;
		CheckBox[] formats;

		double totalProgress;
		System.Windows.Threading.DispatcherTimer timer;

		public MainWindow()
		{
			InitializeComponent();
			resolutions = new CheckBox[] { height480, height720, height1080 };
			formats = new CheckBox[] { webm, h264, theora };

			if (App.StartupFile != null)
			{
				selectedFile(App.StartupFile);
			}
		}

		private void selectedFile(string fileName)
		{
			this.fileName = fileName;
			fileNameTextBox.Text = Path.GetFileName(fileName);
			getVideoInfo();
		}

		private void getVideoInfo()
		{
			Video video = new Video(fileName);
			Converter = new Converter(video);
			Converter.ProgressChanged += new ProgressChangedEventHandler(converter_ProgressChanged);
			Converter.AllFinished += new AllFinishedEventHander(converter_AllFinished);

			try
			{
				Converter.VideoInfo();

				height1080.IsEnabled = (video.Size.Height >= 1080 || video.Size.Width >= 1920);
				height720.IsEnabled = (video.Size.Height >= 720 || video.Size.Width >= 1280);
				height480.IsEnabled = (video.Size.Height >= 480 || video.Size.Width >= 854);

				if (height1080.IsEnabled)
					height1080.ToolTip = video.NewSize(1080).ToString("×");

				if (height720.IsEnabled)
					height720.ToolTip = video.NewSize(720).ToString("×");

				if (height480.IsEnabled)
					height480.ToolTip = video.NewSize(480).ToString("×");
			}
			catch (ConverterException e)
			{
				App.ErrorMessageBox(e.Message);
			}
		}

		private void File_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog ofd = new OpenFileDialog();

			ofd.Filter =  App.GetLocalizedString("VideoFormats") + "|" + Settings.Default.supportedFileExtension  + "|" + App.GetLocalizedString("AllFiles") + "|*.*";

			ofd.CheckPathExists = true;
			ofd.CheckFileExists = true;
			ofd.Multiselect = false;

			if (Directory.Exists(Path.GetDirectoryName(fileName)))
			{
				ofd.InitialDirectory = Path.GetDirectoryName(fileName);

				if (File.Exists(fileName))
					ofd.FileName = fileName;
			}
			else
				ofd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);

			if (ofd.ShowDialog() == true)
			{
				selectedFile(fileName);
			}
		}

		private void Convert_Click(object sender, RoutedEventArgs eventArgs)
		{
			if (Converter == null)
			{
				App.ErrorMessageBox(App.GetLocalizedString("NoVideoSelected"));
				return;
			}

			if (!Converter.ConvertSupported)
			{
				App.ErrorMessageBox(App.GetLocalizedString("CantConvert"));
				return;
			}

			AllowDrop = false;

			progressBar = new ProgressBar();
			progressBar.bar.Value = 0;
			progressBar.Cancelled += new System.EventHandler(progressBar_Cancelled);
			originalContent = Content;

			Content = progressBar;

			TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;

			timer = new System.Windows.Threading.DispatcherTimer();
			timer.Tick += new EventHandler(timer_Tick);
			timer.Interval = TimeSpan.FromSeconds(1);
			timer.Start();

			startTime = DateTime.Now;

			try
			{
				foreach (CheckBox formatCheckBox in formats)
					if (formatCheckBox.IsChecked == true)
					{
						bool nothingChecked = true;
						string format = formatCheckBox.Name;
						foreach (CheckBox resolutionCheckBox in resolutions)
							if (resolutionCheckBox.IsChecked == true && resolutionCheckBox.IsEnabled)
							{
								nothingChecked = false;
								int height = int.Parse((string)resolutionCheckBox.Tag);
								Converter.Convert(format, height, Settings.Default.numberOfPass);
							}
						if (nothingChecked)
							Converter.Convert(format, 0, Settings.Default.numberOfPass);
					}
			}
			catch (ConverterException e)
			{
				allFinished(true);
				App.ErrorMessageBox(e.Message);
			}
		}

		void timer_Tick(object sender, EventArgs e)
		{
			TaskbarItemInfo.ProgressValue = totalProgress;
			progressBar.bar.Value = totalProgress * 100;

			if (totalProgress != 0)
			{
				TimeSpan remaining = TimeSpan.FromMilliseconds((DateTime.Now - startTime).TotalMilliseconds * (1 - totalProgress) / totalProgress);
				string remaingString = App.GetLocalizedString("Remaining", remaining.ToLongString());
				progressBar.textInfo.Text = remaingString;
			}
		}

		void converter_ProgressChanged(object sender, EventArg<double> e)
		{
			totalProgress = e.Data;
		}

		private void allFinished(bool showMain = false, bool allOk = true)
		{
			timer.Stop();
			TaskbarItemInfo.ProgressState = TaskbarItemProgressState.None;

			if (showMain)
			{
				Content = originalContent;
				AllowDrop = true;
			}
			else
			{
				convertDone = new ConvertDone(allOk);
				convertDone.BackButton += new EventHandler(convertDone_BackButton);
				convertDone.ShowOutputFolder += new EventHandler(convertDone_ShowOutputFolder);
				Content = convertDone;
			}
		}

		void converter_AllFinished(object sender, EventArg<bool> e)
		{
			Dispatcher.Invoke((Action)(() =>
			{
				allFinished(false, e.Data);
			}));
		}

		void convertDone_ShowOutputFolder(object sender, EventArgs e)
		{
			Process.Start(Converter.OutputFolder);
		}

		void convertDone_BackButton(object sender, EventArgs e)
		{
			allFinished(true);
		}

		void progressBar_Cancelled(object sender, EventArgs e)
		{
			allFinished(true);

			if(Converter != null)
				Converter.StopAll();
		}

		private void Window_Closed(object sender, EventArgs e)
		{
			if (Converter != null)
				Converter.StopAll();
		}

		private void Window_Drop(object sender, DragEventArgs e)
		{
			string[] a = (string[]) e.Data.GetData(System.Windows.DataFormats.FileDrop, false);
			selectedFile(a[0]);
		}

		private void ShowLog(object sender, RoutedEventArgs e)
		{
			App.Log.ShowWindow();
		}

		private void ShowAboutWindow(object sender, RoutedEventArgs e)
		{
			About about = new About();
			about.Show();
		}
	}
}