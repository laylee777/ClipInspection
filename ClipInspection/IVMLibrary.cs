using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.Collections.Generic;

namespace IVMCommon
{
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct BLOBINFO
	{
		// 
		// Center pointer of object
		public MPoint ptObjCt;
		// Rect area of object
		public MRect rcObj;
		// Area of Object in pixel
		public int nObjArea;
		// average level of object area
		public int nObjLevel;
		//  it has the order after sorting.
		public int nOrder;
		// number of holes object has. 
		public int nHoleCount;
		// in case of hole, number of object which has the hole.
		public int nRoundObj;
		public double dAngle;
		public int k; // for k-curvature
		public double dCurvatureMin;
		public double dCurvatureMax;
		public int nPerimeter;
		// moments
		public int m00;
		public int m10;
		public int m01;
		public int m20;
		public int m11;
		public int m02;
	};

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct BLOBOPTION
	{
		// [in] select CIamgeBlob::BLOB_8CONNECT, CImageBlob::BLOB_4CONNECT, default is BLOB_8CONNECT
		public int nConnectivity;
		// put size of buffer, it returns count of blobs
		// [in, out]
		public int nObjBufSize;
		// [in, out]
		public int nHoleBufSize;
		// original image, put the dib pointer before binarize.
		// can be NULL
		// [MarshalAs(UnmanagedType.LPStruct)]
		public IntPtr pOrg; // 
		// labeling option;
		public uint uflags;
		// return labeled dib
		public IntPtr pLabeledDib;
		// [in]  the k value of k-curvature
		public int kcurve;
		public int process1PixelBlob;

	};

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct MSize
	{
		public int cx;
		public int cy;
		public Size Size
		{
			get
			{
				return new Size(cx, cy);
			}
		}
	}
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct MPoint
	{
		public int X;
		public int Y;
		public Point Point
		{
			get
			{
				return new Point(X, Y);
			}
		}
		public MPoint(int x, int y)
		{
			X = x;
			Y = y;
		}
		public MPoint(Point point)
		{
			X = point.X;
			Y = point.Y;
		}

	}
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct MRect
	{
		public int left;
		public int top;
		public int cx;
		public int cy;
		public int Width
		{
			get
			{
				return cx;
			}
		}
		public int Height
		{
			get
			{
				return cy;
			}
		}
		public MRect(int left, int top, int right, int bottom)
		{
			this.left = left;
			this.top = top;
			this.cx = right - left;
			this.cy = bottom - top;
		}
		public MRect(Rectangle rect)
		{
			left = rect.Left;
			top = rect.Top;
			cx = rect.Width;
			cy = rect.Height;
		}
		public Rectangle Rect
		{
			get
			{
				return new Rectangle(left, top, cx, cy);
			}
		}
		static public MRect FromRectangle(Rectangle rect)
		{
			return new MRect(rect);
		}
		public MPoint Center
		{
			get
			{
				return new MPoint(left + cx / 2, top + cy / 2);
			}
		}
	};
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public class MImage
	{
		public int intDepth;//depth in byte, 8bit->1, color(24bit)->3
		public int intBits;
		public int intWidth;
		public int intHeight;
		public int intAllocMem;
		public MRect rectRoi;
		public IntPtr ptrBuf;
		public IntPtr bitmapInfo;

		public MImage(int width, int height, int bits, IntPtr buf)
		{
			intWidth = width;
			intHeight = height;
			intBits = bits;
			intDepth = bits == 24 ? 3 : 1;
			ptrBuf = buf;
			rectRoi = new MRect(0, 0, width, height);
			intAllocMem = 0;
			ptrBuf = buf;
			bitmapInfo = IntPtr.Zero;
		}

		public void Draw(Graphics g, Rectangle dcRect, Rectangle imageRect)
		{
			MRect dcMRect = MRect.FromRectangle(dcRect);
			MRect imageMRect = MRect.FromRectangle(imageRect);
			IntPtr hDC = g.GetHdc();
			ImageLib.DrawMBitmap(this, hDC, ref dcMRect, ref imageMRect, IntPtr.Zero);
			g.ReleaseHdc();
		}

		public void Draw(Graphics g, Rectangle dcRect, Rectangle imageRect, byte[] palette)
		{
			MRect dcMRect = MRect.FromRectangle(dcRect);
			MRect imageMRect = MRect.FromRectangle(imageRect);
			IntPtr hDC = g.GetHdc();
			ImageLib.DrawMBitmap(this, hDC, ref dcMRect, ref imageMRect, palette);
			g.ReleaseHdc();
		}

		public void Draw(Graphics g, Rectangle dcRect, Rectangle imageRect, Color[] palette)
		{
			MRect dcMRect = MRect.FromRectangle(dcRect);
			MRect imageMRect = MRect.FromRectangle(imageRect);
			IntPtr hDC = g.GetHdc();

			ImageLib.DrawMBitmap(this, hDC, ref dcMRect, ref imageMRect, palette);
			g.ReleaseHdc();
		}

		public static void Save(MImage image, string filename)
		{
			ImageLib.SaveBitmap(image, filename);
		}
		public static void Save(Bitmap image, string filename)
		{
			BitmapData bd = image.LockBits(new Rectangle(new Point(0, 0), image.Size), ImageLockMode.ReadWrite, PixelFormat.Format8bppIndexed);
			MImage mimage = new MImage(image.Width, image.Height, 8, bd.Scan0);
			ImageLib.SaveBitmap(mimage, filename);
			image.UnlockBits(bd);
		}

		public Size Size
		{
			get { return new Size(intWidth, intHeight); }
		}

	};

	/// <summary>
	/// Image Library에 대한 요약 설명입니다.
	/// </summary>
	public class ImageLib
	{
		[DllImport("IVMLibrary.dll")]
		public static extern int Labeling(MImage dib, ref BLOBOPTION option, [MarshalAs(UnmanagedType.LPArray), Out]BLOBINFO[] pInfo);
		[DllImport("IVMLibrary.dll")]
		public static extern int BlobLabeling(IntPtr hBlob, MImage bitmap, ref BLOBOPTION option, [MarshalAs(UnmanagedType.LPArray), Out]BLOBINFO[] pInfo);
		[DllImport("IVMLibrary.dll")]
		public static extern int GetBoundary(MImage bitmap, IntPtr pLabel, ref BLOBOPTION option, BLOBINFO[] pInfo, int nIdxBlob, [MarshalAs(UnmanagedType.LPArray), Out]MPoint[] pt);
		[DllImport("IVMLibrary.dll")]
		public static extern int BlobFillLUTBlobs(IntPtr hBlob, MImage bitmap, byte[] fillLut);
		[DllImport("IVMLibrary.dll")]
		public static extern void BlobCreate(out IntPtr hBlob, MImage bitmap);
		[DllImport("IVMLibrary.dll")]
		public static extern void BlobClose(IntPtr hBlob);
		[DllImport("IVMLibrary.dll")]
		public static extern int BlobGetLargest(IntPtr hBlob);
		[DllImport("IVMLibrary.dll")]
		public static extern int BlobGetBlobNumber(IntPtr hBlob, MImage bitmap, MPoint point);
		[DllImport("IVMLibrary.dll")]
		public static extern void BlobGetVariance(IntPtr blob, BLOBINFO[] pInfo, int blobCount,
			out double averageX, out double averageY, out double varianceX, out double varianceY);
		[DllImport("IVMLibrary.dll")]
		public static extern void Thresholding(MImage dib, [In] int nLevel, MImage dst);
		[DllImport("IVMLibrary.dll")]
		public static extern void ThresholdingInverse(MImage dib, [In] int nLevel, MImage dst);
		[DllImport("IVMLibrary.dll", EntryPoint = "ImageArith_OffsetPlus")]
		public static extern void OffsetPlus(MImage dibSrc, MImage dibTgt, int nLevel);
		[DllImport("IVMLibrary.dll")]
		public static extern void DrawMBitmap(MImage image, IntPtr hDC, ref MRect dcRect, ref MRect imageRect, Color[] palette);
		[DllImport("IVMLibrary.dll")]
		public static extern void DrawMBitmap(MImage image, IntPtr hDC, ref MRect dcRect, ref MRect imageRect, byte[] palette);
		[DllImport("IVMLibrary.dll")]
		public static extern void DrawMBitmap(MImage image, IntPtr hDC, ref MRect dcRect, ref MRect imageRect, IntPtr palette);
		[DllImport("IVMLibrary.dll")]
		public static extern double Collelation(MImage pModel, MImage pTarget, MPoint point, int nXStep, int nYStep);
		[DllImport("IVMLibrary.dll")]
		public static extern bool SaveBitmap(MImage bitmap, string lpszFile);
		[DllImport("IVMLibrary.dll")]
		public static extern double AlignH(MImage pModel, MImage pTarget, MRect rcSearch, int nXStep, int nYStep,
			 int nXImageStep, int nYImageStep, out MPoint ptReturn);
		[DllImport("IVMLibrary.dll")]
		public static extern double Align2(MImage pModel, MImage pTarget, MRect rcSearch, int nXStep, int nYStep,
			 int nXImageStep, int nYImageStep, out MPoint ptReturn);
		[DllImport("IVMLibrary.dll")]
		public static extern void GetHistogram(MImage image, out int[] histogramBuffer);
		[DllImport("IVMLibrary.dll")]
		public static extern void AutoContrast(MImage image, MImage target);

