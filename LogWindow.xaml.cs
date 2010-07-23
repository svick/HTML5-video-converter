using System;
using System.Windows;

namespace Video_converter
{
	public partial class LogWindow : Window
	{
		public LogWindow()
		{
			InitializeComponent();
		}

		public void AddLine(string text)
		{
			Dispatcher.Invoke((Action)(() =>
			{
				text = text + "\n";
				Add(text);
			}
			));
		}

		public void Add(string text)
		{
			TextLog.AppendText(text);
			TextLog.ScrollToEnd();
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			App.LogWindow = null;
		}
	}
}