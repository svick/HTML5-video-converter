﻿using System;
using System.Diagnostics;
using System.IO;
using Video_converter.Properties;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Video_converter
{
	public class Video
	{
		public string Path { get; set; }
		public TimeSpan Duration { get; set; }
		public int BitRate { get; set; }
		public string Format { get; set; }
		public int AudioBitRate { get; set; }
		public string AudioFormat { get; set; }
		public int Width  { get; set; }
		public int Height { get; set; }

		public Video(string path)
		{
			if (!File.Exists(path))
				throw new Exception("File not found");

			this.Path = path;
		}
	}

	public class Converter
	{
		private string ffmpeg { get; set; }

		public Converter()
		{
			ffmpeg = Settings.Default.ExeLocation;

			if (!File.Exists(ffmpeg))
				throw new Exception("Soubor ffmpeg.exe nebyl nalezen");
		}

		public Video VideoInfo(Video video) 
		{
			string parameters = string.Format("-i \"{0}\"", video.Path);
			string output = run(ffmpeg, parameters);

			// is regular video file
			Match m = new Regex("Invalid data found when processing input").Match(output);

			if(m.Success)
			{
				throw new Exception("Neplatný formát vstupního souboru");
			}

			m = new Regex("could not find codec parameters").Match(output);

			if (m.Success)
			{
				throw new Exception("Tento formát videa nelze převést");
			}

			//System.Windows.Forms.MessageBox.Show(output);

			return video;
		}

		private string run(string exeFile, string parameters)
		{
			ProcessStartInfo startInfo = new ProcessStartInfo(exeFile, parameters);
			startInfo.RedirectStandardError = true;
			startInfo.UseShellExecute = false;
			startInfo.CreateNoWindow = true;

			Process proc = Process.Start(startInfo);
			proc.WaitForExit();
			string output = proc.StandardError.ReadToEnd();
			proc.Close();

			return output;
		}
	}
}