		[DllImport("IVMLibrary.dll")]
		public static extern void RotateBi(MImage image, MImage target, double angle);

		[DllImport("IVMLibrary.dll", EntryPoint = "?Thining@MImageMorph@@SAXPAVMBitmap@@@Z")]
		public static extern void Thining(MImage image);

		[DllImport("IVMLibrary.dll")]
		public static extern void Erosion4(MImage image, MImage target);
		[DllImport("IVMLibrary.dll")]
		public static extern void Erosion8(MImage image, MImage target);
		[DllImport("IVMLibrary.dll")]
		public static extern void Dilation4(MImage image, MImage target);
		[DllImport("IVMLibrary.dll")]
		public static extern void Dilation8(MImage image, MImage target);

		[DllImport("IVMLibrary.dll")]
		public static extern void AddEllipticMask(MImage bitmap, ref Rectangle rect, int grayLevel);
		[DllImport("IVMLibrary.dll")]
		public static extern void AddRectMask(MImage bitmap, ref Rectangle rect, int grayLevel);

		// Align 관련
		[DllImport("IVMLibrary.dll")]
		public static extern void AlignCreate(out IntPtr handle, MImage bitmap);
		[DllImport("IVMLibrary.dll")]
		public static extern void AlignClose(IntPtr handle);
		[DllImport("IVMLibrary.dll")]
		public static extern int AlignFindTemplate(IntPtr handle,
					 MImage model,
					 MImage target,
					 MRect rcSearch,
					 int nXStep,
					 int nYStep,
					 int nXImageStep,
					 int nYImageStep,
					 double dMatchRate);
		[DllImport("IVMLibrary.dll")]
		public static extern int AlignGetFindNumber(IntPtr handle);
		[DllImport("IVMLibrary.dll")]
		public static extern int AlignGetMatchPoints(IntPtr handle, AlignMatch[] pMatchPoint, int Count);
	}

	public class Basic
	{
		public enum ConvertType { Average, RChannel, GChannel, BChannel }
		public static Point Align(Bitmap model, Bitmap target, Rectangle searchRect, out double matchLevel)
		{
			matchLevel = 0.0;
			try
			{
				MPoint findPoint;
				BitmapData modelBd = model.LockBits(new Rectangle(new Point(0, 0), model.Size), ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed);
				BitmapData targetBd = target.LockBits(new Rectangle(new Point(0, 0), target.Size), ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed);
				MImage mmodel = new MImage(model.Width, model.Height, 8, modelBd.Scan0);
				MImage mtarget = new MImage(target.Width, target.Height, 8, targetBd.Scan0);

				matchLevel = ImageLib.AlignH(mmodel, mtarget, new MRect(searchRect), 16, 16, 16, 16, out findPoint);

				model.UnlockBits(modelBd);
				target.UnlockBits(targetBd);
				return findPoint.Point;
			}
			catch (Exception e)
			{
				MessageBox.Show(e.Message);
			}
			return new Point(0, 0);

		}

		public static Point AlignLocal(Bitmap model, Bitmap target, Rectangle searchRect, out double matchLevel)
		{
			matchLevel = 0.0;
			try
			{
				MPoint findPoint;
				BitmapData modelBd = model.LockBits(new Rectangle(new Point(0, 0), model.Size), ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed);
				BitmapData targetBd = target.LockBits(new Rectangle(new Point(0, 0), target.Size), ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed);
				MImage mmodel = new MImage(model.Width, model.Height, 8, modelBd.Scan0);
				MImage mtarget = new MImage(target.Width, target.Height, 8, targetBd.Scan0);

				matchLevel = ImageLib.AlignH(mmodel, mtarget, new MRect(searchRect), 2, 2, 2, 2, out findPoint);

				model.UnlockBits(modelBd);
				target.UnlockBits(targetBd);

				return findPoint.Point;
			}
			catch (Exception e)
			{
				MessageBox.Show(e.Message);
			}

			return new Point(0, 0);

		}

		public static void AutoContrast(Bitmap source, Bitmap target)
		{
			Bitmap bitmap = source;
			Bitmap resultImage = target;
			BitmapData bd = bitmap.LockBits(new Rectangle(new Point(0, 0), bitmap.Size), ImageLockMode.ReadWrite, PixelFormat.Format8bppIndexed);
			BitmapData bdResult = resultImage.LockBits(new Rectangle(new Point(0, 0), bitmap.Size), ImageLockMode.ReadWrite, PixelFormat.Format8bppIndexed);
			MImage mimage = new MImage(bitmap.Width, bitmap.Height, 8, bd.Scan0);
			MImage mimageResult = new MImage(bitmap.Width, bitmap.Height, 8, bdResult.Scan0);
			ImageLib.AutoContrast(mimage, mimageResult);
			bitmap.UnlockBits(bd);
			resultImage.UnlockBits(bdResult);
		}

		public static void Thresholding(Bitmap source, Bitmap target, int thresholdLevel, Rectangle roiRect)
		{
			Bitmap bitmap = source;
			Bitmap resultImage = target;
			lock (bitmap)
			{
				BitmapData bd = bitmap.LockBits(new Rectangle(new Point(0, 0), bitmap.Size), ImageLockMode.ReadWrite, PixelFormat.Format8bppIndexed);
				BitmapData bdResult;
				if (bitmap != resultImage)
				{
					bdResult = resultImage.LockBits(new Rectangle(new Point(0, 0), bitmap.Size), ImageLockMode.ReadWrite, PixelFormat.Format8bppIndexed);
				}
				else
				{
					bdResult = bd;
				}
				MImage mimage = new MImage(bitmap.Width, bitmap.Height, 8, bd.Scan0);
				MImage mimageResult = new MImage(bitmap.Width, bitmap.Height, 8, bdResult.Scan0);
				mimage.rectRoi = new MRect(roiRect);
				mimageResult.rectRoi = new MRect(roiRect);
				ImageLib.Thresholding(mimage, thresholdLevel, mimageResult);
				bitmap.UnlockBits(bd);
				if (bitmap != resultImage)
				{
					resultImage.UnlockBits(bdResult);
				}
			}
		}

		public static void Thresholding(Bitmap source, Bitmap target, int thresholdLevel)
		{
			Rectangle roiRect;
			roiRect = new Rectangle(new Point(0, 0), source.Size);
			Thresholding(source, target, thresholdLevel, roiRect);
		}

