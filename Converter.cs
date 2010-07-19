﻿using System;
using System.Diagnostics;
using System.IO;
using Video_converter.Properties;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System.Text;

namespace Video_converter
{
	public class Size
	{
		public int Width { get; set; }
		public int Height { get; set; }
	}

	public class BitRate
	{
		public int Audio { get; set; }
		public int Video { get; set; }
	}

	public class Video
	{
		public string Path { get; private set; }
		public TimeSpan Duration { get; set; }
		public string Format { get; set; }
		public string AudioFormat { get; set; }
		public BitRate BitRate = new BitRate();
		public Size Size = new Size();

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
	public delegate void ConvertExitedEventHandler(ConvertProcess sender, EventArg<bool> e);
	public delegate void AllFinishedEventHander(object sender, EventArg<bool> e);

	public class Converter
	{
		public event ProgressChangedEventHandler ProgressChanged;
		public event AllFinishedEventHander AllFinished;
		public string OutputFolder { get; private set; }

		private Video video;
		private ConvertProcesses convertProcesses;

		public Converter(Video video)
		{
			this.video = video;

			convertProcesses = new ConvertProcesses(Environment.ProcessorCount);
			convertProcesses.ProgressChanged += new ProgressChangedEventHandler(convertProcesses_ProgressChanged);
			convertProcesses.AllFinished += new AllFinishedEventHander(convertProcesses_AllFinished);

			OutputFolder = Path.GetDirectoryName(video.Path);
		}

		void convertProcesses_AllFinished(object sender, EventArg<bool> e)
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
				video.Size.Width = int.Parse(m.Groups[2].Value);
				video.Size.Height =  int.Parse(m.Groups[3].Value);
			}

			// Audio info
			m = Regex.Match(output, @"Audio: ([^,]*), [^,]*, [^,]*, [^,]*(?:, (\d*))?");

			if (m.Success)
			{
				video.AudioFormat = m.Groups[1].Value;

				if(m.Groups[2].Value != String.Empty)
					video.BitRate.Audio = int.Parse(m.Groups[2].Value);
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
			int width = video.Size.Width / (video.Size.Height / height);

			string parameters = string.Format("-i \"{0}\" -an -vframes 1 -s {1}x{2} -ss 00:00:10 -f image2 {3}", video.Path, width, height, imageFileName);

			string output = new ConvertProcess(parameters).Run();

			if (File.Exists(imageFileName))
			{
				return imageFileName;
			}

			return string.Empty;
		}

		public void Convert(string formatName, int height = 0, int pass = 0)
		{
			Format format = Format.GetFormatByName(formatName);

			string outputFilePath = Path.Combine(OutputFolder, string.Format("{0}_{1}p.{2}", Path.GetFileNameWithoutExtension(video.Path), height.ToString(), format.Extension));

			string parameters = string.Format("-y -i \"{0}\" {1} \"{2}\"", video.Path, format.BuildParams(video, height), outputFilePath);

			ConvertProcess process = new ConvertProcess(parameters, false);
			process.ProcessName = string.Format("{0}_{1}_{2}", formatName, height, "0");
			process.ProcessingFile = outputFilePath;

			if(pass == 2)
				process.ParentProcessName = string.Format("{0}_{1}_{2}", formatName, height, "1");

			convertProcesses.Add(process);
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

		void process_ConvertExited(ConvertProcess sender, EventArg<bool> e)
		{
			--currentThreads;

			foreach (ConvertProcess process in processes)
			{
				if (process.Status == ConvertProcess.ProcessStatus.Waiting)
				{
					// depend process
					if (process.ParentProcessName != null)
					{
						bool sucess = false;
						foreach (ConvertProcess p in processes)
						{
							if (p.ProcessName == process.ParentProcessName)
							{
								if (p.Status == ConvertProcess.ProcessStatus.Finished)
									sucess = true;
								break;
							}
						}

						if (!sucess)
							break;
					}

					process.Run();
					++currentThreads;
					break;
				}
			}

			if (currentThreads == 0)
			{
				AllFinished(this, new EventArg<bool>(sender.Status == ConvertProcess.ProcessStatus.Finished));
			}
		}

		public void StopAll() 
		{
			App.Log.Add("ConvertProcesses.StopAll()");
			foreach (ConvertProcess process in processes)
			{ 
				if (process.Status == ConvertProcess.ProcessStatus.Waiting)
				{
					process.Status = ConvertProcess.ProcessStatus.Stopped;
				}
			}

			foreach (ConvertProcess process in processes)
			{
				if (process.Status == ConvertProcess.ProcessStatus.Running)
				{
					process.Stop();
				}
			}
		}
	}

