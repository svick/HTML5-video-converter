using System;
using System.Diagnostics;
using System.IO;
using Video_converter.Properties;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Video_converter
{
	//from http://jasonjano.wordpress.com/2010/02/09/a-simple-c-wrapper-for-ffmpeg/

	public class Converter
	{
		#region Properties
		private string _ffExe;
		public string ffExe
		{
			get
			{
				return _ffExe;
			}
			set
			{
				_ffExe = value;
			}
		}

		private string _WorkingPath;
		public string WorkingPath
		{
			get
			{
				return _WorkingPath;
			}
			set
			{
				_WorkingPath = value;
			}
		}

		string applicationDirectory;

		#endregion

		#region Constructors
		public Converter()
		{
			Initialize();
		}
		public Converter(string ffmpegExePath)
		{
			_ffExe = ffmpegExePath;
			Initialize();
		}
		#endregion

		#region Initialization
		private void Initialize()
		{
			//first make sure we have a value for the ffexe file setting
			if (string.IsNullOrEmpty(_ffExe))
			{
				string o = Settings.Default.ExeLocation;
				if (string.IsNullOrEmpty(o))
				{
					throw new Exception("Could not find the location of the ffmpeg exe file.  The path for ffmpeg.exe " +
					"can be passed in via a constructor of the ffmpeg class (this class) or by setting in the app.config or web.config file.  " +
					"in the appsettings section, the correct property name is: ffmpeg:ExeLocation");
				}
				else
				{
					_ffExe = o;
				}
			}

			//Now see if ffmpeg.exe exists
			string workingpath = GetWorkingFile();
			if (string.IsNullOrEmpty(workingpath))
			{
				//ffmpeg doesn't exist at the location stated.
				throw new Exception("Could not find a copy of ffmpeg.exe");
			}
			_ffExe = workingpath;

			//now see if we have a temporary place to work
			if (string.IsNullOrEmpty(_WorkingPath))
			{
				string o = Settings.Default.WorkingPath;
				if (o != null)
				{
					_WorkingPath = o.ToString();
				}
				else
				{
					_WorkingPath = string.Empty;
				}
			}

			applicationDirectory = Directory.GetCurrentDirectory();
		}

		private string GetWorkingFile()
		{
			//try the stated directory
			if (File.Exists(_ffExe))
			{
				return _ffExe;
			}

			//oops, that didn't work, try the base directory
			if (File.Exists(Path.GetFileName(_ffExe)))
			{
				return Path.GetFileName(_ffExe);
			}

			//well, now we are really unlucky, let's just return null
			return null;
		}
		#endregion



		#region Run the process
		private string RunProcess(string parameters)
		{
			//create a process info
			ProcessStartInfo oInfo = new ProcessStartInfo(this._ffExe, parameters);
			oInfo.UseShellExecute = false;
			oInfo.CreateNoWindow = true;
			oInfo.RedirectStandardOutput = true;
			oInfo.RedirectStandardError = true;

			string datadir = Path.GetDirectoryName(_ffExe);
			if (!Path.IsPathRooted(datadir))
				datadir = Path.Combine(applicationDirectory, datadir);

			oInfo.EnvironmentVariables["FFMPEG_DATADIR"] = datadir;

			//Create the output and streamreader to get the output
			string output = null; StreamReader srOutput = null;

			//try the process
			try
			{
				//run the process
				Process proc = System.Diagnostics.Process.Start(oInfo);

				proc.WaitForExit();

				//get the output
				srOutput = proc.StandardError;

				//now put it in a string
				output = srOutput.ReadToEnd();

				proc.Close();
			}
			catch (Exception)
			{
				output = string.Empty;
			}
			finally
			{
				//now, if we succeded, close out the streamreader
				if (srOutput != null)
				{
					srOutput.Close();
					srOutput.Dispose();
				}
			}
			return output;
		}
		#endregion

		#region GetVideoInfo
		public VideoFile GetVideoInfo(string inputPath)
		{
			VideoFile vf = null;
			vf = new VideoFile(inputPath);
			GetVideoInfo(vf);
			return vf;
		}
		public void GetVideoInfo(VideoFile input)
		{
			//set up the parameters for video info
			string parameters = string.Format("-i \"{0}\"", input.Path);
			string output = RunProcess(parameters);
			input.RawInfo = output;

			//get duration
			Regex re = new Regex("[D|d]uration:.((\\d|:|\\.)*)");
			Match m = re.Match(input.RawInfo);

			if (m.Success)
			{
				string duration = m.Groups[1].Value;
				string[] timepieces = duration.Split(new char[] { ':', '.' });
				if (timepieces.Length == 4)
				{
					input.Duration = new TimeSpan(0, Convert.ToInt16(timepieces[0]), Convert.ToInt16(timepieces[1]), Convert.ToInt16(timepieces[2]), Convert.ToInt16(timepieces[3]));
				}
			}

			//get audio bit rate
			re = new Regex("[B|b]itrate:.((\\d|:)*)");
			m = re.Match(input.RawInfo);
			double kb = 0.0;
			if (m.Success)
			{
				Double.TryParse(m.Groups[1].Value, out kb);
			}
			input.BitRate = kb;

			//get the audio format
			re = new Regex("[A|a]udio:.*");
			m = re.Match(input.RawInfo);
			if (m.Success)
			{
				input.AudioFormat = m.Value;
			}

			//get the video format
			re = new Regex("[V|v]ideo:.*");
			m = re.Match(input.RawInfo);
			if (m.Success)
			{
				input.VideoFormat = m.Value;
			}

			//get the video format
			re = new Regex("(\\d{2,4})x(\\d{2,4})");
			m = re.Match(input.RawInfo);
			if (m.Success)
			{
				int width = 0; int height = 0;
				int.TryParse(m.Groups[1].Value, out width);
				int.TryParse(m.Groups[2].Value, out height);
				input.Width = width;
				input.Height = height;
			}
			input.infoGathered = true;
		}
		#endregion

		#region Convert
		public void ConvertFormat(string inputPath, string format, int height)
		{
			ConvertFormat(inputPath, format, height, true);
		}

		public void ConvertFormat(string inputPath, string format, int height, bool addHeightToFileName)
		{
			VideoFile vf = GetVideoInfo(inputPath);

			FormatInfo fi = FormatInfo.FormatInfos[format];
			string filename = Path.Combine(
				Path.GetDirectoryName(inputPath),
				Path.GetFileNameWithoutExtension(inputPath) + (addHeightToFileName ? height.ToString() : "") + "." + fi.Extension);
			int width = vf.Width * height / vf.Height;
			string parameters = string.Format("-i \"{0}\" -y -s {3}x{4} -vpre libx264-normal -f {2} \"{1}\"", inputPath, filename, fi.Parameter, width, height);
			string output = RunProcess(parameters);
			System.Windows.MessageBox.Show(output);
		}
		#endregion
	}

	public class VideoFile
	{
		#region Properties
		private string _Path;
		public string Path
		{
			get
			{
				return _Path;
			}
			set
			{
				_Path = value;
			}
		}

		public TimeSpan Duration { get; set; }
		public double BitRate { get; set; }
		public string AudioFormat { get; set; }
		public string VideoFormat { get; set; }
		public int Height { get; set; }
		public int Width { get; set; }
		public string RawInfo { get; set; }
		public bool infoGathered { get; set; }
		#endregion

		#region Constructors
		public VideoFile(string path)
		{
			_Path = path;
			Initialize();
		}
		#endregion

		#region Initialization
		private void Initialize()
		{
			this.infoGathered = false;
			//first make sure we have a value for the video file setting
			if (string.IsNullOrEmpty(_Path))
			{
				throw new Exception("Could not find the location of the video file");
			}

			//Now see if the video file exists
			if (!File.Exists(_Path))
			{
				throw new Exception("The video file " + _Path + " does not exist.");
			}
		}
		#endregion
	}

	class FormatInfo
	{
		static Dictionary<string, FormatInfo> formatInfos = new Dictionary<string, FormatInfo>
		{
			{ "flv", new FormatInfo("flv", "flv") },
			{ "theora", new FormatInfo("ogv", "ogg") },
			{ "webm", new FormatInfo("webm", "webm") },
			{ "h264", new FormatInfo("mp4", "h264") }
		};
		public static Dictionary<string, FormatInfo> FormatInfos
		{
			get
			{
				return formatInfos;
			}
		}

		public string Extension
		{
			get;
			protected set;
		}
		public string Parameter
		{
			get;
			protected set;
		}

		public FormatInfo(string extension, string parameter)
		{
			Extension = extension;
			Parameter = parameter;
		}
	}
}