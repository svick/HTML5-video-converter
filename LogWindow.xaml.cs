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
			if (TextLog.ExtentHeight < TextLog.ViewportHeight || TextLog.VerticalOffset + TextLog.ViewportHeight == TextLog.ExtentHeight)
				TextLog.ScrollToEnd();
			else
				TextLog.ScrollToVerticalOffset(TextLog.VerticalOffset);
		}

		private void TextLog_Loaded(object sender, RoutedEventArgs e)
		{
			TextLog.ScrollToEnd();
		}
	}
}