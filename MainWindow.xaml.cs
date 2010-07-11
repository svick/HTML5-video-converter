using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shell;

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
		Converter converter;
		
		ProgressBar progressBar;
		object mainContent;
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

		private void File_Click(object sender, RoutedEventArgs e)
		{
			fileName = fileNameTextBox.Text;
			System.Windows.Forms.OpenFileDialog ofd = new System.Windows.Forms.OpenFileDialog();

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

			if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
			{
				fileName = ofd.FileName;
				fileNameTextBox.Text = fileName;
				
				GetVideoInfo(fileName);
			}
		}

		private void GetVideoInfo(string FileName)
		{
			Video video = new Video(FileName);
			converter = new Converter(video);
			converter.ProgressChanged += new ProgressChangedEventHandler(converter_ProgressChanged);
			converter.AllFinished += new AllFinishedEventHander(converter_AllFinished);
			Converter = new Converter(video);
			Converter.ProgressChanged += new ProgressChangedEventHandler(converter_ProgressChanged);
			Converter.AllFinished += new AllFinishedEventHander(converter_AllFinished);

			try
			{
				converter.VideoInfo();
				Converter.VideoInfo();
				ConvertButton.IsEnabled = true;

				height1080.IsEnabled = (video.Size.Height >= 1080 || video.Size.Width >= 1920);
				height720.IsEnabled  = (video.Size.Height >= 720  || video.Size.Width >= 1280);
				height480.IsEnabled  = (video.Size.Height >= 480  || video.Size.Width >= 854);
			}
			catch (Exception e)
			{
				ConvertButton.IsEnabled = false;
				System.Windows.Forms.MessageBox.Show(e.Message);
			}
		}

		private void Convert_Click(object sender, RoutedEventArgs e)
		{
			progressBar = new ProgressBar();

			progressBar.Cancelled += new System.EventHandler(progressBar_Cancelled);
			mainContent = Content;

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
					string format = formatCheckBox.Name;
					foreach (CheckBox resolutionCheckBox in resolutions)
						if (resolutionCheckBox.IsChecked == true)
						{
							int height = int.Parse((string)resolutionCheckBox.Tag);
							converter.Convert(format, height);
						}
				}
		}

		void timer_Tick(object sender, EventArgs e)
		{
			taskBarItemInfo.ProgressValue = totalProgress;
			progressBar.bar.Value = totalProgress * 100;
			TimeSpan remain = TimeSpan.FromMilliseconds((DateTime.Now - startTime).TotalMilliseconds * (1 - totalProgress) / totalProgress);
			progressBar.textInfo.Text = "Hotovo: " + totalProgress.ToString("P") + ", zbývá " + remain.ToString();
		}

		void converter_ProgressChanged(object sender, EventArg<double> e)
		{
			totalProgress = e.Data;
		}

		void converter_AllFinished(object sender, EventArgs e)
		{
			Dispatcher.Invoke((Action)(() =>
			{
				timer.Stop();
				progressBar.bar.Value = 100;
				progressBar.textInfo.Text = "Hotovo: 100 %";
				taskBarItemInfo.ProgressState = TaskbarItemProgressState.None;
			}));
		}

		void progressBar_Cancelled(object sender, EventArgs e)
		{
			Content = mainContent;
			converter.StopAll();

			if(Converter != null)
				Converter.StopAll();
		}

		private void Window_Closed(object sender, EventArgs e)
		{
			converter.StopAll();
			Converter.StopAll();
		}
	}
}