		public static void ThresholdingInverse(Bitmap source, Bitmap target, int thresholdLevel, Rectangle roiRect)
		{
			Bitmap bitmap = source;
			Bitmap resultImage = target;
			lock (bitmap)
			{
				BitmapData bd = bitmap.LockBits(new Rectangle(new Point(0, 0), bitmap.Size), ImageLockMode.ReadWrite, PixelFormat.Format8bppIndexed);

				BitmapData bdResult;
				if (bitmap != resultImage)
				{
					bdResult = resultImage.LockBits(new Rectangle(new Point(0, 0), bitmap.Size), ImageLockMode.ReadWrite, PixelFormat.Format8bppIndexed);
				}
				else
				{
					bdResult = bd;
				}

				MImage mimage = new MImage(bitmap.Width, bitmap.Height, 8, bd.Scan0);
				MImage mimageResult = new MImage(bitmap.Width, bitmap.Height, 8, bdResult.Scan0);
				mimage.rectRoi = new MRect(roiRect);
				mimageResult.rectRoi = new MRect(roiRect);
				ImageLib.ThresholdingInverse(mimage, thresholdLevel, mimageResult);

				bitmap.UnlockBits(bd);
				if (bitmap != resultImage)
				{
					resultImage.UnlockBits(bdResult);
				}
			}
		}

		public static void Thining(Bitmap bitmap)
		{
			//            Bitmap bitmap = source;
			BitmapData bd = bitmap.LockBits(new Rectangle(new Point(0, 0), bitmap.Size), ImageLockMode.ReadWrite, PixelFormat.Format8bppIndexed);
			//            BitmapData bdResult = resultImage.LockBits(new Rectangle(new Point(0, 0), bitmap.Size), ImageLockMode.ReadWrite, PixelFormat.Format8bppIndexed);
			MImage mimage = new MImage(bitmap.Width, bitmap.Height, 8, bd.Scan0);
			//            MImage mimageResult = new MImage(bitmap.Width, bitmap.Height, 8, bdResult.Scan0);
			//            mimage.rectRoi = new MRect(roiRect);
			//            mimageResult.rectRoi = new MRect(roiRect);
			ImageLib.Thining(mimage);

			bitmap.UnlockBits(bd);
			//            resultImage.UnlockBits(bdResult);
		}

		public static void ThresholdingInverse(Bitmap source, Bitmap target, int thresholdLevel)
		{
			Rectangle roiRect;
			roiRect = new Rectangle(new Point(0, 0), source.Size);
			ThresholdingInverse(source, target, thresholdLevel, roiRect);
		}

		public static void GetHistogram(Bitmap bitmap, out int[] histogramBuffer)
		{
			BitmapData bd = bitmap.LockBits(new Rectangle(new Point(0, 0), bitmap.Size), ImageLockMode.ReadWrite, PixelFormat.Format8bppIndexed);
			MImage mimage = new MImage(bitmap.Width, bitmap.Height, 8, bd.Scan0);
			ImageLib.GetHistogram(mimage, out histogramBuffer);
			bitmap.UnlockBits(bd);
		}

		public static Bitmap BitmapClone(Bitmap src, Rectangle rect)
		{
			Bitmap target = new Bitmap(rect.Width, rect.Height, src.PixelFormat);
			GrayPalette(target);

			BitmapData bdOrg = src.LockBits(new Rectangle(new Point(0, 0), src.Size),
													ImageLockMode.ReadWrite,
													src.PixelFormat);


			BitmapData bdTgt = target.LockBits(new Rectangle(new Point(0, 0), target.Size),
													ImageLockMode.ReadWrite,
													target.PixelFormat);

			if (bdTgt.Stride > target.Width)
			{
				int nWidth = bdTgt.Stride;
				target.UnlockBits(bdTgt);
				target.Dispose();
				target = new Bitmap(nWidth, rect.Height, src.PixelFormat);
				GrayPalette(target);
				bdTgt = target.LockBits(new Rectangle(new Point(0, 0), target.Size),
														ImageLockMode.ReadWrite,
														target.PixelFormat);
			}
			unsafe
			{
				int orgStride = bdOrg.Stride;
				int tgtStride = bdTgt.Stride;
				byte* pOrg = (byte*)bdOrg.Scan0.ToPointer();
				byte* pTgt = (byte*)bdTgt.Scan0.ToPointer();

				int width = rect.Width;
				int height = rect.Height;

				for (int y = 0; y < height; y++)
				{
					byte* pOrgLine = pOrg + (orgStride * (y + rect.Y)) + rect.X;
					byte* pTgtLine = pTgt + (tgtStride * y);
					for (int x = 0; x < width; x++)
					{
						pTgtLine[x] = pOrgLine[x];
					}
				}
			}
			target.UnlockBits(bdTgt);
			src.UnlockBits(bdOrg);

			return target;
		}

		public static Bitmap BitmapClone(Bitmap src, Rectangle rect, int scale)
		{
			Bitmap target = new Bitmap(rect.Width / scale, rect.Height / scale, src.PixelFormat);
			GrayPalette(target);

			BitmapData bdOrg = src.LockBits(new Rectangle(new Point(0, 0), src.Size),
													ImageLockMode.ReadWrite,
													src.PixelFormat);


			BitmapData bdTgt = target.LockBits(new Rectangle(new Point(0, 0), target.Size),
													ImageLockMode.ReadWrite,
													target.PixelFormat);
			if (bdTgt.Stride > target.Width)
			{
				int nWidth = bdTgt.Stride;
				target.UnlockBits(bdTgt);
				target.Dispose();
				target = new Bitmap(nWidth, rect.Height / scale, src.PixelFormat);
				GrayPalette(target);
				bdTgt = target.LockBits(new Rectangle(new Point(0, 0), target.Size),
														ImageLockMode.ReadWrite,
														target.PixelFormat);
			}
			unsafe
			{
				int orgStride = bdOrg.Stride;
				int tgtStride = bdTgt.Stride;
				byte* pOrg = (byte*)bdOrg.Scan0.ToPointer();
				byte* pTgt = (byte*)bdTgt.Scan0.ToPointer();

				int left = rect.Left;
				int top = rect.Top;
				int right = rect.Right - scale;
				int bottom = rect.Bottom - scale;
				int value = 0;

				for (int y = top; y < bottom; y += scale)
				{
					for (int x = left; x < right; x += scale)
					{
						value = 0;
						for (int yy = y; yy < y + scale; yy++)
						{
							for (int xx = x; xx < x + scale; xx++)
							{
								value += pOrg[orgStride * yy + xx];
							}
						}
						pTgt[tgtStride * (y - top) / scale + (x - left) / scale] = (byte)((double)value / (double)(scale * scale));
					}
				}
			}
			target.UnlockBits(bdTgt);
			src.UnlockBits(bdOrg);

			return target;
		}

