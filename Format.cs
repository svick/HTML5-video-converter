using System;
using System.Collections.Generic;
using System.Text;

namespace Video_converter
{
	public class ParamsBuilder 
	{
		private Dictionary<string, string> parameters = new Dictionary<string, string>();
		public string OutputFile { get; set; }
		public string InputFile { get; set; }

		public void Add(string key, string value = "") 
		{
			parameters.Add(key, value);
		}

		public void Add(string key, int value)
		{
			parameters.Add(key, value.ToString());
		}

		public bool Contains(string key)
		{
			return parameters.ContainsKey(key);
		}

		public string Get(string key)
		{
			if (Contains(key))
				return parameters[key];
			else
				return null;
		}

		public override string ToString()
		{
			StringBuilder builder = new StringBuilder();

			if (InputFile != null)
				builder.AppendFormat("-i \"{0}\" ", InputFile);

			foreach (KeyValuePair<string, string> par in parameters)
			{
				if(par.Value == string.Empty)
					builder.AppendFormat("-{0} ", par.Key);
				else
					builder.AppendFormat("-{0} {1} ", par.Key, par.Value);
			}

			if (OutputFile == null || Get("pass") == "1")
				builder.Append("NUL");
			else
				builder.AppendFormat("\"{0}\"", OutputFile);
				

			return builder.ToString().Trim();
		}
	}

	public abstract class Format
	{
		public abstract string Extension { get; }

		protected abstract void formatParams(BitRate bitRate, int pass = 0);

		protected Video video;
		protected ParamsBuilder parameters;

		public static Format GetFormatByName(string name)
		{
			switch (name)
			{
			case "webm":
				return WebMFormat.Instance;
			case "h264":
				return H264Format.Instance;
			case "theora":
				return TheoraFormat.Instance;
			default:
				throw new Exception(App.GetLocalizedString("UnknownFormat", name));
			}
		}

		public ParamsBuilder BuildParams(Video video, int height, int pass = 0)
		{
			this.video = video;
			parameters = new ParamsBuilder();
#if DEBUG
			parameters.Add("loglevel", "debug");
#endif
			parameters.Add("threads", Environment.ProcessorCount);

			if (pass != 0)
			{
				parameters.Add("pass", pass);
			}

			Size newSize;
			if (height != 0)
			{
				 newSize = resize(height);

				if (newSize.Height != video.Size.Height)
				{
					parameters.Add("s", string.Format("{0}x{1}", newSize.Width, newSize.Height));
				}
			}
			else
			{
				newSize = video.Size;
			}

			BitRate bitRate = computeBitRate(newSize);

			formatParams(bitRate, pass);

			return parameters;
		}

		private Size resize(int height)
		{
			int width;

			// 16:9 and higher
			if (((double)video.Size.Width / video.Size.Height) > ((double)16 / 9))
			{
				width = (int)Math.Ceiling((double)height * 16 / 9);

				if (width > video.Size.Width)
					width = video.Size.Width;

				height = (int)Math.Ceiling((double)video.Size.Height * width / video.Size.Width);
			}
			else
			{
				if (height > video.Size.Height)
					height = video.Size.Height;

				width = (int)Math.Ceiling((double)video.Size.Width * height / video.Size.Height);
			}

			// Height and width must be divisible by two
			if (height % 2 == 1)
				height--;

			if (width % 2 == 1)
				width--;

			return new Size { Height = height, Width = width };
		}

		private BitRate computeBitRate(Size size)
		{
			BitRate bitRate = new BitRate();

			bitRate.Video = (int)(size.Width * size.Height * 0.002 + 300);

			if (size.Height >= 1080 || size.Width >= 1920)
			{
				bitRate.Audio = 320;
			}
			else
			{
				bitRate.Audio = 256;
			}

			if (video.BitRate.Video != 0 && bitRate.Video > video.BitRate.Video)
				bitRate.Video = video.BitRate.Video;

			if (video.BitRate.Audio != 0 && bitRate.Audio > video.BitRate.Audio)
				bitRate.Audio = video.BitRate.Audio;

			return bitRate;
		}
	}

	public class WebMFormat : Format
	{
		protected WebMFormat()
		{ }

		static WebMFormat instance = new WebMFormat();
		public static WebMFormat Instance
		{
			get
			{
				return instance;
			}
		}

		public override string Extension
		{
			get
			{
				return "webm";
			}
		}

		protected override void formatParams(BitRate bitRate, int pass = 0)
		{
			parameters.Add("f", "webm");

			parameters.Add("vcodec", "libvpx");
			parameters.Add("b", string.Format("{0}k", bitRate.Video));

			if (pass == 1)
			{
				parameters.Add("an");
			}
			else
			{
				parameters.Add("acodec", "libvorbis");
				parameters.Add("ab", string.Format("{0}k", bitRate.Audio));
			}
		}
	}

	public class H264Format : Format
	{
		protected H264Format()
		{ }

		static H264Format instance = new H264Format();
		public static H264Format Instance
		{
			get
			{
				return instance;
			}
		}

		public override string Extension
		{
			get
			{
				return "mp4";
			}
		}

		protected override void formatParams(BitRate bitRate, int pass = 0)
		{
			parameters.Add("f", "mp4");

			parameters.Add("vcodec", "libx264");
			parameters.Add("b", string.Format("{0}k", bitRate.Video));

			if (pass == 1)
			{
				parameters.Add("an");
				parameters.Add("vpre", "slow_firstpass");
			}
			else
			{
				parameters.Add("acodec", "aac");
				parameters.Add("strict", "experimental"); // acc acodec
				parameters.Add("ab", string.Format("{0}k", bitRate.Audio));
				parameters.Add("vpre", "slow");
			}
		}
	}

	public class TheoraFormat : Format
	{
		protected TheoraFormat()
		{ }

		static TheoraFormat instance = new TheoraFormat();
		public static TheoraFormat Instance
		{
			get
			{
				return instance;
			}
		}

		public override string Extension
		{
			get
			{
				return "ogv";
			}
		}

		protected override void formatParams(BitRate bitRate, int pass = 0)
		{
			parameters.Add("f", "ogg");

			parameters.Add("vcodec", "libtheora");
			parameters.Add("b", string.Format("{0}k", bitRate.Video));

			if (pass == 1)
			{
				parameters.Add("an");
			}
			else
			{
				parameters.Add("acodec", "libvorbis");
				parameters.Add("ab", string.Format("{0}k", bitRate.Audio));
			}
		}
	}
}