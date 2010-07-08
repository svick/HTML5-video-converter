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
	public delegate void NewLineEventHandler(object sender, DataReceivedEventArgs e);

	public class Converter
	{
		public event ProgressChangedEventHandler ProgressChanged;

		private Video video;
		private ConvertProcesses convertProcesses;

		public Converter(Video video)
		{
			this.video = video;
			convertProcesses = new ConvertProcesses(2);
		}

		public Video VideoInfo() 
		{
			string parameters = string.Format("-i \"{0}\"", video.Path);

			string output = new ConvertProcess(parameters).Run();

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

		public string PreviewImage()
		{
			string imageFileName = System.Guid.NewGuid().ToString() + ".png";

			int height = 100;
			int width = video.Width / (video.Height / height);

			string parameters = string.Format("-i \"{0}\" -an -vframes 1 -s {1}x{2} -ss 00:00:10 -f image2 {3}", video.Path, width, height, imageFileName);

			string output = new ConvertProcess(parameters).Run();

			if (File.Exists(imageFileName))
			{
				return imageFileName;
			}

			return string.Empty;
		}

		public bool Convert(string format, string size)
		{
			string outputFilePath = Path.GetDirectoryName(video.Path);
			outputFilePath += "\\" + Path.GetFileNameWithoutExtension(video.Path);
			outputFilePath += ".webm";

			string parameters = string.Format("-y -i \"{0}\" -threads 4 -f webm -vcodec libvpx -acodec libvorbis -ab {1} -b {2} \"{3}\"", video.Path, "320k", "1000k", outputFilePath);

			ConvertProcess process = new ConvertProcess(parameters, false);
			convertProcesses.Add(process, outputFilePath);
			process.NewLine += new NewLineEventHandler(parseConvertTime);

			return true;
		}

		public void StopAll() 
		{
			convertProcesses.StopAll();
		}

		static Regex timeRegex = new Regex(@"time=(\d*).(\d*)", RegexOptions.Compiled);

		private void parseConvertTime(object sender, DataReceivedEventArgs e)
		{
			Match m = timeRegex.Match(e.Data);
			if (m.Success)
			{
				TimeSpan progress = new TimeSpan(0, 0, 0, int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value));
				computeProgress(progress);
			}
		}

		private void computeProgress(TimeSpan progress)
		{
			double percent = progress.TotalMilliseconds / video.Duration.TotalMilliseconds * 100;
			ProgressChangedEventArgs args = new ProgressChangedEventArgs(percent);
			ProgressChanged(this, args);
		}
	}

	public class ProcessDescription
	{
		public ConvertProcess Process;
		public string File;
	}

	public class ConvertProcesses
	{
		private int maxThreads;
		private int currentThreads;
		private List<ProcessDescription> processes = new List<ProcessDescription>(4);

		public ConvertProcesses(int maxThreads)
		{
			this.maxThreads = maxThreads;
		}

		public void Add(ConvertProcess process, string file)
		{
			processes.Add(new ProcessDescription(){ Process = process, File = file });
			process.Run();
		}

		public void Stop(ProcessDescription processDescription) 
		{
			processDescription.Process.Stop();

			if (File.Exists(processDescription.File))
			{
				File.Delete(processDescription.File);
			}
		}

		public void StopAll() 
		{
			foreach (ProcessDescription process in processes)
			{
				Stop(process);
			}
		}
	}

	public class ConvertProcess
	{
		public event NewLineEventHandler NewLine;

		private string Ffmpeg { get; set; }
		private Process proc;
		private string parameters;
		private bool outputAtEnd;

		public ConvertProcess(string parameters, bool outputAtEnd = true) 
		{
			Ffmpeg = Settings.Default.ExeLocation;

			if (!File.Exists(Ffmpeg))
				throw new Exception("Soubor ffmpeg.exe nebyl nalezen");

			this.parameters = parameters;
			this.outputAtEnd = outputAtEnd;
		}

		public string Run()
		{
			string output = string.Empty;

			ProcessStartInfo startInfo = new ProcessStartInfo(Ffmpeg, parameters);
			startInfo.RedirectStandardError = true;
			startInfo.UseShellExecute = false;
			startInfo.CreateNoWindow = true;

			proc = Process.Start(startInfo);
			
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

		public void Stop() 
		{
			proc.Kill();
			proc.WaitForExit();
		}

		private void proc_ErrorDataReceived(object sender, DataReceivedEventArgs e)
		{
			if (e.Data == null)
				return;

			NewLine(this, e);
		}
	}
}