		public static Bitmap BitmapResizeBilinear(Bitmap src)
		{
			Bitmap target = new Bitmap(src.Width * 2, src.Height * 2, src.PixelFormat);
			GrayPalette(target);

			BitmapData bdOrg = src.LockBits(new Rectangle(new Point(0, 0), src.Size),
													ImageLockMode.ReadOnly,
													src.PixelFormat);


			BitmapData bdTgt = target.LockBits(new Rectangle(new Point(0, 0), target.Size),
													ImageLockMode.ReadWrite,
													target.PixelFormat);
			unsafe
			{
				int orgStride = bdOrg.Stride;
				int tgtStride = bdTgt.Stride;
				byte* pOrg = (byte*)bdOrg.Scan0.ToPointer();
				byte* pTgt = (byte*)bdTgt.Scan0.ToPointer();

				int w = src.Width;
				int h = src.Height;
				int nw = w - 1;
				int nh = h - 1;

				byte* pOrgLine1, pOrgLine2, pTgtLine1, pTgtLine2;
				for (int j = 0; j < h; j++)
				{
					pOrgLine1 = pOrg + (orgStride * j);
					pOrgLine2 = pOrgLine1 + orgStride;
					pTgtLine1 = pTgt + (tgtStride * (j * 2));
					pTgtLine2 = pTgtLine1 + tgtStride;
					for (int i = 0; i < w; i++)
					{
						if (i < nw && j < nh)
						{
							pTgtLine1[i * 2] = pOrgLine1[i];
							pTgtLine1[i * 2 + 1] = (byte)((pOrgLine1[i] + pOrgLine1[i + 1]) >> 1);
							pTgtLine2[i * 2] = (byte)((pOrgLine1[i] + pOrgLine2[i]) >> 1);
							pTgtLine2[i * 2 + 1] = (byte)((pOrgLine1[i] + pOrgLine1[i + 1] + pOrgLine2[i] + pOrgLine2[i + 1]) >> 2);
						}
						else if (i == nw && j < nh)
						{
							pTgtLine1[i * 2] = pOrgLine1[i];
							pTgtLine1[i * 2 + 1] = pOrgLine1[i];
							pTgtLine2[i * 2] = (byte)((pOrgLine1[i] + pOrgLine2[i]) >> 1);
							pTgtLine2[i * 2 + 1] = (byte)((pOrgLine1[i] + pOrgLine2[i]) >> 1);
						}
						else if (i < nw && j == nh)
						{
							pTgtLine1[i * 2] = pOrgLine1[i];
							pTgtLine1[i * 2 + 1] = (byte)((pOrgLine1[i] + pOrgLine1[i + 1]) >> 1);
							pTgtLine2[i * 2] = pOrgLine1[i];
							pTgtLine2[i * 2 + 1] = (byte)((pOrgLine1[i] + pOrgLine1[i + 1]) >> 1);
						}
						else
						{
							pTgtLine1[i * 2] = pOrgLine1[i];
							pTgtLine1[i * 2 + 1] = pOrgLine1[i];
							pTgtLine2[i * 2] = pOrgLine1[i];
							pTgtLine2[i * 2 + 1] = pOrgLine1[i];
						}
					}
				}
			}
			target.UnlockBits(bdTgt);
			src.UnlockBits(bdOrg);

			return target;
		}

		public static Bitmap RawToBitmap(int width, int height, byte[] rawData)
		{
			try
			{
				Bitmap bitmapImage = new Bitmap(width, height, PixelFormat.Format8bppIndexed);
				BitmapData bitmapImageData = bitmapImage.LockBits(new Rectangle(0, 0, width, height)
																, ImageLockMode.WriteOnly
																, PixelFormat.Format8bppIndexed);
				unsafe
				{
					byte* pointer = (byte*)bitmapImageData.Scan0.ToPointer();
					for (int xy = 0; xy < width * height; xy++)
						*pointer++ = rawData[xy];
				}
				bitmapImage.UnlockBits(bitmapImageData);
				GrayPalette(bitmapImage);

				return bitmapImage;
			}
			catch (Exception)
			{
				return null;
			}
		}

		public static Bitmap Bayer8ToBitmap(int width, int height, int bufferPitch, IntPtr bufferAddress)
		{
			/// Bayer8 구조(8bit)
			/// G B G B
			/// R G R G
			/// G B G B
			/// R G R G
			try
			{
				int r = 0;
				int g = 0;
				int b = 0;
				Bitmap bitmapImage = new Bitmap(width, height, PixelFormat.Format24bppRgb);
				BitmapData bitmapImageData = bitmapImage.LockBits(new Rectangle(0, 0, width, height)
																, ImageLockMode.WriteOnly
																, PixelFormat.Format24bppRgb);
				int widthStride = bitmapImageData.Stride;
				unsafe
				{
					byte* src = (byte*)bufferAddress.ToPointer();
					byte* pointer = (byte*)bitmapImageData.Scan0.ToPointer();
					for (int y = 0; y < height; y++)
					{
						for (int x = 0; x < width; x++)
						{
							r = 0;
							g = 0;
							b = 0;
							if ((x % 2) == 0 && (y % 2) == 1)
							{
								r += *(src + (y * bufferPitch) + x);

								if ( x != 0)
									g += *(src + (y * bufferPitch) + x - 1);
								if (y != 0)
									g += *(src + ((y - 1) * bufferPitch) + x);
								if (x != width)
									g += *(src + (y * bufferPitch) + x + 1);
								if (y != height)
									g += *(src + ((y + 1) * bufferPitch) + x);

								if (x != 0 && y != 0)
									b += *(src + ((y - 1) * bufferPitch) + x - 1);
								if (y != 0 && x != width)
									b += *(src + ((y - 1) * bufferPitch) + x + 1);
								if (x != width && y != height)
									b += *(src + ((y + 1) * bufferPitch) + x + 1);
								if (y != height && x != 0)
									b += *(src + ((y + 1) * bufferPitch) + x - 1);

								r /= 1;
								g /= 4;
								b /= 4;
							}
							else if ((x % 2) == 1 && (y % 2) == 1)
							{
								g += *(src + (y * bufferPitch) + x);

								if (x != 0)
									r += *(src + (y * bufferPitch) + x - 1);
								if (y != 0)
									b += *(src + ((y - 1) * bufferPitch) + x);
								if (x != width)
									r += *(src + (y * bufferPitch) + x + 1);
								if (y != height)
									b += *(src + ((y + 1) * bufferPitch) + x);

								if (x != 0 && y != 0)
									g += *(src + ((y - 1) * bufferPitch) + x - 1);
								if (y != 0 && x != width)
									g += *(src + ((y - 1) * bufferPitch) + x + 1);
								if (x != width && y != height)
									g += *(src + ((y + 1) * bufferPitch) + x + 1);
								if (y != height && x != 0)
									g += *(src + ((y + 1) * bufferPitch) + x - 1);

								r /= 2;
								g /= 5;
								b /= 2;
							}
							else if ((x % 2) == 0 && (y % 2) == 0)
							{
								g += *(src + (y * bufferPitch) + x);

								if (x != 0)
									b += *(src + (y * bufferPitch) + x - 1);
								if (y != 0)
									r += *(src + ((y - 1) * bufferPitch) + x);
								if (x != width)
									b += *(src + (y * bufferPitch) + x + 1);
								if (y != height)
									r += *(src + ((y + 1) * bufferPitch) + x);

								if (x != 0 && y != 0)
									g += *(src + ((y - 1) * bufferPitch) + x - 1);
								if (y != 0 && x != width)
									g += *(src + ((y - 1) * bufferPitch) + x + 1);
								if (x != width && y != height)
									g += *(src + ((y + 1) * bufferPitch) + x + 1);
								if (y != height && x != 0)
									g += *(src + ((y + 1) * bufferPitch) + x - 1);

								r /= 2;
								g /= 5;
								b /= 2;
							}
							else
							{
								b += *(src + (y * bufferPitch) + x);

								if (x != 0)
									g += *(src + (y * bufferPitch) + x - 1);
								if (y != 0)
									g += *(src + ((y - 1) * bufferPitch) + x);
								if (x != width)
									g += *(src + (y * bufferPitch) + x + 1);
								if (y != height)
									g += *(src + ((y + 1) * bufferPitch) + x);

								if (x != 0 && y != 0)
									r += *(src + ((y - 1) * bufferPitch) + x - 1);
								if (y != 0 && x != width)
									r += *(src + ((y - 1) * bufferPitch) + x + 1);
								if (x != width && y != height)
									r += *(src + ((y + 1) * bufferPitch) + x + 1);
								if (y != height && x != 0)
									r += *(src + ((y + 1) * bufferPitch) + x - 1);

								r /= 4;
								g /= 4;
								b /= 1;
							}
							*(pointer + (y * widthStride) + (x * 3)) = (byte)r;
							*(pointer + (y * widthStride) + (x * 3) + 1) = (byte)g;
							*(pointer + (y * widthStride) + (x * 3) + 2) = (byte)b;
						}
					}
				}
				bitmapImage.UnlockBits(bitmapImageData);

				return bitmapImage;
			}
			catch (Exception)
			{
				return null;
			}
		}

