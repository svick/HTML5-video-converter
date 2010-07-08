using System.Windows;

namespace Video_converter
{
	public partial class App : Application
	{
		private void Application_Exit(object sender, ExitEventArgs e)
		{
			Converter converter = ((MainWindow)MainWindow).Converter;
			if (converter != null)
				converter.StopAll();
		}
	}
}