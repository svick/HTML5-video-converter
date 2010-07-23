using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Video_converter
{
	public class Log
	{
		private StringBuilder log = new StringBuilder(10);
		private LogWindow window;

		public void Add(string line)
		{
			line = DateTime.Now.ToString("HH:mm:ss.ff") + ": " + line;
			log.AppendLine(line);

			if (window != null)
			{
				window.AddLine(line);
			}
		}

		public string Get()
		{
			return log.ToString();
		}

		public void ShowWindow()
		{
			window = new LogWindow();
			window.Add(Get());
			window.Show();
		}
	}
}
