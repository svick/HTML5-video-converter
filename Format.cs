using System;
using System.Text;
using System.Collections.Generic;


namespace Video_converter
{
	public class Size
	{
		public int Width { get; set; }
		public int Height { get; set; }
	}

	public class ParamsBuilder 
	{
		private Dictionary<string, string> parameters = new Dictionary<string, string>();

		public void Add(string key, string value = "") 
		{
			parameters.Add(key, value);
		}

		public override string ToString()
		{
			StringBuilder builder = new StringBuilder();
			foreach (KeyValuePair<string, string> par in parameters)
			{
				if(par.Value == "")
					builder.AppendFormat("-{0} ", par.Key);
				else
					builder.AppendFormat("-{0} {1} ", par.Key, par.Value);
			}

			return builder.ToString();
		}
	}

	public abstract class Format
	{
		public abstract string Extension { get; }

		public abstract string BuildParams(Video video, int height);

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
				throw new Exception(string.Format("Neznámý formát {0}.", name));
			}
		}

		public Size Resize(Video video, int height)
		{
			int width;

			// 16:9 and higger
			if (( (double) video.Width / video.Height) > ( (double) 16 / 9))
			{
				width = height * 16 / 9;

				if (width > video.Width)
					width = video.Width;

				height = video.Height * width / video.Width;
			}
			else
			{
				if (height > video.Height)
					height = video.Height;

				width = video.Width * height / video.Height;
			}
			
			return new Size { Height = height, Width = width };
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

		public override string BuildParams(Video video, int height = 0)
		{
			ParamsBuilder parameters = new ParamsBuilder();

			if (height != 0)
			{
				Size size = Resize(video, height);

				if (size.Height != video.Height)
				{
					parameters.Add("s", string.Format("{0}x{1}", size.Width.ToString(), size.Height.ToString()));
				}
			}
			
			parameters.Add("threads", "4");
			parameters.Add("f", "webm");

			if (video.Format != "vp8")
			{
				parameters.Add("vcodec", "libvpx");
				parameters.Add("b", "1000k");
			}
			else
			{
				parameters.Add("vcodec", "copy");
			}

			if (video.AudioFormat != "vorbis")
			{
				parameters.Add("acodec", "libvorbis");
				parameters.Add("ab", "320k");
			}
			else
			{
				parameters.Add("acodec", "copy");
			}

			return parameters.ToString();
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

		public override string BuildParams(Video video, int height)
		{
			return string.Format("-threads 4 -f mp4 -vcodec libx264 -acodec aac -strict experimental -vpre normal -ab {0} -b {1}", "320k", "1000k");
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

		public override string BuildParams(Video video, int height)
		{
			return string.Format("-threads 4 -f ogg -vcodec libtheora -acodec libvorbis -ab {0} -b {1}", "320k", "1000k");
		}
	}
}