		public static Rectangle CheckRoi(Rectangle org, Bitmap basis)
		{
			Rectangle tar = org;
			try
			{
				if (org.Left < 0)
					tar.X = 0;
				if (org.Top < 0)
					tar.Y = 0;
				if (org.Right > basis.Width)
					tar.X -= org.Right - basis.Width;
				if (org.Bottom > basis.Height)
					tar.Y -= org.Bottom - basis.Height;
				if (tar.Left < 0)
				{
					tar.Width += tar.X;
					tar.X = 0;
				}
				if (tar.Top < 0)
				{
					tar.Height += tar.Y;
					tar.Y = 0;
				}
				if (tar.Right > basis.Width)
					tar.Width = basis.Width - tar.X;
				if (tar.Bottom > basis.Height)
					tar.Height = basis.Height - tar.Y;
			}
			catch { }
			return tar;
		}

		public static Image ConvertToGrayImage(Bitmap image, ConvertType type)
		{
			int height = image.Height;
			int width = image.Width;
			Bitmap convImage = new Bitmap(image.Width, image.Height, PixelFormat.Format8bppIndexed);
			BitmapData bdConv = convImage.LockBits(new Rectangle(new Point(0, 0), image.Size),
													ImageLockMode.ReadWrite,
													convImage.PixelFormat);
			BitmapData bdOrg = image.LockBits(new Rectangle(new Point(0, 0), image.Size),
													ImageLockMode.ReadWrite,
													image.PixelFormat);
			unsafe
			{
				int orgStride = bdOrg.Stride;
				int tgtStride = bdConv.Stride;
				byte* pOrg = (byte*)bdOrg.Scan0.ToPointer();
				byte* pConv = (byte*)bdConv.Scan0.ToPointer();

				switch (type)
				{
					case ConvertType.Average:
						for (int y = 0; y < height; y++)
						{
							byte* pOrgLine = pOrg + (orgStride * y);
							byte* pConvLine = pConv + (tgtStride * y);
							for (int x = 0; x < width; x++)
							{
								*pConvLine = (byte)((*pOrgLine + *(pOrgLine + 1) + *(pOrgLine + 2)) / 3);
								pConvLine++;
								pOrgLine += 3;
							}

						}
						break;

					case ConvertType.BChannel:
						for (int y = 0; y < height; y++)
						{
							byte* pOrgLine = pOrg + (orgStride * y);
							byte* pConvLine = pConv + (tgtStride * y);
							for (int x = 0; x < width; x++)
							{
								*pConvLine = *pOrgLine;
								pConvLine++;
								pOrgLine += 3;
							}
						}
						break;
					case ConvertType.GChannel:
						for (int y = 0; y < height; y++)
						{
							byte* pOrgLine = pOrg + (orgStride * y);
							byte* pConvLine = pConv + (tgtStride * y);
							for (int x = 0; x < width; x++)
							{
								*pConvLine = *(pOrgLine + 1);
								pConvLine++;
								pOrgLine += 3;
							}
						}
						break;
					case ConvertType.RChannel:
						for (int y = 0; y < height; y++)
						{
							byte* pOrgLine = pOrg + (orgStride * y);
							byte* pConvLine = pConv + (tgtStride * y);
							for (int x = 0; x < width; x++)
							{
								*pConvLine = *(pOrgLine + 2);
								pConvLine++;
								pOrgLine += 3;
							}
						}
						break;
				}

			}

			image.UnlockBits(bdOrg);
			convImage.UnlockBits(bdConv);
			GrayPalette(convImage);
			return convImage;
		}

		public static void GrayPalette(Image image)
		{
			if (image == null)
				return;
			ColorPalette palette = image.Palette;
			for (int i = 0; i < 256; i++)
			{
				palette.Entries[i] = Color.FromArgb(i, i, i);
			}
			image.Palette = palette;
		}

		public static void RotateBi(Bitmap image, Bitmap target, double angle)
		{
			BitmapData bdTarget = target.LockBits(new Rectangle(new Point(0, 0), image.Size),
													ImageLockMode.ReadWrite,
													image.PixelFormat);
			BitmapData bdOrg = image.LockBits(new Rectangle(new Point(0, 0), image.Size),
													ImageLockMode.ReadWrite,
													image.PixelFormat);

			MImage mimage = new MImage(image.Width, image.Height, 8, bdOrg.Scan0);
			MImage mimageResult = new MImage(image.Width, image.Height, 8, bdTarget.Scan0);

			ImageLib.RotateBi(mimage, mimageResult, angle);


			image.UnlockBits(bdOrg);
			target.UnlockBits(bdTarget);
		}

		public static void Dilation4(Bitmap image, Bitmap target)
		{
			lock (image)
			{

				BitmapData bdTarget = target.LockBits(new Rectangle(new Point(0, 0), image.Size),
														ImageLockMode.ReadWrite,
														image.PixelFormat);
				BitmapData bdOrg;
				if (image != target)
				{
					bdOrg = image.LockBits(new Rectangle(new Point(0, 0), image.Size),
															ImageLockMode.ReadWrite,
															image.PixelFormat);
				}
				else
				{
					bdOrg = bdTarget;
				}
				MImage mimage = new MImage(image.Width, image.Height, 8, bdOrg.Scan0);
				MImage mimageResult = new MImage(image.Width, image.Height, 8, bdTarget.Scan0);

				ImageLib.Dilation4(mimage, mimageResult);


				if (image != target)
				{
					image.UnlockBits(bdOrg);
				}
				target.UnlockBits(bdTarget);
			}
		}
		public static void Dilation8(Bitmap image, Bitmap target)
		{
			lock (image)
			{

				BitmapData bdTarget = target.LockBits(new Rectangle(new Point(0, 0), image.Size),
														ImageLockMode.ReadWrite,
														image.PixelFormat);
				BitmapData bdOrg;
				if (image != target)
				{
					bdOrg = image.LockBits(new Rectangle(new Point(0, 0), image.Size),
															ImageLockMode.ReadWrite,
															image.PixelFormat);
				}
				else
				{
					bdOrg = bdTarget;
				}
				MImage mimage = new MImage(image.Width, image.Height, 8, bdOrg.Scan0);
				MImage mimageResult = new MImage(image.Width, image.Height, 8, bdTarget.Scan0);

				ImageLib.Dilation8(mimage, mimageResult);


				if (image != target)
				{
					image.UnlockBits(bdOrg);
				}
				target.UnlockBits(bdTarget);
			}
		}

		public static void Erosion4(Bitmap image, Bitmap target)
		{
			lock (image)
			{

				BitmapData bdTarget = target.LockBits(new Rectangle(new Point(0, 0), image.Size),
														ImageLockMode.ReadWrite,
														image.PixelFormat);
				BitmapData bdOrg;
				if (image != target)
				{
					bdOrg = image.LockBits(new Rectangle(new Point(0, 0), image.Size),
															ImageLockMode.ReadWrite,
															image.PixelFormat);
				}
				else
				{
					bdOrg = bdTarget;
				}
				MImage mimage = new MImage(image.Width, image.Height, 8, bdOrg.Scan0);
				MImage mimageResult = new MImage(image.Width, image.Height, 8, bdTarget.Scan0);

				ImageLib.Erosion4(mimage, mimageResult);


				if (image != target)
				{
					image.UnlockBits(bdOrg);
				}
				target.UnlockBits(bdTarget);
			}
		}

