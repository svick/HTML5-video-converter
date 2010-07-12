using System;
using System.Collections.Generic;
using System.Text;

namespace Video_converter
{
	public class ParamsBuilder 
	{
		private Dictionary<string, string> parameters = new Dictionary<string, string>();

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

		public override string ToString()
		{
			StringBuilder builder = new StringBuilder();
			foreach (KeyValuePair<string, string> par in parameters)
			{
				if(par.Value == string.Empty)
					builder.AppendFormat("-{0} ", par.Key);
				else
					builder.AppendFormat("-{0} {1} ", par.Key, par.Value);
			}

			return builder.ToString().Trim();
		}
	}

	public abstract class Format
	{
		public abstract string Extension { get; }

		protected abstract void formatParams(BitRate bitRate);

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
				throw new Exception(string.Format("Neznámý formát {0}.", name));
			}
		}

		public string BuildParams(Video video, int height)
		{
			this.video = video;
			parameters = new ParamsBuilder();

			parameters.Add("threads", Environment.ProcessorCount);

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

			formatParams(bitRate);

			return parameters.ToString();
		}

		private Size resize(int height)
		{
			int width;

			// 16:9 and higger
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

			return new Size { Height = height, Width = width };
		}

		private BitRate computeBitRate(Size size)
		{
			BitRate bitRate = new BitRate();

			if (size.Height >= 1080 || size.Width >= 1920)
			{
				bitRate.Audio = 320;
				bitRate.Video = 4000;
			}
			else if (size.Height >= 720 || size.Width >= 1280)
			{
				bitRate.Audio = 256;
				bitRate.Video = 2000;
			}
			else
			{
				bitRate.Audio = 256;
				bitRate.Video = 1000;
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

		protected override void formatParams(BitRate bitRate)
		{
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

		protected override void formatParams(BitRate bitRate)
		{
			parameters.Add("f", "mp4");

			if (video.Format == "h264" && !parameters.Contains("s") && video.BitRate.Video != 0 && video.BitRate.Video < bitRate.Video)
			{
				parameters.Add("vcodec", "copy");
			}
			else
			{
				parameters.Add("vcodec", "libx264");
				parameters.Add("vpre", "default");
				parameters.Add("b", bitRate.Video.ToString() + "k");
			}

			if (video.AudioFormat != "aac" || (video.BitRate.Audio != 0 && video.BitRate.Audio > bitRate.Audio))
			{
				parameters.Add("strict", "experimental");
				parameters.Add("acodec", "aac");
				parameters.Add("ab", bitRate.Audio.ToString() + "k");
			}
			else
			{
				parameters.Add("acodec", "copy");
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

		protected override void formatParams(BitRate bitRate)
		{
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
		}
	}
}