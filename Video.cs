using System;
using System.IO;
using Video_converter.Properties;

namespace Video_converter
{
	public class Size
	{
		public int Width { get; set; }
		public int Height { get; set; }

		public string ToString(string separator = "x")
		{
			return string.Format("{0}{1}{2}", Width, separator, Height);
		}
	}

	public class BitRate
	{
		public int Audio { get; set; }
		public int Video { get; set; }
	}

	public class Video
	{
		public string Path { get; private set; }
		public TimeSpan Duration { get; set; }
		public int FrameCount { get; set; }
		public string Format { get; set; }
		public string AudioFormat { get; set; }
		public BitRate BitRate = new BitRate();
		public Size Size = new Size();

		public Video(string path)
		{
			this.Path = path;
		}

		public bool Exist()
		{
			return File.Exists(Path);
		}

		public Size NewSize(int height)
		{
			int width;

			// 16:9 and higher
			if (((double)Size.Width / Size.Height) > ((double)16 / 9))
			{
				width = (int)Math.Ceiling((double)height * 16 / 9);

				if (width > Size.Width)
					width = Size.Width;

				height = (int)Math.Ceiling((double)Size.Height * width / Size.Width);
			}
			else
			{
				if (height > Size.Height)
					height = Size.Height;

				width = (int)Math.Ceiling((double)Size.Width * height / Size.Height);
			}

			// Height and width must be divisible by two
			if (height % 2 == 1)
				height--;

			if (width % 2 == 1)
				width--;

			return new Size { Height = height, Width = width };
		}

		public BitRate ComputeNewBitRate(Size size)
		{
			BitRate bitRate = new BitRate();

			bitRate.Video = (int)(size.Width * size.Height * Settings.Default.bitRateLinear + Settings.Default.bitRateOffset);

			if (size.Height >= 1080 || size.Width >= 1920)
			{
				bitRate.Audio = 320;
			}
			else
			{
				bitRate.Audio = 256;
			}

			if (BitRate.Video != 0 && bitRate.Video > BitRate.Video)
				bitRate.Video = BitRate.Video;

			if (BitRate.Audio != 0 && bitRate.Audio > BitRate.Audio)
				bitRate.Audio = BitRate.Audio;

			return bitRate;
		}
	}
}
