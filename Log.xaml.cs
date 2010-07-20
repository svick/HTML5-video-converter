using System;
using System.Windows;

namespace Video_converter
{
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