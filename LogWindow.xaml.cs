﻿using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace Video_converter
{
	public partial class LogWindow : Window
	{
		public LogWindow()
		{
			InitializeComponent();
		}

		private void TextLog_TextChanged(object sender, TextChangedEventArgs e)
		{
			TextLog.ScrollToEnd();
		}
	}
}