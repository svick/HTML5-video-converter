using System;
using System.Windows;
using System.Reflection;
using Video_converter.Properties;

namespace Video_converter
{
	public partial class About : Window
	{
		public About()
		{
			InitializeComponent();
			Version version = Assembly.GetExecutingAssembly().GetName().Version;
			string versionString = string.Format("{0}.{1}", version.Major, version.Minor);
			versionTextBlock.Text = App.GetLocalizedString("Version", versionString);
			librariesVersionsTextBlock.DataContext = Settings.Default;
		}
	}
}