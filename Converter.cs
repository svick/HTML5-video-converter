using System;
using System.Diagnostics;
using System.IO;
using Video_converter.Properties;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System.Text;

namespace Video_converter
{
	public class Video
	{
		public string Path { get; private set; }
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

	public class EventArg<T> : EventArgs
	{
		private readonly T eventData;

		public EventArg(T data)
		{
			eventData = data;
		}

		public T Data
		{
			get { return eventData; }
		}
	}

	public delegate void ProgressChangedEventHandler(object sender, EventArg<double> e);
	public delegate void DoneUpdatedEventHandler(ConvertProcess sender, DataReceivedEventArgs e);
	public delegate void ConvertExitedEventHandler(object sender, EventArg<bool> e);
	public delegate void AllFinishedEventHander(object sender, EventArgs e);

	public class Converter
	{
		public event ProgressChangedEventHandler ProgressChanged;
		public event AllFinishedEventHander AllFinished;

		private Video video;
		private ConvertProcesses convertProcesses;

		public Converter(Video video)
		{
			this.video = video;

			convertProcesses = new ConvertProcesses(2);
			convertProcesses.ProgressChanged += new ProgressChangedEventHandler(convertProcesses_ProgressChanged);
			convertProcesses.AllFinished += new AllFinishedEventHander(convertProcesses_AllFinished);
		}

		void convertProcesses_AllFinished(object sender, EventArgs e)
		{
			AllFinished(this, e);
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
			Match m = Regex.Match(output, @"Video: ([^,]*), [^,]*, (\d*)x(\d*), ");

			if (m.Success)
			{
				video.Format = m.Groups[1].Value;
				video.Width = int.Parse(m.Groups[2].Value);
				video.Height = int.Parse(m.Groups[3].Value);
			}

			// Audio info
			m = Regex.Match(output, @"Audio: ([^,]*), [^,]*, [^,]*, [^,]*(?:, (\d*))?");

			if (m.Success)
			{
				video.AudioFormat = m.Groups[1].Value;

				if(m.Groups[2].Value != String.Empty)
					video.AudioBitRate = int.Parse(m.Groups[2].Value);
			}

			// Duration
			m = Regex.Match(output, @"Duration: (\d*:\d*:\d*.\d*)");

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

		public bool Convert(string formatName, int height)
		{
			Format format = Format.GetFormatByName(formatName);

			string outputFilePath = Path.GetDirectoryName(video.Path);
			outputFilePath += "\\" + Path.GetFileNameWithoutExtension(video.Path);
			outputFilePath += "_" + height.ToString() + "." + format.Extension;

			string parameters = string.Format("-y -i \"{0}\" {1} \"{2}\"", video.Path, format.BuildParams(video, height), outputFilePath);

			ConvertProcess process = new ConvertProcess(parameters, false);
			process.ProccesingFile = outputFilePath;
			convertProcesses.Add(process);

			return true;
		}

		public void StopAll() 
		{
			convertProcesses.StopAll();
		}

		void convertProcesses_ProgressChanged(object sender, EventArg<double> e)
		{
			double percent = e.Data / video.Duration.TotalMilliseconds;
			ProgressChanged(this, new EventArg<double>(percent));
		}
	}

	public class ConvertProcesses
	{
		public event ProgressChangedEventHandler ProgressChanged;
		public event AllFinishedEventHander AllFinished;

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

			if (currentThreads < maxThreads)
			{
				++currentThreads;
				process.Run();
			}
		}

		void process_DoneUpdated(ConvertProcess sender, DataReceivedEventArgs e)
		{
			double avg = processes.Select(p => p.Done.TotalMilliseconds).Average();
			ProgressChanged(this,	new EventArg<double>(avg));
		}

		void process_ConvertExited(object sender, EventArg<bool> e)
		{
			--currentThreads;
			foreach (ConvertProcess process in processes)
			{
				if (process.Status == ConvertProcess.ProcessStatus.Waiting)
				{
					process.Run();
					++currentThreads;
					break;
				}
			}

			if (currentThreads == 0)
			{
				AllFinished(this, new EventArgs());
			}
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
			foreach (ConvertProcess process in processes.ToArray())
			{
				if (process.Status == ConvertProcess.ProcessStatus.Running)
				{
					Stop(process);
				}
				else if (process.Status == ConvertProcess.ProcessStatus.Waiting)
				{
					processes.Remove(process);
				}
			}
		}
	}

	public class ConvertProcess
	{
		public string ProccesingFile;
		public TimeSpan Done;
		public enum ProcessStatus { Waiting, Running, Finished, Failed, Stopped }
		public ProcessStatus Status;
		public StringBuilder ResultBuilder = new StringBuilder();

		public event DoneUpdatedEventHandler DoneUpdated;
		public event ConvertExitedEventHandler ConvertExited;

		private string ffmpegLocation;
		private Process proc;
		private string parameters;
		private bool outputAtEnd;
		

		public ConvertProcess(string parameters, bool outputAtEnd = true) 
		{
			if (Environment.Is64BitOperatingSystem && File.Exists(Settings.Default.ffmpegLocation64))
			{
				// use 64 bit version of ffmpeg
				ffmpegLocation = Settings.Default.ffmpegLocation64;
			}
			else if (File.Exists(Settings.Default.ffmpegLocation))
			{
				ffmpegLocation = Settings.Default.ffmpegLocation;
			}
			else
			{
				throw new Exception("Soubor " + ffmpegLocation + " nebyl nalezen");
			}

			this.parameters = parameters;
			this.outputAtEnd = outputAtEnd;
			Status = ProcessStatus.Waiting;
		}

		public string Run()
		{
			string output = string.Empty;

			ProcessStartInfo startInfo = new ProcessStartInfo(ffmpegLocation, parameters);
			startInfo.RedirectStandardError = true;
			startInfo.UseShellExecute = false;
			startInfo.CreateNoWindow = true;

			string dataDir = Path.GetDirectoryName(ffmpegLocation);

			// datadir for libx264
			if (!Path.IsPathRooted(dataDir))
				dataDir = Path.Combine(Directory.GetCurrentDirectory(), dataDir); 

			startInfo.EnvironmentVariables["FFMPEG_DATADIR"] = dataDir;

			proc = Process.Start(startInfo);
			proc.EnableRaisingEvents = true;

			Status = ProcessStatus.Running;
			
			if (outputAtEnd)
			{
				proc.WaitForExit();
				output = proc.StandardError.ReadToEnd();
				proc.Close();
				Status = ProcessStatus.Finished;
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
			// is video sucessfully converted?
			string lastLine = ResultBuilder.ToString().Trim().Split('\n').Last();
			bool success = lastLine.StartsWith("video:");

			if (success)
			{
				Status = ProcessStatus.Finished;
			}
			else if (Status != ProcessStatus.Stopped)
			{
				Status = ProcessStatus.Failed;
			}

			ConvertExited(this, new EventArg<bool>(success));
		}

		public void Stop() 
		{
			if (!proc.HasExited)
			{
				proc.Kill();
				proc.WaitForExit();
				Status = ProcessStatus.Stopped;
			}
		}

		static Regex timeRegex = new Regex(@"time=(\d*).(\d*)", RegexOptions.Compiled);

		private void proc_ErrorDataReceived(object sender, DataReceivedEventArgs e)
		{
			if (e.Data == null)
				return;

			ResultBuilder.AppendLine(e.Data);

			Match m = timeRegex.Match(e.Data);
			if (m.Success)
			{
				Done = new TimeSpan(0, 0, 0, int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value));
				DoneUpdated(this, e);
			}
		}
	}
}