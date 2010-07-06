using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Microsoft.Win32;
using System.IO;

namespace Video_converter
{
	public partial class MainWindow : Window
	{
		string fileName;
		Converter converter;

		public MainWindow()
		{
			converter = new Converter();
			InitializeComponent();
			resolutions = new CheckBox[] { height480, height720, height1080 };
			formats = new CheckBox[] { webm, h264, theora };
		}

		private void File_Click(object sender, RoutedEventArgs e)
		{
			fileName = fileNameTextBox.Text;
			OpenFileDialog ofd = new OpenFileDialog();

			ofd.Filter = "Všechny soubory|*.*";

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
			}
		}

		CheckBox[] resolutions;
		CheckBox[] formats;

		private void Convert_Click(object sender, RoutedEventArgs e)
		{
			foreach (CheckBox formatCheckBox in formats)
				if (formatCheckBox.IsChecked == true)
				{
					string format = formatCheckBox.Name;
					foreach (CheckBox resolutionCheckBox in resolutions)
						if (resolutionCheckBox.IsChecked == true)
						{
							int height = int.Parse((string)resolutionCheckBox.Tag);
							converter.ConvertFormat(fileNameTextBox.Text, format, height);
						}
				}
		}
	}
}
