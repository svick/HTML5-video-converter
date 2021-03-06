﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace Video_converter
{
	public class Log : DependencyObject
	{
		static DependencyPropertyKey LogTextProperty = DependencyProperty.RegisterReadOnly("LogText", typeof(string), typeof(Log), new PropertyMetadata());

		private string log = "";
		private LogWindow window;

		public void Add(string line)
		{
			line = string.Format("{0}{1:HH:mm:ss.ff}: {2}", log == "" ? "" : Environment.NewLine, DateTime.Now, line.TrimEnd());
			log += line;

			App.Current.Dispatcher.BeginInvoke((Action)(() =>
			{
				SetValue(LogTextProperty, log);
			}), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
		}

		public string LogText
		{
			get
			{
				return (string)GetValue(LogTextProperty.DependencyProperty);
			}
		}

		public void ShowWindow()
		{
			window = new LogWindow();
			window.DataContext = this;
			window.Show();
		}
	}
}
