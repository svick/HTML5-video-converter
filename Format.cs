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

		public ParamsBuilder DefaultParams(Video video, int height, ParamsBuilder parameters) 
		{
			parameters.Add("threads", Environment.ProcessorCount.ToString());

			if (height != 0)
			{
				Size size = Resize(video, height);

				if (size.Height != video.Size.Height)
				{
					parameters.Add("s", string.Format("{0}x{1}", size.Width.ToString(), size.Height.ToString()));
				}
			}

			return parameters;
		}

		public BitRate ComputeBitRate(Video video, int height)
		{
			int audioBitRate, videoBitRate;

			if (height >= 1080)
			{
				audioBitRate = 320;
				videoBitRate = 4000;
			}
			else if (height >= 720)
			{
				audioBitRate = 256;
				videoBitRate = 2000;
			}
			else
			{
				audioBitRate = 256;
				videoBitRate = 1000;
			}

			if (video.BitRate.Video != 0 && videoBitRate > video.BitRate.Video)
				videoBitRate = video.BitRate.Video;

			if (video.BitRate.Audio != 0 && audioBitRate > video.BitRate.Audio)
				audioBitRate = video.BitRate.Audio;

			return new BitRate { Audio = audioBitRate, Video = videoBitRate };
		}

		public Size Resize(Video video, int height)
		{
			int width;

			// 16:9 and higger
			if (( (double) video.Size.Width / video.Size.Height) > ( (double) 16 / 9))
			{
				width = (int)Math.Ceiling((double)height * 16 / 9);

				if (width > video.Size.Width)
					width = video.Size.Width;

				height = video.Size.Height * width / video.Size.Width;
			}
			else
			{
				if (height > video.Size.Height)
					height = video.Size.Height;

				width = (int)Math.Ceiling((double)video.Size.Width * height / video.Size.Height);
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
			ParamsBuilder parameters = DefaultParams(video, height, new ParamsBuilder());

			BitRate bitRate = ComputeBitRate(video, height);
			
			parameters.Add("f", "webm");

			if (video.Format != "vp8")
			{
				parameters.Add("vcodec", "libvpx");
				parameters.Add("b", bitRate.Video.ToString() + "k");
			}
			else
			{
				parameters.Add("vcodec", "copy");
			}

			if (video.AudioFormat != "vorbis")
			{
				parameters.Add("acodec", "libvorbis");
				parameters.Add("ab", bitRate.Audio.ToString() + "k");
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
			ParamsBuilder parameters = DefaultParams(video, height, new ParamsBuilder());

			BitRate bitRate = ComputeBitRate(video, height);

			parameters.Add("f", "mp4");

			if (video.Format != "theora")
			{
				parameters.Add("vcodec", "libx264");
				parameters.Add("vpre", "normal");
				parameters.Add("b", bitRate.Video.ToString() + "k");
			}
			else
			{
				parameters.Add("vcodec", "copy");
			}

			if (video.AudioFormat != "aac")
			{
				parameters.Add("strict", "experimental");
				parameters.Add("acodec", "aac");
				parameters.Add("ab", bitRate.Audio.ToString() + "k");
			}
			else
			{
				parameters.Add("acodec", "copy");
			}

			return parameters.ToString();
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
			ParamsBuilder parameters = DefaultParams(video, height, new ParamsBuilder());

			BitRate bitRate = ComputeBitRate(video, height);

			parameters.Add("f", "ogg");

			if (video.Format != "theora")
			{
				parameters.Add("vcodec", "libtheora");
				parameters.Add("b", bitRate.Video.ToString() + "k");
			}
			else
			{
				parameters.Add("vcodec", "copy");
			}

			if (video.AudioFormat != "vorbis")
			{
				parameters.Add("acodec", "libvorbis");
				parameters.Add("ab", bitRate.Audio.ToString() + "k");
			}
			else
			{
				parameters.Add("acodec", "copy");
			}

			return parameters.ToString();
		}
	}
}