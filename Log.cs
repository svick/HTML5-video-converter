using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Video_converter
{
	public class Log
	{
		private StringBuilder log = new StringBuilder(10);
		public LogWindow Window { private get; set; }

		public void Add(string line)
		{
			line = DateTime.Now.ToString("HH:mm:ss.ff") + ": " + line;
			log.AppendLine(line);

			if (Window != null)
			{
				Window.Add(line);
			}
		}

		public string Get()
		{
			return log.ToString();
		}
	}
}
