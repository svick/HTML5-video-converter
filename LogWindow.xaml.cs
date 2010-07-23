using System;
using System.Windows;

namespace Video_converter
{
	public partial class LogWindow : Window
	{
		public LogWindow()
		{
			InitializeComponent();
			TextLog.AppendText(App.Log.Get());
		}

		public void Add(string text)
		{
			Dispatcher.Invoke((Action)(() =>
			{
				TextLog.AppendText(text + "\n");
				TextLog.ScrollToEnd();
			}
			));
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			App.LogWindow = null;
		}
	}
}