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

	public class ProgressChangedEventArgs : EventArgs
	{
		public readonly double Progress;

		public ProgressChangedEventArgs(double Progress)
    {
        this.Progress = Progress;
    }    
	}

	public delegate void ProgressChangedEventHandler(object sender, ProgressChangedEventArgs e);
	public delegate void DoneUpdatedEventHandler(ConvertProcess sender, DataReceivedEventArgs e);
	public delegate void ConvertExitedEventHandler(object sender, EventArgs e);

	public class Converter
	{
		public event ProgressChangedEventHandler ProgressChanged;

		private Video video;
		private ConvertProcesses convertProcesses;

		public Converter(Video video)
		{
			this.video = video;
			convertProcesses = new ConvertProcesses(2);
			convertProcesses.ProgressChanged += new ProgressChangedEventHandler(convertProcesses_ProgressChanged);
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
			outputFilePath += "_" + size + ".webm";

			string parameters = string.Format("-y -i \"{0}\" -threads 4 -f webm -vcodec libvpx -acodec libvorbis -ab {1} -b {2} \"{3}\"", video.Path, "320k", "1000k", outputFilePath);

			ConvertProcess process = new ConvertProcess(parameters, false);
			process.ProccesingFile = outputFilePath;
			convertProcesses.Add(process);

			return true;
		}

		public void StopAll() 
		{
			convertProcesses.StopAll();
		}

		void convertProcesses_ProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			double percent = e.Progress / video.Duration.TotalMilliseconds;
			ProgressChanged(this, new ProgressChangedEventArgs(percent));
		}
	}

	public class ConvertProcesses
	{
		public event ProgressChangedEventHandler ProgressChanged;

		private int maxThreads;
		private int currentThreads;
		private List<ConvertProcess> processes = new List<ConvertProcess>(4);

		public ConvertProcesses(int maxThreads)
		{
			this.maxThreads = maxThreads;
		}

		public void Add(ConvertProcess process)
		{
			processes.Add(process);
			process.DoneUpdated += new DoneUpdatedEventHandler(process_DoneUpdated);
			process.ConvertExited += new ConvertExitedEventHandler(process_ConvertExited);
			process.Run();
		}

		void process_DoneUpdated(ConvertProcess sender, DataReceivedEventArgs e)
		{
			double avg = 0;
			int count = 0;

			foreach (ConvertProcess process in processes)
			{
				avg += process.Done.TotalMilliseconds;
				count++;
			}

			avg /= count;

			ProgressChanged(this,	new ProgressChangedEventArgs(avg));
		}

		public void Stop(ConvertProcess process) 
		{
			process.Stop();

			if (File.Exists(process.ProccesingFile))
			{
				File.Delete(process.ProccesingFile);
			}
		}

		public void StopAll() 
		{
			foreach (ConvertProcess process in processes)
			{
				Stop(process);
			}
		}

		void process_ConvertExited(object sender, EventArgs e)
		{
			System.Windows.Forms.MessageBox.Show("Konec");
		}
	}

	public class ConvertProcess
	{
		public string ProccesingFile;
		public TimeSpan Done;

		public event DoneUpdatedEventHandler DoneUpdated;
		public event ConvertExitedEventHandler ConvertExited;

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
			proc.EnableRaisingEvents = true;
			
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
				proc.Exited += new EventHandler(proc_Exited);
			}

			return output;
		}

		void proc_Exited(object sender, EventArgs e)
		{
			ConvertExited(this, e);
		}

		public void Stop() 
		{
			if (!proc.HasExited)
			{
				proc.Kill();
				proc.WaitForExit();
			}
		}

		static Regex timeRegex = new Regex(@"time=(\d*).(\d*)", RegexOptions.Compiled);

		private void proc_ErrorDataReceived(object sender, DataReceivedEventArgs e)
		{
			if (e.Data == null)
				return;

			Match m = timeRegex.Match(e.Data);
			if (m.Success)
			{
				Done = new TimeSpan(0, 0, 0, int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value));
			}

			DoneUpdated(this, e);
		}
	}
}