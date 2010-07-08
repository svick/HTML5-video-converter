using System.Windows;

namespace Video_converter
{
	public partial class App : Application
	{
		private void Application_Exit(object sender, ExitEventArgs e)
		{
			((MainWindow)MainWindow).Converter.StopAll();
		}
	}
}