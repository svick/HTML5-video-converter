﻿using System;
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

		private string fileName;
		private string lastSelectedFileName;
		
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
			try
			{
				this.fileName = fileName;
				getVideoInfo();
				fileNameTextBox.Text = Path.GetFileName(fileName);
			}
			catch (ConverterException e)
			{
				fileNameTextBox.Text = null;
				this.fileName = null;
				App.ErrorMessageBox(e.Message);
			}
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

				foreach (CheckBox resolutionCheckBox in resolutions)
				{
					int height = int.Parse((string)resolutionCheckBox.Tag);

					resolutionCheckBox.IsEnabled = (video.Size.Height >= height || video.Size.Width >= (int)Math.Ceiling((double)height * 16 / 9));
					if (resolutionCheckBox.IsEnabled)
					{
						Size newSize = video.NewSize(height);
						BitRate newBitRate = video.ComputeNewBitRate(newSize);
						double expectedSize = (((newBitRate.Video + newBitRate.Audio) * video.Duration.TotalSeconds) / 8000); // in MB

						resolutionCheckBox.ToolTip = string.Format("{0}, {1} kb/s, ≈{2} MB", newSize.ToString("×"), newBitRate.Video, expectedSize.ToString("F"));
					}
				}
			}
			catch (ConverterException e)
			{
				height1080.IsEnabled = height720.IsEnabled = height480.IsEnabled = true;
				height1080.ToolTip = height720.ToolTip = height480.ToolTip = null;
				throw e;
			}
		}

		private void File_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog ofd = new OpenFileDialog();

			ofd.Filter =  App.GetLocalizedString("VideoFormats") + "|" + Settings.Default.supportedFileExtension  + "|" + App.GetLocalizedString("AllFiles") + "|*.*";

			ofd.CheckPathExists = true;
			ofd.CheckFileExists = true;
			ofd.Multiselect = false;

			string file = fileName != null ? fileName : lastSelectedFileName;

			if (file != null && Directory.Exists(Path.GetDirectoryName(file)))
			{
				ofd.InitialDirectory = Path.GetDirectoryName(file);

				if (File.Exists(file))
					ofd.FileName = file;
			}
			else
				ofd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);

			if (ofd.ShowDialog() == true)
			{
				lastSelectedFileName = ofd.FileName;
				selectedFile(ofd.FileName);
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

		private void allFinished(bool showMain = false, ConvertProcess.ProcessStatus status = ConvertProcess.ProcessStatus.Waiting)
		{
			App.Log.Add(string.Format("Doba trvání převodu: {0}", DateTime.Now - startTime));
			timer.Stop();
			TaskbarItemInfo.ProgressState = TaskbarItemProgressState.None;

			if (showMain || status == ConvertProcess.ProcessStatus.Stopped)
			{
				Content = originalContent;
				AllowDrop = true;
			}
			else
			{
				convertDone = new ConvertDone(status);
				convertDone.BackButton += new EventHandler(convertDone_BackButton);
				convertDone.ShowOutputFolder += new EventHandler(convertDone_ShowOutputFolder);
				Content = convertDone;
			}
		}

		void converter_AllFinished(object sender, EventArg<ConvertProcess.ProcessStatus> e)
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

			if (Converter != null)
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

			if (a.Length > 0)
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