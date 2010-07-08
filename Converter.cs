using System;
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

	public class ProgressChangedEventArgs : EventArgs
	{
		public readonly double Progress;

		public ProgressChangedEventArgs(double Progress)
    {
        this.Progress = Progress;
    }    
	}

	public delegate void ProgressChangedEventHandler(object sender, ProgressChangedEventArgs e);

	public class Converter
	{
		public event ProgressChangedEventHandler ProgressChanged;

		private string ffmpeg { get; set; }
		private Video video;

		public Converter(Video video)
		{
			this.video = video;
			ffmpeg = Settings.Default.ExeLocation;

			if (!File.Exists(ffmpeg))
				throw new Exception("Soubor ffmpeg.exe nebyl nalezen");
		}

		public Video VideoInfo() 
		{
			string parameters = string.Format("-i \"{0}\"", video.Path);

			string output = run(ffmpeg, parameters, true);

			// is regular video file
			if (output.Contains("Invalid data found when processing input"))
			{
				throw new Exception("Neplatný formát vstupního souboru");
			}

			if (output.Contains("could not find codec parameters"))
			{
				throw new Exception("Tento formát videa nelze převést");
			}

			// TODO: Kontrola správnosti regexpů a ošetření chyb
			// Video info
			Match m = new Regex(@"Video: ([^,]*), [^,]*, (\d*)x(\d*), ").Match(output);

			if (m.Success)
			{
				video.Format = m.Groups[1].Value;
				video.Width = int.Parse(m.Groups[2].Value);
				video.Height = int.Parse(m.Groups[3].Value);
			}

			// Audio info
			m = new Regex(@"Audio: ([^,]*), [^,]*, [^,]*, [^,]*, (\d*)").Match(output);

			if (m.Success)
			{
				video.AudioFormat = m.Groups[1].Value;
				video.AudioBitRate = int.Parse(m.Groups[2].Value);
			}

			// Duration
			m = new Regex(@"Duration: (\d*:\d*:\d*.\d*)").Match(output);

			if (m.Success)
			{
				video.Duration = TimeSpan.Parse(m.Groups[1].Value);
			}

			return video;
		}

		public string previewImage()
		{
			string imageFileName = System.Guid.NewGuid().ToString() + ".png";

			int height = 100;
			int width = video.Width / (video.Height / height);

			string parameters = string.Format("-i \"{0}\" -an -vframes 1 -s {1}x{2} -ss 00:00:10 -f image2 {3}", video.Path, width, height, imageFileName);
			
			string output = run(ffmpeg, parameters);

			if (File.Exists(imageFileName))
			{
				return imageFileName;
			}

			return string.Empty;
		}

		public bool convert(string format, string size)
		{
			string outputFilePath = Path.GetDirectoryName(video.Path);
			outputFilePath += "\\" + Path.GetFileNameWithoutExtension(video.Path);
			outputFilePath += ".webm";

			string parameters = string.Format("-y -i \"{0}\" -threads 4 -f webm -vcodec libvpx -acodec libvorbis -ab {1} -b {2} \"{3}\"", video.Path, "320k", "1000k", outputFilePath);

			string output = run(ffmpeg, parameters, false);

			return true;
		}

		private string run(string exeFile, string parameters, bool outputAtEnd = true)
		{
			string output = string.Empty;

			ProcessStartInfo startInfo = new ProcessStartInfo(exeFile, parameters);
			startInfo.RedirectStandardError = true;
			startInfo.UseShellExecute = false;
			startInfo.CreateNoWindow = true;

			Process proc = Process.Start(startInfo);

			if (outputAtEnd)
			{
				proc.WaitForExit();
				output = proc.StandardError.ReadToEnd();
				proc.Close();
			}
			else
			{
				proc.BeginErrorReadLine();
				proc.ErrorDataReceived += new DataReceivedEventHandler(proc_ErrorDataReceived);
			}

			return output;
		}

		static Regex timeRegex = new Regex(@"time=(\d*).(\d*)", RegexOptions.Compiled);

		private void proc_ErrorDataReceived(object sender, DataReceivedEventArgs e)
		{
			if (e.Data == null)
				return;

			Match m = timeRegex.Match(e.Data);
			if (m.Success)
			{
				TimeSpan progress = new TimeSpan(0, 0, 0, int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value));
				computeProgress(progress);
			}
			return;
		}

		private void computeProgress(TimeSpan progress)
		{
			double percent = progress.TotalMilliseconds / video.Duration.TotalMilliseconds * 100;
			ProgressChangedEventArgs args = new ProgressChangedEventArgs(percent);
			ProgressChanged(this, args);
		}
	}
}