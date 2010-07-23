using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Video_converter
{
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
	public delegate void AllFinishedEventHander(object sender, EventArg<ConvertProcess.ProcessStatus> e);

	public class ConverterException : Exception 
	{
		public ConverterException() : base() { }
		public ConverterException(string message) : base(message) 
		{
			App.Log.Add("Nastala vyjímka: " + message);
		}
		public ConverterException(string message, System.Exception inner) : base(message, inner) { }
	}

	public class Converter
	{
		public event ProgressChangedEventHandler ProgressChanged;
		public event AllFinishedEventHander AllFinished;
		public bool ConvertSupported { get; private set; }
		public string OutputFolder { get; private set; }
		public string TempFolder { get; private set; }

		private Video video;
		private ConvertProcesses convertProcesses;

		public Converter(Video video)
		{
			this.video = video;

			convertProcesses = new ConvertProcesses(Environment.ProcessorCount);
			convertProcesses.ProgressChanged += new ProgressChangedEventHandler(convertProcesses_ProgressChanged);
			convertProcesses.AllFinished += new AllFinishedEventHander(convertProcesses_AllFinished);

			OutputFolder = Path.GetDirectoryName(video.Path);
			TempFolder = Path.Combine(OutputFolder, Path.GetFileNameWithoutExtension(video.Path) + ".tmp");
			ConvertSupported = false;
		}

		void convertProcesses_AllFinished(object sender, EventArg<ConvertProcess.ProcessStatus> e)
		{
			if (Directory.Exists(TempFolder))
			{
				Directory.Delete(TempFolder, true);
			}

			AllFinished(this, e);
		}

		public Video VideoInfo() 
		{
			if (!video.Exist()) 
			{
				ConvertSupported = false;
				throw new ConverterException(App.GetLocalizedString("FileNotFound", video.Path));
			}

			ParamsBuilder parameters = new ParamsBuilder { InputFile = video.Path };

			string output = new ConvertProcess(parameters).Run();

			// is regular video file
			if (output.Contains("Invalid data found when processing input"))
			{
				throw new ConverterException(App.GetLocalizedString("InvalidInputFileFormat"));
			}

			if (output.Contains("could not find codec parameters"))
			{
				throw new ConverterException(App.GetLocalizedString("CantConvert"));
			}

			// TODO: Kontrola správnosti regexpů a ošetření chyb
			// Video info
			Match m = Regex.Match(output, @"Video: ([^,]*), [^,]*, (\d*)x(\d*)");

			if (m.Success)
			{
				video.Format = m.Groups[1].Value;
				video.Size.Width = int.Parse(m.Groups[2].Value);
				video.Size.Height =  int.Parse(m.Groups[3].Value);
			}

			m = Regex.Match(output, @"Video: .* (\d*) kb/s");

			if (m.Success)
			{
				video.BitRate.Video = int.Parse(m.Groups[1].Value);
			}

			// Audio info
			m = Regex.Match(output, @"Audio: ([^,]*), [^,]*, [^,]*, [^,]*(?:, (\d*))?");

			if (m.Success)
			{
				video.AudioFormat = m.Groups[1].Value;

				if(m.Groups[2].Value != String.Empty)
					video.BitRate.Audio = int.Parse(m.Groups[2].Value);
			}

			if (video.BitRate.Video == 0 && video.BitRate.Audio != 0)
			{
				m = Regex.Match(output, @"bitrate: (\d*) kb/s");

				if (m.Success)
				{
					// Video bitrate = total bitrate - audio bitrate 
					int bitrate = int.Parse(m.Groups[1].Value);
					video.BitRate.Video = bitrate - video.BitRate.Audio;
				}
			}

			// Duration
			m = Regex.Match(output, @"Duration: (\d*:\d*:\d*.\d*)");

			if (m.Success)
			{
				video.Duration = TimeSpan.Parse(m.Groups[1].Value);
			}

			m = Regex.Match(output, @"([\d.]*) (?:fps|tbr)");

			if (m.Success)
			{
				video.FrameCount = (int)(video.Duration.TotalSeconds * float.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture));
			}

			ConvertSupported = true;

			return video;
		}

		public string PreviewImage()
		{
			if (!video.Exist())
				throw new ConverterException(App.GetLocalizedString("FileNotFound", video.Path));

			string imageFileName = System.Guid.NewGuid().ToString() + ".png";

			int height = 100;
			int width = video.Size.Width / (video.Size.Height / height);

			ParamsBuilder parameters = new ParamsBuilder();
			parameters.Add("an");
			parameters.Add("vframes", 1);
			parameters.Add("s", string.Format("{1}x{2}", width, height));
			parameters.Add("f", "image2");
			parameters.OutputFile = imageFileName;
			parameters.InputFile = video.Path;

			string output = new ConvertProcess(parameters).Run();

			if (File.Exists(imageFileName))
			{
				return imageFileName;
			}

			return string.Empty;
		}

		public void Convert(string formatName, int height = 0, int passNumber = 1)
		{
			if (!video.Exist())
				throw new ConverterException(App.GetLocalizedString("FileNotFound", video.Path));

			if (passNumber == 1)
			{
				createConvertProcess(formatName, height, 0);
			}
			else if (passNumber == 2)
			{
				ConvertProcess parentProcess = createConvertProcess(formatName, height, 1);
				createConvertProcess(formatName, height, 2, parentProcess);
			}
			else
			{
				throw new Exception(string.Format("Wrong pass number {0}. Must be 1 or 2.", passNumber));
			}
		}

		private ConvertProcess createConvertProcess(string formatName, int height = 0, int pass = 0, ConvertProcess parentProcess = null)
		{
			Format format = Format.GetFormatByName(formatName);

			ParamsBuilder parameters = format.BuildParams(video, height, pass);
			parameters.Add("y");
			parameters.InputFile = video.Path;
			parameters.OutputFile = Path.Combine(OutputFolder, string.Format(
				"{0}{1}.{2}",
				Path.GetFileNameWithoutExtension(video.Path),
				(height != 0 ? "_" + height.ToString() + "p" : ""),
				format.Extension));

			if (pass != 0)
				parameters.Add("passlogfile", formatName + "_" + height.ToString());

			ConvertProcess process = new ConvertProcess(parameters, false);
			process.ProcessName = string.Format("{0} {1}p{2}", formatName, height, (pass != 0 ? " pass " + pass.ToString() : string.Empty));
			process.TempFolder = TempFolder;
			process.Pass = pass;

			if (parentProcess != null)
				process.ParentProcess = parentProcess;

			convertProcesses.Add(process);

			return process;
		}

		public void StopAll() 
		{
			convertProcesses.StopAll();
		}

		void convertProcesses_ProgressChanged(object sender, EventArg<double> e)
		{
			double percent = e.Data / video.FrameCount;
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
				runNext();
			}
		}

		void process_DoneUpdated(ConvertProcess sender, DataReceivedEventArgs e)
		{
			double avg = processes.Select(p => p.Done).Average();
			ProgressChanged(this,	new EventArg<double>(avg));
		}

		void process_ConvertExited(ConvertProcess sender, EventArg<bool> e)
		{
			--currentThreads;

			runNext();

			if (currentThreads == 0)
			{
				ConvertProcess.ProcessStatus status = ConvertProcess.ProcessStatus.Finished;
				foreach (ConvertProcess process in processes)
				{
					if (process.Status == ConvertProcess.ProcessStatus.Stopped)
					{
						status = process.Status;
						break;
					}
					else if (process.Status == ConvertProcess.ProcessStatus.Failed)
					{
						status = process.Status;
						break;
					}
				}
				processes.Clear();

				AllFinished(this, new EventArg<ConvertProcess.ProcessStatus>(status));
			}
		}

		private void runNext() 
		{
			foreach (ConvertProcess process in processes)
			{
				if (process.Status == ConvertProcess.ProcessStatus.Waiting)
				{
					// depend process
					if (process.ParentProcess != null)
					{
						ConvertProcess p = process.ParentProcess;
						if (p.Status == ConvertProcess.ProcessStatus.Failed)
						{
							process.Status = ConvertProcess.ProcessStatus.Failed;
							continue;
						}
						else if (p.Status != ConvertProcess.ProcessStatus.Finished)
							continue;
					}

					process.Run();
					++currentThreads;
					break;
				}
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
		public ConvertProcess ParentProcess;
		public string TempFolder;
		public int Pass;
		public int Done;
		public enum ProcessStatus { Waiting, Running, Finished, Failed, Stopped }
		public ProcessStatus Status;

		public event DoneUpdatedEventHandler DoneUpdated;
		public event ConvertExitedEventHandler ConvertExited;

		private Process proc;
		private ParamsBuilder parameters;
		private bool outputAtEnd;
		private bool converSucesfullyEnded = false;

		public ConvertProcess(ParamsBuilder parameters, bool outputAtEnd = true)
		{
			App.Log.Add(parameters.ToString());
			this.parameters = parameters;
			this.outputAtEnd = outputAtEnd;
			Status = ProcessStatus.Waiting;
		}

		public string Run()
		{
			string output = string.Empty;

			ProcessStartInfo startInfo = new ProcessStartInfo(App.FfmpegLocation, parameters.ToString());
			startInfo.RedirectStandardError = true;
			startInfo.UseShellExecute = false;
			startInfo.CreateNoWindow = true;

			if (Pass != 0)
			{
				if (!Directory.Exists(TempFolder))
				{
					Directory.CreateDirectory(TempFolder);
				}

				startInfo.WorkingDirectory = TempFolder;
			}

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
			Status = ProcessStatus.Stopped;
			if (!proc.HasExited)
			{
				proc.Kill();
				App.Log.Add(string.Format("{0}: Process byl zastaven", ProcessName));
			}
		}

		void proc_Exited(object sender, EventArgs e)
		{
			bool success = false;

			if(Status != ProcessStatus.Stopped)
			{
				App.Log.Add(string.Format("{0}: Process převodu skončil", ProcessName));

				if (!converSucesfullyEnded)
				{
					System.Threading.Thread.Sleep(100);
				}

				if (converSucesfullyEnded)
					Status = ProcessStatus.Finished;
				else
					Status = ProcessStatus.Failed;
			}

			if (Status == ProcessStatus.Failed)
			{
				App.Log.Add(string.Format("{0}: Při převodu nastala chyba, výstupní soubor bude smazán", ProcessName));
				deleteProcessingFile();
			}
			else if (Status == ProcessStatus.Stopped)
			{
				App.Log.Add(string.Format("{0}: Převod byl zastaven, výstupní soubor bude smazán", ProcessName));
				deleteProcessingFile();
			}
			else if(Status == ProcessStatus.Finished)
			{
				if (Pass == 1)
					App.Log.Add(string.Format("{0}: První průchod byl úspěšně dokončen", ProcessName));
				else if (Pass == 2)
					App.Log.Add(string.Format("{0}: Druhý průchod byl úspěšně dokončen", ProcessName));
				else
					App.Log.Add(string.Format("{0}: Převod byl úspěšně dokončen", ProcessName));
			}

			ConvertExited(this, new EventArg<bool>(success));
		}

		static Regex frameCountRegex = new Regex(@"frame=[ ]*(\d*)", RegexOptions.Compiled);

		private void proc_ErrorDataReceived(object sender, DataReceivedEventArgs e)
		{
			if (e.Data == null)
				return;

			App.Log.Add(ProcessName + ": " + e.Data);

			if (e.Data.IndexOf("video:") != -1)
			{
				converSucesfullyEnded = true;
				return;
			}

			Match m = frameCountRegex.Match(e.Data);
			if (m.Success)
			{
				Done = int.Parse(m.Groups[1].Value);
				DoneUpdated(this, e);
			}
		}

		private void deleteProcessingFile()
		{
			if (File.Exists(parameters.OutputFile))
			{
				File.Delete(parameters.OutputFile);
			}
		}
	}
}
