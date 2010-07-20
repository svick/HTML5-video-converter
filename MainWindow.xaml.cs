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
		}

		private void getVideoInfo(string fileName)
		{
			Video video = new Video(fileName);
			Converter = new Converter(video);
			Converter.ProgressChanged += new ProgressChangedEventHandler(converter_ProgressChanged);
			Converter.AllFinished += new AllFinishedEventHander(converter_AllFinished);

			try
			{
				Converter.VideoInfo();
				ConvertButton.IsEnabled = true;

				height1080.IsEnabled = (video.Size.Height >= 1080 || video.Size.Width >= 1920);
				height720.IsEnabled = (video.Size.Height >= 720 || video.Size.Width >= 1280);
				height480.IsEnabled = (video.Size.Height >= 480 || video.Size.Width >= 854);
			}
			catch (Exception e)
			{
				ConvertButton.IsEnabled = false;
				MessageBox.Show(e.Message, "", MessageBoxButton.OK, MessageBoxImage.Error);
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
				fileName = ofd.FileName;
				fileNameTextBox.Text = Path.GetFileName(fileName);
				
				getVideoInfo(fileName);
			}
		}

		private void Convert_Click(object sender, RoutedEventArgs e)
		{
			if (Converter == null)
			{
				MessageBox.Show(App.GetLocalizedString("NoVideoSelected"), "", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			Win.AllowDrop = false;

			progressBar = new ProgressBar();

			progressBar.Cancelled += new System.EventHandler(progressBar_Cancelled);
			originalContent = Content;

			Content = progressBar;

			taskBarItemInfo.ProgressState = TaskbarItemProgressState.Normal;

			timer = new System.Windows.Threading.DispatcherTimer();
			timer.Tick += new EventHandler(timer_Tick);
			timer.Interval = TimeSpan.FromSeconds(1);
			timer.Start();

			startTime = DateTime.Now;

			foreach (CheckBox formatCheckBox in formats)
				if (formatCheckBox.IsChecked == true)
				{
					bool nothingChecked = true;
					string format = formatCheckBox.Name;
					foreach (CheckBox resolutionCheckBox in resolutions)
						if (resolutionCheckBox.IsChecked == true && resolutionCheckBox.IsEnabled == true)
						{
							nothingChecked = false;
							int height = int.Parse((string)resolutionCheckBox.Tag);
							Converter.Convert(format, height);
						}
					if (nothingChecked)
						Converter.Convert(format);
				}
		}

		void timer_Tick(object sender, EventArgs e)
		{
			taskBarItemInfo.ProgressValue = totalProgress;
			progressBar.bar.Value = totalProgress * 100;

			if (totalProgress != 0)
			{
				TimeSpan remaining = TimeSpan.FromMilliseconds((DateTime.Now - startTime).TotalMilliseconds * (1 - totalProgress) / totalProgress);

				string remaingString = string.Format(App.GetLocalizedString("Remaining"), remaining.ToLongString());

				progressBar.textInfo.Text = remaingString;
			}
		}

		void converter_ProgressChanged(object sender, EventArg<double> e)
		{
			totalProgress = e.Data;
		}

		void converter_AllFinished(object sender, EventArg<bool> e)
		{
			Dispatcher.Invoke((Action)(() =>
			{
				timer.Stop();
				taskBarItemInfo.ProgressState = TaskbarItemProgressState.None;

				convertDone = new ConvertDone();
				convertDone.BackButton += new EventHandler(convertDone_BackButton);
				convertDone.ShowOutputFolder += new EventHandler(convertDone_ShowOutputFolder);

				if(e.Data)
					Content = convertDone;
			}));
		}

		void convertDone_ShowOutputFolder(object sender, EventArgs e)
		{
			Process.Start(Converter.OutputFolder);
		}

		void convertDone_BackButton(object sender, EventArgs e)
		{
			Content = originalContent;
			Win.AllowDrop = true;
		}

		void progressBar_Cancelled(object sender, EventArgs e)
		{
			Content = originalContent;
			Win.AllowDrop = true;

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
			fileName = a[0];
			fileNameTextBox.Text = Path.GetFileName(fileName);
			getVideoInfo(fileName);
		}

		private void ShowLog(object sender, RoutedEventArgs e)
		{
			App.Log.Show();
		}

		private void ShowAboutWindow(object sender, RoutedEventArgs e)
		{
			About about = new About();
			about.Show();
		}
	}
}
