using System;

namespace Video_converter
{
	public abstract class FormatProperties
	{
		public abstract string Extension { get; }

		public abstract string BuildParams(Video video, string targetResolution);
	}

	public class WebMFormat : FormatProperties
	{
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

	public class H264Format : FormatProperties
	{
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

	public class TheoraFormat : FormatProperties
	{
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