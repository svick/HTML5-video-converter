using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shell;
using Microsoft.Win32;

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
			fileNameTextBox.Text = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
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
				MessageBox.Show(e.Message);
			}
		}

		private void File_Click(object sender, RoutedEventArgs e)
		{
			fileName = fileNameTextBox.Text;
			OpenFileDialog ofd = new OpenFileDialog();

			ofd.Filter = "Video|*.avi;*.mp4;*.wmv;*.ogv;*.webm;*.mkv;*.flv;*.mov;*.3gp|Všechny soubory|*.*";

			ofd.CheckPathExists = true;
			ofd.CheckFileExists = true;
			ofd.Multiselect = false;

			if (File.Exists(fileName))
				ofd.FileName = fileName;
			else if (Directory.Exists(fileName))
				ofd.InitialDirectory = fileName;
			else
				ofd.InitialDirectory = Path.GetDirectoryName(fileName);

			if (ofd.ShowDialog() == true)
			{
				fileName = ofd.FileName;
				fileNameTextBox.Text = fileName;
				
				getVideoInfo(fileName);
			}
		}

		private void Convert_Click(object sender, RoutedEventArgs e)
		{
			if (Converter == null)
				return;

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

		string remainSeconds(TimeSpan remain)
		{
			if (remain.Seconds == 1)
				return "1 sekunda";
			else if (remain.Seconds <= 4)
				return remain.Seconds.ToString() + " sekundy";
			else
				return remain.Seconds.ToString() + " sekund";
		}

		string remainMinutes(TimeSpan remain)
		{
			if (remain.Minutes == 1)
				return "1 minuta";
			else if (remain.Minutes <= 4)
				return remain.Minutes.ToString() + " minuty";
			else
				return remain.Minutes.ToString() + " minut";
		}

		string remainHours(TimeSpan remain)
		{
			if (remain.Hours == 1)
				return "1 hodina";
			else if (remain.Hours <= 4)
				return remain.Hours.ToString() + " hodiny";
			else
				return remain.Hours.ToString() + " hodin";
		}

		void timer_Tick(object sender, EventArgs e)
		{
			taskBarItemInfo.ProgressValue = totalProgress;
			progressBar.bar.Value = totalProgress * 100;

			if (totalProgress != 0)
			{
				TimeSpan remain = TimeSpan.FromMilliseconds((DateTime.Now - startTime).TotalMilliseconds * (1 - totalProgress) / totalProgress);

				string remainString = ", zbývá ";

				if (remain.TotalSeconds < 10)
					remainString += "několik sekund";
				else if (remain.TotalSeconds < 60)
					remainString += remainSeconds(remain);
				else if (remain.TotalMinutes < 10)
					remainString += remainMinutes(remain) + " a " + remainSeconds(remain);
				else if (remain.TotalMinutes < 60)
					remainString += remainMinutes(remain);
				else
					remainString += remainHours(remain) + " a " + remainMinutes(remain);

				progressBar.textInfo.Text = "Hotovo: " + totalProgress.ToString("P") + remainString;
			}
			else
			{
				progressBar.textInfo.Text = "Hotovo: " + totalProgress.ToString("P");
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
			fileNameTextBox.Text = a[0];
			getVideoInfo(a[0]);
		}

		private void MenuItem_Click(object sender, RoutedEventArgs e)
		{
			App.Log.Show();
		}
	}
}
