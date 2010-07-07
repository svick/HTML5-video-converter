using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;

namespace Video_converter
{
	public partial class MainWindow : Window
	{
		string fileName;
		Converter converter;

		ProgressBar progressBar;

		public MainWindow()
		{
			converter = new Converter();
			InitializeComponent();

			resolutions = new System.Windows.Controls.CheckBox[] { height480, height720, height1080 };
			formats = new System.Windows.Controls.CheckBox[] { webm, h264, theora };
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

            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
			{
				fileName = ofd.FileName;
				fileNameTextBox.Text = fileName;
			}
		}

		System.Windows.Controls.CheckBox[] resolutions;
		System.Windows.Controls.CheckBox[] formats;

		private void Convert_Click(object sender, RoutedEventArgs e)
		{
			progressBar = new ProgressBar();

			progressBar.Cancelled += new System.EventHandler(progressBar_Cancelled);

			Content = progressBar;
			

			/*foreach (CheckBox formatCheckBox in formats)
				if (formatCheckBox.IsChecked == true)
				{
					string format = formatCheckBox.Name;
					foreach (CheckBox resolutionCheckBox in resolutions)
						if (resolutionCheckBox.IsChecked == true)
						{
							int height = int.Parse((string)resolutionCheckBox.Tag);
							converter.ConvertFormat(fileNameTextBox.Text, format, height);
						}
				}*/

		}

		void progressBar_Cancelled(object sender, EventArgs e)
		{
		}
	}
}
