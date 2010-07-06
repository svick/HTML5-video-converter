using System;
using System.Windows;
using System.Windows.Controls;

namespace Video_converter
{
	public partial class ProgressBar : UserControl
	{
		public event EventHandler Cancelled = delegate { };

		public ProgressBar()
		{
			InitializeComponent();
		}

		private void Cancel_Click(object sender, RoutedEventArgs e)
		{
			Cancelled(this, EventArgs.Empty);
		}
	}
}