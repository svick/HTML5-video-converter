using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Video_converter
{
	/// <summary>
	/// Interaction logic for Log.xaml
	/// </summary>
	public partial class Log : Window
	{
		public Log()
		{
			InitializeComponent();
		}

		public void Add(string text)
		{
			Dispatcher.Invoke((Action)(() =>
			{
				TextLog.AppendText(DateTime.Now.ToString("HH:mm:ss.ff") + ": " +  text + "\n");
				TextLog.ScrollToEnd();
			}
			));
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			e.Cancel = true;
			Hide();
		}
	}
}
