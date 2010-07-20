using System;
using System.Windows;
using System.Windows.Controls;

namespace Video_converter
{
	public partial class ConvertDone : UserControl
	{
		public event EventHandler BackButton = delegate { };
		public event EventHandler ShowOutputFolder = delegate { };

		public ConvertDone()
		{
			InitializeComponent();
		}

		private void Back_Click(object sender, RoutedEventArgs e)
		{
			BackButton(this, EventArgs.Empty);
		}

		private void ShowFolder_Click(object sender, RoutedEventArgs e)
		{
			ShowOutputFolder(this, EventArgs.Empty);
		}
	}
}