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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Video_converter
{
	/// <summary>
	/// Interaction logic for ConvertDone.xaml
	/// </summary>
	public partial class ConvertDone : UserControl
	{
		public event EventHandler BackButton = delegate { };

		public ConvertDone()
		{
			InitializeComponent();
		}

		private void Back_Click(object sender, RoutedEventArgs e)
		{
			BackButton(this, EventArgs.Empty);
		}
	}
}