		public static void Erosion8(Bitmap image, Bitmap target)
		{
			lock (image)
			{

				BitmapData bdTarget = target.LockBits(new Rectangle(new Point(0, 0), image.Size),
														ImageLockMode.ReadWrite,
														image.PixelFormat);
				BitmapData bdOrg;
				if (image != target)
				{
					bdOrg = image.LockBits(new Rectangle(new Point(0, 0), image.Size),
															ImageLockMode.ReadWrite,
															image.PixelFormat);
				}
				else
				{
					bdOrg = bdTarget;
				}
				MImage mimage = new MImage(image.Width, image.Height, 8, bdOrg.Scan0);
				MImage mimageResult = new MImage(image.Width, image.Height, 8, bdTarget.Scan0);

				ImageLib.Erosion8(mimage, mimageResult);


				if (image != target)
				{
					image.UnlockBits(bdOrg);
				}
				target.UnlockBits(bdTarget);
			}

		}

		public static void AddEllipticMask(Bitmap image, ref Rectangle rect, int grayLevel)
		{
			lock (image)
			{
				BitmapData bdOrg;
				bdOrg = image.LockBits(new Rectangle(new Point(0, 0), image.Size),
														ImageLockMode.ReadWrite,
														image.PixelFormat);
				MImage mimage = new MImage(image.Width, image.Height, 8, bdOrg.Scan0);

				ImageLib.AddEllipticMask(mimage, ref rect, grayLevel);


				image.UnlockBits(bdOrg);
			}
		}

		public static void AddRectMask(Bitmap image, Rectangle rect, int grayLevel)
		{
			lock (image)
			{
				BitmapData bdOrg;
				bdOrg = image.LockBits(new Rectangle(new Point(0, 0), image.Size),
														ImageLockMode.ReadWrite,
														image.PixelFormat);
				unsafe
				{
					int orgStride = bdOrg.Stride;
					byte* pOrg = (byte*)bdOrg.Scan0.ToPointer();
					int right = rect.Right;
					int bottom = rect.Bottom;
					for (int y = rect.Top; y < bottom; y++)
					{
						byte* pOrgLine = pOrg + (orgStride * y);
						for (int x = rect.Left; x < right; x++)
						{
							pOrgLine[x] = (byte)grayLevel;
						}
					}
				}

				image.UnlockBits(bdOrg);
			}
		}

	}

	public class ConvMatrix
	{
		public int TopLeft = 0, TopMid = 0, TopRight = 0;
		public int MidLeft = 0, Pixel = 1, MidRight = 0;
		public int BottomLeft = 0, BottomMid = 0, BottomRight = 0;
		public int Factor = 1;
		public int Offset = 0;
		public void SetAll(int nVal)
		{
			TopLeft = TopMid = TopRight = MidLeft = Pixel = MidRight =
					  BottomLeft = BottomMid = BottomRight = nVal;
		}
	}

	public class ImageFilter
	{
		private static bool Conv3x3(Bitmap b, ConvMatrix m, PixelFormat pfmt)
		{
			if (pfmt == PixelFormat.Format24bppRgb)
				return Conv3x3_24Rgb(b, m);
			else
				return Conv3x3_8Indexed(b, m);
		}

		private static bool Conv3x3_8Indexed(Bitmap b, ConvMatrix m)
		{
			// Avoid divide by zero errors
			if (0 == m.Factor)
				return false; Bitmap

			// GDI+ still lies to us - the return format is BGR, NOT RGB. 
			bSrc = (Bitmap)b.Clone();
			BitmapData bmData = b.LockBits(new Rectangle(0, 0, b.Width, b.Height),
								ImageLockMode.ReadWrite,
								PixelFormat.Format8bppIndexed);
			BitmapData bmSrc = bSrc.LockBits(new Rectangle(0, 0, bSrc.Width, bSrc.Height),
							   ImageLockMode.ReadWrite,
							   PixelFormat.Format8bppIndexed);
			int stride = bmData.Stride;
			int stride2 = stride * 2;

			System.IntPtr Scan0 = bmData.Scan0;
			System.IntPtr SrcScan0 = bmSrc.Scan0;

			unsafe
			{
				byte* p = (byte*)(void*)Scan0;
				byte* pSrc = (byte*)(void*)SrcScan0;
				int nOffset = stride - b.Width + 2;
				int nWidth = b.Width - 2;
				int nHeight = b.Height - 2;

				int nPixel;

				for (int y = 0; y < nHeight; ++y)
				{
					for (int x = 0; x < nWidth; ++x)
					{
						nPixel = ((((pSrc[0] * m.TopLeft) +
							(pSrc[1] * m.TopMid) +
							(pSrc[2] * m.TopRight) +
							(pSrc[0 + stride] * m.MidLeft) +
							(pSrc[1 + stride] * m.Pixel) +
							(pSrc[2 + stride] * m.MidRight) +
							(pSrc[0 + stride2] * m.BottomLeft) +
							(pSrc[1 + stride2] * m.BottomMid) +
							(pSrc[2 + stride2] * m.BottomRight))
							/ m.Factor) + m.Offset);

						if (nPixel < 0) nPixel = 0;
						if (nPixel > 255) nPixel = 255;
						p[1 + stride] = (byte)nPixel;

						p += 1;
						pSrc += 1;
					}

					p += nOffset;
					pSrc += nOffset;
				}
			}

			b.UnlockBits(bmData);
			bSrc.UnlockBits(bmSrc);
			return true;
		}

		private static bool Conv3x3_24Rgb(Bitmap b, ConvMatrix m)
		{
			// Avoid divide by zero errors
			if (0 == m.Factor)
				return false; Bitmap

			// GDI+ still lies to us - the return format is BGR, NOT RGB. 
			bSrc = (Bitmap)b.Clone();
			BitmapData bmData = b.LockBits(new Rectangle(0, 0, b.Width, b.Height),
								ImageLockMode.ReadWrite,
								PixelFormat.Format24bppRgb);
			BitmapData bmSrc = bSrc.LockBits(new Rectangle(0, 0, bSrc.Width, bSrc.Height),
							   ImageLockMode.ReadWrite,
							   PixelFormat.Format24bppRgb);
			int stride = bmData.Stride;
			int stride2 = stride * 2;

			System.IntPtr Scan0 = bmData.Scan0;
			System.IntPtr SrcScan0 = bmSrc.Scan0;

			unsafe
			{
				byte* p = (byte*)(void*)Scan0;
				byte* pSrc = (byte*)(void*)SrcScan0;
				int nOffset = stride - b.Width * 3;
				int nWidth = b.Width - 2;
				int nHeight = b.Height - 2;

				int nPixel;

				for (int y = 0; y < nHeight; ++y)
				{
					for (int x = 0; x < nWidth; ++x)
					{
						nPixel = ((((pSrc[2] * m.TopLeft) +
							(pSrc[5] * m.TopMid) +
							(pSrc[8] * m.TopRight) +
							(pSrc[2 + stride] * m.MidLeft) +
							(pSrc[5 + stride] * m.Pixel) +
							(pSrc[8 + stride] * m.MidRight) +
							(pSrc[2 + stride2] * m.BottomLeft) +
							(pSrc[5 + stride2] * m.BottomMid) +
							(pSrc[8 + stride2] * m.BottomRight))
							/ m.Factor) + m.Offset);

						if (nPixel < 0) nPixel = 0;
						if (nPixel > 255) nPixel = 255;
						p[5 + stride] = (byte)nPixel;

						nPixel = ((((pSrc[1] * m.TopLeft) +
							(pSrc[4] * m.TopMid) +
							(pSrc[7] * m.TopRight) +
							(pSrc[1 + stride] * m.MidLeft) +
							(pSrc[4 + stride] * m.Pixel) +
							(pSrc[7 + stride] * m.MidRight) +
							(pSrc[1 + stride2] * m.BottomLeft) +
							(pSrc[4 + stride2] * m.BottomMid) +
							(pSrc[7 + stride2] * m.BottomRight))
							/ m.Factor) + m.Offset);

						if (nPixel < 0) nPixel = 0;
						if (nPixel > 255) nPixel = 255;
						p[4 + stride] = (byte)nPixel;

						nPixel = ((((pSrc[0] * m.TopLeft) +
									   (pSrc[3] * m.TopMid) +
									   (pSrc[6] * m.TopRight) +
									   (pSrc[0 + stride] * m.MidLeft) +
									   (pSrc[3 + stride] * m.Pixel) +
									   (pSrc[6 + stride] * m.MidRight) +
									   (pSrc[0 + stride2] * m.BottomLeft) +
									   (pSrc[3 + stride2] * m.BottomMid) +
									   (pSrc[6 + stride2] * m.BottomRight))
							/ m.Factor) + m.Offset);

						if (nPixel < 0) nPixel = 0;
						if (nPixel > 255) nPixel = 255;
						p[3 + stride] = (byte)nPixel;

						p += 3;
						pSrc += 3;
					}

					p += nOffset;
					pSrc += nOffset;
				}
			}

			b.UnlockBits(bmData);
			bSrc.UnlockBits(bmSrc);
			return true;
		}

