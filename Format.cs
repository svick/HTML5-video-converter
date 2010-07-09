using System;

namespace Video_converter
{
	public abstract class Format
	{
		public abstract string Extension { get; }

		public abstract string BuildParams(Video video, string targetResolution);

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

		public override string BuildParams(Video video, string targetResolution)
		{
			return string.Format("-threads 4 -f webm -vcodec libvpx -acodec libvorbis -ab {0} -b {1}", "320k", "1000k");
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

		public override string BuildParams(Video video, string targetResolution)
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

		public override string BuildParams(Video video, string targetResolution)
		{
			return string.Format("-threads 4 -f ogg -vcodec libtheora -acodec libvorbis -ab {0} -b {1}", "320k", "1000k");
		}
	}
}