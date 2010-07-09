using System;

namespace Video_converter
{
	public abstract class FormatProperties
	{
		public string Extension { get; protected set; }

		public abstract string BuildParams(Video video, string targetResolution);
	}

	public class WebMFormat : FormatProperties
	{
		public WebMFormat() 
		{
			Extension = "webm";
		}

		public override string BuildParams(Video video, string targetResolution)
		{
			string parameters = string.Format("-threads 4 -f webm -vcodec libvpx -acodec libvorbis -ab {0} -b {1}", "320k", "1000k");
			return parameters;
		}
	}

	public class H264Format : FormatProperties
	{
		public H264Format()
		{
			Extension = "mp4";
		}

		public override string BuildParams(Video video, string targetResolution)
		{
			string parameters = string.Format("-threads 4 -f mp4 -vcodec libx264 -acodec aac -strict experimental -vpre normal -ab {0} -b {1}", "320k", "1000k");
			return parameters;
		}
	}

	public class TheoraFormat : FormatProperties
	{
		public TheoraFormat()
		{
			Extension = "ogv";
		}

		public override string BuildParams(Video video, string targetResolution)
		{
			string parameters = string.Format("-threads 4 -f ogg -vcodec libtheora -acodec libvorbis -ab {0} -b {1}", "320k", "1000k");
			return parameters;
		}
	}



}