		public static bool Smooth(Bitmap b, int nWeight /* default to 1 */)
		{
			ConvMatrix m = new ConvMatrix();
			m.SetAll(1);
			m.Pixel = nWeight;
			m.Factor = nWeight + 8;

			return Conv3x3(b, m, b.PixelFormat);
		}

		public static bool GaussianBlur(Bitmap b)
		{
			ConvMatrix m = new ConvMatrix();
			m.SetAll(1);
			m.TopMid = m.MidLeft = m.MidRight = m.BottomMid = 2;
			m.Pixel = 4;
			m.Factor = 16;

			return Conv3x3(b, m, b.PixelFormat);
		}

		public static bool Sharpen(Bitmap b)
		{
			ConvMatrix m = new ConvMatrix();
			m.SetAll(0);
			m.TopMid = m.MidLeft = m.MidRight = m.BottomMid = -2;
			m.Pixel = 11;
			m.Factor = 3;

			return Conv3x3(b, m, b.PixelFormat);
		}

		public static bool MeanRemoval(Bitmap b)
		{
			ConvMatrix m = new ConvMatrix();
			m.SetAll(-1);
			//m.TopMid = m.MidLeft = m.MidRight = m.BottomMid = -2;
			m.Pixel = 9;
			m.Factor = 1;

			return Conv3x3(b, m, b.PixelFormat);
		}

		public static bool Embossing(Bitmap b)
		{
			ConvMatrix m = new ConvMatrix();
			m.SetAll(-1);
			m.TopMid = m.MidLeft = m.MidRight = m.BottomMid = 0;
			m.Pixel = 4;
			m.Factor = 1;
			m.Offset = 127;

			return Conv3x3(b, m, b.PixelFormat);
		}

		public static bool HorzVertical(Bitmap b)
		{
			ConvMatrix m = new ConvMatrix();
			m.SetAll(0);
			m.TopMid = m.MidLeft = m.MidRight = m.BottomMid = -1;
			m.Pixel = 4;
			m.Factor = 1;
			m.Offset = 127;

			return Conv3x3(b, m, b.PixelFormat);
		}

		public static bool AllDirections(Bitmap b)
		{
			ConvMatrix m = new ConvMatrix();
			m.SetAll(-1);
			//m.TopMid = m.MidLeft = m.MidRight = m.BottomMid = -1;
			m.Pixel = 8;
			m.Factor = 1;
			m.Offset = 127;

			return Conv3x3(b, m, b.PixelFormat);
		}

		public static bool Lossy(Bitmap b)
		{
			ConvMatrix m = new ConvMatrix();
			m.SetAll(1);
			m.TopMid = m.MidLeft = m.MidRight = m.BottomLeft = m.BottomRight = -2;
			m.Pixel = 4;
			m.Factor = 1;
			m.Offset = 127;

			return Conv3x3(b, m, b.PixelFormat);
		}

		public static bool HorizontalOnly(Bitmap b)
		{
			ConvMatrix m = new ConvMatrix();
			m.SetAll(0);
			m.MidLeft = m.MidRight = -1;
			m.Pixel = 2;
			m.Factor = 1;
			m.Offset = 127;

			return Conv3x3(b, m, b.PixelFormat);
		}

		public static bool VerticalOnly(Bitmap b)
		{
			ConvMatrix m = new ConvMatrix();
			m.SetAll(0);
			m.TopMid = -1;
			m.BottomMid = 1;
			m.Pixel = 0;
			m.Factor = 1;
			m.Offset = 127;

			return Conv3x3(b, m, b.PixelFormat);
		}

		public static bool EdgeDetection(Bitmap b)
		{
			ConvMatrix m = new ConvMatrix();
			m.TopLeft = m.TopMid = m.TopRight = 1;
			m.MidLeft = m.Pixel = m.MidRight = 0;
			m.BottomLeft = m.BottomMid = m.BottomRight = -1;
			m.Factor = 1;
			m.Offset = 127;

			return Conv3x3(b, m, b.PixelFormat);
		}

		public static bool Different(Bitmap b)
		{
			Bitmap bSrc = (Bitmap)b.Clone();
			BitmapData bmData = b.LockBits(new Rectangle(0, 0, b.Width, b.Height),
								ImageLockMode.ReadWrite,
								PixelFormat.Format8bppIndexed);
			BitmapData bmSrc = bSrc.LockBits(new Rectangle(0, 0, bSrc.Width, bSrc.Height),
							   ImageLockMode.ReadWrite,
							   PixelFormat.Format8bppIndexed);
			int stride = bmData.Stride;
			int stride2 = stride * 2;

			System.IntPtr Scan0 = bmData.Scan0;
			System.IntPtr SrcScan0 = bmSrc.Scan0;

			unsafe
			{
				byte* p = (byte*)(void*)Scan0;
				byte* pSrc = (byte*)(void*)SrcScan0;
				int nOffset = stride - b.Width + 2;
				int nWidth = b.Width - 2;
				int nHeight = b.Height - 2;

				int nPixel;

				for (int y = 0; y < nHeight; ++y)
				{
					for (int x = 0; x < nWidth; ++x)
					{
						nPixel = (((Math.Abs(pSrc[0] - pSrc[1 + stride]) +
							Math.Abs(pSrc[1] - pSrc[1 + stride]) +
							Math.Abs(pSrc[2] - pSrc[1 + stride]) +
							Math.Abs(pSrc[0 + stride] - pSrc[1 + stride]) +
							Math.Abs(pSrc[1 + stride] - pSrc[1 + stride]) +
							Math.Abs(pSrc[2 + stride] - pSrc[1 + stride]) +
							Math.Abs(pSrc[0 + stride2] - pSrc[1 + stride]) +
							Math.Abs(pSrc[1 + stride2] - pSrc[1 + stride]) +
							Math.Abs(pSrc[2 + stride2] - pSrc[1 + stride]))
							/ 1));

						if (nPixel < 0) nPixel = 0;
						if (nPixel > 255) nPixel = 255;
						p[1 + stride] = (byte)nPixel;

						p += 1;
						pSrc += 1;
					}

					p += nOffset;
					pSrc += nOffset;
				}
			}

			b.UnlockBits(bmData);
			bSrc.UnlockBits(bmSrc);
			return true;
		}

