using System;

namespace Video_converter
{
	public class FormatProperties
	{
		public string Extension { get; protected set; }

		public extern string BuildParams(Video video, string targetResolution);
	}

	public class WebMFormat : FormatProperties
	{
		public WebMFormat() 
		{
			Extension = "webm";
		}

		public string BuildParams(Video video, string targetResolution)
		{
			string parameters = string.Format("-threads 4 -f webm -vcodec libvpx -acodec libvorbis -ab {1} -b {2}", "320k", "1000k");
			return parameters;
		}
	}

	public class H264Format : FormatProperties
	{
		public H264Format()
		{
			Extension = "mp4";
		}

		public string BuildParams(Video video, string targetResolution)
		{
			string parameters = string.Format("-threads 4 -f webm -vcodec libvpx -acodec libvorbis -ab {1} -b {2}", "320k", "1000k");
			return parameters;
		}
	}

	public class TheoraFormat : FormatProperties
	{
		public TheoraFormat()
		{
			Extension = "ogv";
		}

		public string BuildParams(Video video, string targetResolution)
		{
			string parameters = string.Format("-threads 4 -f ogg -vcodec libtheora -acodec libvorbis -ab {1} -b {2}", "320k", "1000k");
			return parameters;
		}
	}



}
