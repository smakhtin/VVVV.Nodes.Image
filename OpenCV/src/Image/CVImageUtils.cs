﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Emgu.CV.CvEnum;
using Emgu.CV;
using Emgu.CV.Structure;
using SlimDX.Direct3D9;
using System.Drawing;
using System.Runtime.InteropServices;

namespace VVVV.Nodes.OpenCV
{
	class CVImageUtils
	{
		[DllImport("kernel32.dll", EntryPoint = "RtlMoveMemory")]
		public static extern void CopyMemory(IntPtr Destination, IntPtr Source, uint Length);

		public static COLOR_CONVERSION ConvertRoute(TColorFormat src, TColorFormat dst)
		{
			switch (src)
			{
				case TColorFormat.L8:
					{
						switch (dst)
						{
							case TColorFormat.RGBA8:
								return COLOR_CONVERSION.CV_GRAY2RGBA;
						}
						break;
					}

				case TColorFormat.RGB8:
					{
						switch (dst)
						{
							case TColorFormat.L8:
								return COLOR_CONVERSION.CV_RGB2GRAY;

							case TColorFormat.RGBA8:
								return COLOR_CONVERSION.CV_RGB2RGBA;
						}
						break;
					}
			}

			return COLOR_CONVERSION.CV_COLORCVT_MAX;
		}

		public static IImage CreateImage(int width, int height, TColorFormat format)
		{
			switch(format)
			{
				case TColorFormat.L8:
					return new Image<Gray, byte>(width, height);
				case TColorFormat.L16:
					return new Image<Gray, ushort>(width, height);


				case TColorFormat.RGB8:
					return new Image<Rgb, byte>(width, height);
				case TColorFormat.RGB32F:
					return new Image<Rgb, float>(width, height);

				case TColorFormat.RGBA8:
					return new Image<Rgba, byte>(width, height);
			}

			throw (new NotImplementedException("We have not implemented the automatic creation of this image type"));
		}

		public static TColorFormat GetFormat(IImage image)
		{
			Image<Gray, byte> ImageL8 = image as Image<Gray, byte>;
			if (ImageL8 != null)
				return TColorFormat.L8;

			Image<Gray, ushort> ImageL16 = image as Image<Gray, ushort>;
			if (ImageL16 != null)
				return TColorFormat.L16;
			
			Image<Rgb, byte> ImageRGB8 = image as Image<Rgb, byte>;
			if (ImageRGB8 != null)
				return TColorFormat.RGB8;
			//camera captures seem to arrive as bgr even though rgb
			//may need to revisit this later on
			Image<Bgr, byte> ImageBGR8 = image as Image<Bgr, byte>;
			if (ImageBGR8 != null)
				return TColorFormat.RGB8;

			Image<Rgb, float> ImageRGB32F = image as Image<Rgb, float>;
			if (ImageRGB32F != null)
				return TColorFormat.RGB32F;

			Image<Rgba, byte> ImageRGBA8 = image as Image<Rgba, byte>;
			if (ImageRGBA8 != null)
				return TColorFormat.RGBA8;

			return TColorFormat.UnInitialised;
		}

		public static uint BytesPerPixel(TColorFormat format)
		{
			switch (format)
			{
				case TColorFormat.L8:
					return 1;
				case TColorFormat.L16:
					return 2;

				case TColorFormat.RGB8:
					return 3;

				case TColorFormat.RGB32F:
					return 3 * sizeof(float);

				case TColorFormat.RGBA8:
					return 4;

				default:
					throw(new NotImplementedException("We haven't implemented BytesPerPixel for this type"));
			}
		}


		public static Format GetDXFormat(TColorFormat format)
		{
			switch (format)
			{
				case TColorFormat.L8:
					return Format.L8;
				case TColorFormat.L16:
					return Format.L16;

				case TColorFormat.RGBA8:
					return Format.A8R8G8B8;

				default:
					throw (new NotImplementedException("Cannot create a texture to match Image's format"));
			}
		}

		public static string AsString(TColorFormat format)
		{
			switch (format)
			{
				case TColorFormat.L8:
					return "L8";
				case TColorFormat.L16:
					return "L16";

				case TColorFormat.RGB8:
					return "RGB8";

				case TColorFormat.RGB32F:
					return "RGB32F";

				case TColorFormat.RGBA8:
					return "RGBA8";

				default:
					throw (new NotImplementedException("We haven't implemented AsString for this type"));
			}
		}

		public static Texture CreateTexture(CVImageAttributes attributes, Device device)
		{
			TColorFormat format = attributes.ColourFormat;
			TColorFormat newFormat;
			bool useConverted = NeedsConversion(format, out newFormat);

			return new Texture(device, Math.Max(attributes.Width, 1), Math.Max(attributes.Height, 1), 1, Usage.None, GetDXFormat(useConverted ? newFormat : format), Pool.Managed);
		}

		public static bool NeedsConversion(TColorFormat format, out TColorFormat targetFormat)
		{
			switch(format)
			{
				case TColorFormat.RGB8:
					targetFormat = TColorFormat.RGBA8;
					return true;

				default:
					targetFormat = TColorFormat.UnInitialised;
					return false;
			}
		}

		public static void CopyImage(CVImage source, CVImage target)
		{
			if (source.Size != target.Size)
				throw (new Exception("Can't copy between these 2 images, they differ in dimensions"));

			if (source.NativeFormat != target.NativeFormat)
				throw (new Exception("Can't copy between these 2 images, they differ in pixel colour format"));

			CopyImage(source.CvMat, target.CvMat, target.ImageAttributes.BytesPerFrame);
		}

		public static void CopyImage(IImage source, CVImage target)
		{
			if (source.Size != target.Size)
				throw (new Exception("Can't copy between these 2 images, they differ in dimensions"));

			if (GetFormat(source) != target.NativeFormat)
				throw (new Exception("Can't copy between these 2 images, they differ in pixel colour format"));

			CopyImage(source.Ptr, target.CvMat, target.ImageAttributes.BytesPerFrame);
		}

		/// <summary>
		/// Copys by hand raw image data from source to target
		/// </summary>
		/// <param name="source">CvArray object</param>
		/// <param name="target">CvArray object</param>
		/// <param name="size">Size in bytes</param>
		public static void CopyImage(IntPtr source, IntPtr target, uint size)
		{
			IntPtr sourceRaw;
			IntPtr targetRaw;

			int step;
			Size dims;

			CvInvoke.cvGetRawData(source, out sourceRaw, out step, out dims);
			CvInvoke.cvGetRawData(target, out targetRaw, out step, out dims);

			CopyMemory(targetRaw, sourceRaw, size);
		}

		public static void CopyImageConverted(CVImage source, CVImage target)
		{
			COLOR_CONVERSION route = ConvertRoute(source.NativeFormat, target.NativeFormat);

			if (route==COLOR_CONVERSION.CV_COLORCVT_MAX)
				throw(new Exception("Unsupported conversion"));

			try
			{
				CvInvoke.cvCvtColor(source.CvMat, target.CvMat, route);
			}
			catch
			{
				//CV likes to throw here sometimes, but the next frame it's fine
			}
		}

		public static bool IsIntialised(IImage image)
		{
			if (image == null)
				return false;

			if (image.Size.Width==0 || image.Size.Height==0)
				return false;

			return true;
		}
	}
}