		public static bool Emphasis(Bitmap b)
		{
			Bitmap bSrc = (Bitmap)b.Clone();
			BitmapData bmData = b.LockBits(new Rectangle(0, 0, b.Width, b.Height),
								ImageLockMode.ReadWrite,
								PixelFormat.Format8bppIndexed);
			BitmapData bmSrc = bSrc.LockBits(new Rectangle(0, 0, bSrc.Width, bSrc.Height),
							   ImageLockMode.ReadWrite,
							   PixelFormat.Format8bppIndexed);
			int stride = bmData.Stride;
			int stride2 = stride * 2;

			System.IntPtr Scan0 = bmData.Scan0;
			System.IntPtr SrcScan0 = bmSrc.Scan0;

			unsafe
			{
				byte* p = (byte*)(void*)Scan0;
				byte* pSrc = (byte*)(void*)SrcScan0;
				int nOffset = stride - b.Width + 2;
				int nWidth = b.Width - 2;
				int nHeight = b.Height - 2;

				int nPixel;

				for (int y = 0; y < nHeight; ++y)
				{
					for (int x = 0; x < nWidth; ++x)
					{
						nPixel = (((Math.Abs(pSrc[0] - pSrc[1 + stride]) +
							Math.Abs(pSrc[1] - pSrc[1 + stride]) +
							Math.Abs(pSrc[2] - pSrc[1 + stride]) +
							Math.Abs(pSrc[0 + stride] - pSrc[1 + stride]) +
							Math.Abs(pSrc[1 + stride] - pSrc[1 + stride]) +
							Math.Abs(pSrc[2 + stride] - pSrc[1 + stride]) +
							Math.Abs(pSrc[0 + stride2] - pSrc[1 + stride]) +
							Math.Abs(pSrc[1 + stride2] - pSrc[1 + stride]) +
							Math.Abs(pSrc[2 + stride2] - pSrc[1 + stride]))
							/ 3));

						if (nPixel < 0) nPixel = 0;
						if (nPixel > 255 - p[1 + stride]) nPixel = 255 - p[1 + stride];
						p[1 + stride] += (byte)nPixel;

						p += 1;
						pSrc += 1;
					}

					p += nOffset;
					pSrc += nOffset;
				}
			}

			b.UnlockBits(bmData);
			bSrc.UnlockBits(bmSrc);
			return true;
		}
	}

	public struct AlignMatch
	{
		public double matchRate;
		public MPoint matchPoint;
	}

	public class Align
	{
		private IntPtr handle;
		private Bitmap bitmap;
		private AlignMatch[] alignMatch;

		public Align(Bitmap bitmap)
		{
			this.bitmap = bitmap;
			BitmapData bd = bitmap.LockBits(new Rectangle(new Point(0, 0), bitmap.Size), ImageLockMode.ReadWrite, PixelFormat.Format8bppIndexed);
			MImage mimage = new MImage(bitmap.Width, bitmap.Height, 8, bd.Scan0);
			ImageLib.AlignCreate(out handle, mimage);
			bitmap.UnlockBits(bd);
		}
		~Align()
		{
			ImageLib.AlignClose(handle);
		}

		public int FindTemplate(Bitmap model, Bitmap target, Rectangle searchRect, double matchRate, out AlignMatch[] alignMatch)
		{
			BitmapData modelBd = model.LockBits(new Rectangle(new Point(0, 0), model.Size), ImageLockMode.ReadWrite, PixelFormat.Format8bppIndexed);
			BitmapData targetBd = target.LockBits(new Rectangle(new Point(0, 0), target.Size), ImageLockMode.ReadWrite, PixelFormat.Format8bppIndexed);
			MImage mmodel = new MImage(model.Width, model.Height, 8, modelBd.Scan0);
			MImage mtarget = new MImage(target.Width, target.Height, 8, targetBd.Scan0);
			MPoint findPoint = new MPoint(0, 0);

			int count = ImageLib.AlignFindTemplate(handle, mmodel, mtarget, new MRect(searchRect), 16, 16, 16, 16, matchRate);
			this.alignMatch = new AlignMatch[count];
			ImageLib.AlignGetMatchPoints(handle, this.alignMatch, count);
			alignMatch = this.alignMatch;
			model.UnlockBits(modelBd);
			target.UnlockBits(targetBd);
			return count;
		}
	}

	public class Blob
	{
		private IntPtr hBlob;
		private Bitmap bitmap;
		private Rectangle roi;
		public IntPtr Handle
		{
			get { return hBlob; }
		}

		public Blob(Bitmap bm)
		{
			bitmap = bm;
			BitmapData bd = bitmap.LockBits(new Rectangle(new Point(0, 0), bitmap.Size), ImageLockMode.ReadWrite, PixelFormat.Format8bppIndexed);
			MImage mimage = new MImage(bitmap.Width, bitmap.Height, 8, bd.Scan0);
			this.roi = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
			ImageLib.BlobCreate(out hBlob, mimage);
			bitmap.UnlockBits(bd);
		}

		public Blob(Bitmap bm, Rectangle roi)
		{
			bitmap = bm;
			BitmapData bd = bitmap.LockBits(new Rectangle(new Point(0, 0), bitmap.Size), ImageLockMode.ReadWrite, PixelFormat.Format8bppIndexed);
			MImage mimage = new MImage(bitmap.Width, bitmap.Height, 8, bd.Scan0);
			this.roi = roi;
			mimage.rectRoi = new MRect(roi);
			ImageLib.BlobCreate(out hBlob, mimage);
			bitmap.UnlockBits(bd);
		}

		~Blob()
		{
			ImageLib.BlobClose(hBlob);
		}

		public int Labeling(ref BLOBOPTION option, BLOBINFO[] pInfo)
		{
			int blobCount = 0;
			BitmapData bd = bitmap.LockBits(new Rectangle(new Point(0, 0), bitmap.Size), ImageLockMode.ReadWrite, PixelFormat.Format8bppIndexed);
			MImage mimage = new MImage(bitmap.Width, bitmap.Height, 8, bd.Scan0);
			mimage.rectRoi = new MRect(roi);
			blobCount = ImageLib.BlobLabeling(hBlob, mimage, ref option, pInfo);
			bitmap.UnlockBits(bd);
			return blobCount;
		}

		public int GetBlobNumber(Point pt)
		{
			int blobNumber = 0;
			MPoint mpt = new MPoint(pt);
			BitmapData bd = bitmap.LockBits(new Rectangle(new Point(0, 0), bitmap.Size), ImageLockMode.ReadWrite, PixelFormat.Format8bppIndexed);
			MImage mimage = new MImage(bitmap.Width, bitmap.Height, 8, bd.Scan0);
			mimage.rectRoi = new MRect(roi);
			blobNumber = ImageLib.BlobGetBlobNumber(hBlob, mimage, mpt);
			bitmap.UnlockBits(bd);
			return blobNumber;
		}

		public void FillLUTBlobs(byte[] fillLut)
		{
			BitmapData bd = bitmap.LockBits(new Rectangle(new Point(0, 0), bitmap.Size), ImageLockMode.ReadWrite, PixelFormat.Format8bppIndexed);
			MImage mimage = new MImage(bitmap.Width, bitmap.Height, 8, bd.Scan0);
			mimage.rectRoi = new MRect(roi);
			ImageLib.BlobFillLUTBlobs(hBlob, mimage, fillLut);
			bitmap.UnlockBits(bd);
		}

		public void GetVariance(BLOBINFO[] pInfo, int blobCount, out double averageX, out double averageY, out double varianceX, out double varianceY)
		{
			ImageLib.BlobGetVariance(hBlob, pInfo, blobCount, out averageX, out averageY, out varianceX, out varianceY);
		}

		public int GetBoundary(Bitmap bitmap, IntPtr pLabel, ref BLOBOPTION option, BLOBINFO[] pInfo, int nIdxBlob, MPoint[] pt)
		{
			int perimeter;
			BitmapData bd = bitmap.LockBits(new Rectangle(new Point(0, 0), bitmap.Size), ImageLockMode.ReadWrite, PixelFormat.Format8bppIndexed);
			MImage mimage = new MImage(bitmap.Width, bitmap.Height, 8, bd.Scan0);
			mimage.rectRoi = new MRect(roi);
			perimeter = ImageLib.GetBoundary(mimage, option.pLabeledDib, ref option, pInfo, nIdxBlob, pt);
			bitmap.UnlockBits(bd);
			return perimeter;
		}

		public int GetLargest()
		{
			return ImageLib.BlobGetLargest(hBlob);
		}
	}
}