	public class ConvertProcess
	{
		public string ProcessName;
		public string ParentProcessName;
		public string ProcessingFile;
		public TimeSpan Done;
		public enum ProcessStatus { Waiting, Running, Finished, Failed, Stopped }
		public ProcessStatus Status;
		public StringBuilder ResultBuilder = new StringBuilder();

		public event DoneUpdatedEventHandler DoneUpdated;
		public event ConvertExitedEventHandler ConvertExited;

		private Process proc;
		private string parameters;
		private bool outputAtEnd;

		public ConvertProcess(string parameters, bool outputAtEnd = true)
		{
			App.Log.Add(parameters);
			this.parameters = parameters;
			this.outputAtEnd = outputAtEnd;
			Status = ProcessStatus.Waiting;
		}

		public string Run()
		{
			string output = string.Empty;

			ProcessStartInfo startInfo = new ProcessStartInfo(App.FfmpegLocation, parameters);
			startInfo.RedirectStandardError = true;
			startInfo.UseShellExecute = false;
			startInfo.CreateNoWindow = true;
			startInfo.WorkingDirectory = Path.GetDirectoryName(ProcessingFile);

			string dataDir = Path.GetDirectoryName(App.FfmpegLocation);

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
				App.Log.Add(output);
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

		public void Stop() 
		{
			App.Log.Add("ConvertProcess.Stop()");
			Status = ProcessStatus.Stopped;
			if (!proc.HasExited)
			{
				proc.Kill();
				App.Log.Add("Process byl zastaven");
			}
		}

		public void DeleteProcessingFile()
		{
			if (File.Exists(ProcessingFile))
			{
				File.Delete(ProcessingFile);
			}
		}

		void proc_Exited(object sender, EventArgs e)
		{
			bool success;
			if (Status == ProcessStatus.Stopped)
			{
				success = false;
			}
			else
			{
				App.Log.Add("Proces převodu skončil");
				success = ResultBuilder.ToString().IndexOf("video:") != -1;

				if (!success)
				{
					System.Threading.Thread.Sleep(100);
					success = ResultBuilder.ToString().IndexOf("video:") != -1;
				}
			}

			if (success)
			{
				Status = ProcessStatus.Finished;
			}
			else if (Status != ProcessStatus.Stopped)
			{
				Status = ProcessStatus.Failed;
			}

			if (Status != ProcessStatus.Finished)
			{
				App.Log.Add("Při převodu nastala chyba, výstupní soubor bude smazán");
				DeleteProcessingFile();
			}

			ConvertExited(this, new EventArg<bool>(success));
		}

		static Regex timeRegex = new Regex(@"time=(\d*).(\d*)", RegexOptions.Compiled);

		private void proc_ErrorDataReceived(object sender, DataReceivedEventArgs e)
		{
			if (e.Data == null)
				return;

			App.Log.Add(ProcessName + ": " + e.Data);

			ResultBuilder.AppendLine(e.Data);

			Match m = timeRegex.Match(e.Data);
			if (m.Success)
			{
				Done = new TimeSpan(0, 0, 0, int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value) * 10);
				DoneUpdated(this, e);
			}
		}
	}
}