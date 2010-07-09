using System;
using System.Text;
using System.Collections.Generic;


namespace Video_converter
{
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

		public override string BuildParams(Video video, int height)
		{
			ParamsBuilder parameters = new ParamsBuilder();
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