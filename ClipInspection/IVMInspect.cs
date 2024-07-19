using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;
using System.Drawing;

namespace IVMCommon
{
    class MInspect
    {
        [DllImport("IVMInspect.dll")]
        public static extern int WasherFindOutRect(MImage bitmap, int threshold, out MRect rect);
		[DllImport("IVMInspect.dll")]
        public static extern int WasherFindInnerRect(MImage bitmap, int threshold, MPoint centerPoint, ref MRect rect);
		[DllImport("IVMInspect.dll")]
        public static extern int MakeRegion(MImage target, ref MRect outterRect, ref MRect innerRect);

		[DllImport("IVMInspect.dll")]
        public static extern int WasherThreshold(MImage bitmap, MImage pMask, int nLowThreshold, int nHighThreshold);

		[DllImport("IVMInspect.dll")]
        public static extern int WasherThresholdWhite(MImage bitmap, MImage pDest, MImage pMask, int nHighThreshold);

		[DllImport("IVMInspect.dll")]
        public static extern int WasherThresholdBlack(MImage bitmap, MImage pDest, MImage pMask, int nLowThreshold);
		[DllImport("IVMInspect.dll")]
        public static extern int Fiter(MImage bitmap, MImage target);

		[DllImport("IVMInspect.dll")]
        public static extern int Closing(MImage bitmap, MImage target, int nTime);

    }
    static class WasherInspect
    {
        public static void Filter(Bitmap bitmap, Bitmap target)
        {
            BitmapData bd = bitmap.LockBits(new Rectangle(new Point(0, 0), bitmap.Size), ImageLockMode.ReadWrite, PixelFormat.Format8bppIndexed);
            BitmapData bdTgt = target.LockBits(new Rectangle(new Point(0, 0), bitmap.Size), ImageLockMode.ReadWrite, PixelFormat.Format8bppIndexed);
			MImage mimage = new MImage(bitmap.Width, bitmap.Height, 8, bd.Scan0);
			MImage timage = new MImage(bitmap.Width, bitmap.Height, 8, bdTgt.Scan0);
            MInspect.Fiter(mimage, timage);

            bitmap.UnlockBits(bd);
            target.UnlockBits(bdTgt);
        }

        public static void FindOutRect(Bitmap bitmap, int threshold, out Rectangle rect)
        {
            MRect moutRect = new MRect();
            BitmapData bd = bitmap.LockBits(new Rectangle(new Point(0, 0), bitmap.Size), ImageLockMode.ReadWrite, PixelFormat.Format8bppIndexed);
			MImage mimage = new MImage(bitmap.Width, bitmap.Height - 1, 8, bd.Scan0);
           // mimage.rectRoi = new MRect(new Rectangle(10, 10, bitmap.Width-20, bitmap.Height-20));
            try
            {
                MInspect.WasherFindOutRect(mimage, threshold, out moutRect);
            }
            catch
            {
            }

            rect = moutRect.Rect;
            bitmap.UnlockBits(bd);
        }

        public static void FindInRect(Bitmap bitmap, int threshold, Point centerPoint, ref Rectangle rect)
        {
            MRect moutRect = new MRect(rect);
            MPoint mcenterPt = new MPoint(centerPoint);
            BitmapData bd = bitmap.LockBits(new Rectangle(new Point(0, 0), bitmap.Size), ImageLockMode.ReadWrite, PixelFormat.Format8bppIndexed);
			MImage mimage = new MImage(bitmap.Width, bitmap.Height, 8, bd.Scan0);
			//            mimage.rectRoi = new MRect(roiRect);
            MInspect.WasherFindInnerRect(mimage, threshold, mcenterPt, ref moutRect);

            rect = moutRect.Rect;
            bitmap.UnlockBits(bd);
        }
        public static void MakeRegion(Bitmap bitmap, Rectangle outterRect, Rectangle innerRect)
        {
            MRect moutRect = new MRect(outterRect);
            MRect minRect = new MRect(innerRect);

            BitmapData bd = bitmap.LockBits(new Rectangle(new Point(0, 0), bitmap.Size), ImageLockMode.ReadWrite, PixelFormat.Format8bppIndexed);
			MImage mimage = new MImage(bitmap.Width, bitmap.Height, 8, bd.Scan0);
			//            mimage.rectRoi = new MRect(roiRect);
            MInspect.MakeRegion(mimage, ref moutRect, ref minRect);

            bitmap.UnlockBits(bd);
        }

        public static void Closing(Bitmap bitmap, Bitmap tgt, int nTime)
        {
            BitmapData bd = bitmap.LockBits(new Rectangle(new Point(0, 0), bitmap.Size), ImageLockMode.ReadWrite, PixelFormat.Format8bppIndexed);
//            BitmapData bdTgt = tgt.LockBits(new Rectangle(new Point(0, 0), tgt.Size), ImageLockMode.ReadWrite, PixelFormat.Format8bppIndexed);
			MImage mimage = new MImage(bitmap.Width, bitmap.Height, 8, bd.Scan0);
			//          MImage mimageMask = new MImage(tgt.Width, tgt.Height, 8, bdTgt.Scan0);
			//            mimage.rectRoi = new MRect(roiRect);
            MInspect.Closing(mimage, mimage, nTime);

//            tgt.UnlockBits(bdTgt);
            bitmap.UnlockBits(bd);
        }

        public static void Threshold(Bitmap bitmap, Bitmap mask, int lowThreshold, int highThreshold)
        {
            BitmapData bd = bitmap.LockBits(new Rectangle(new Point(0, 0), bitmap.Size), ImageLockMode.ReadWrite, PixelFormat.Format8bppIndexed);
            BitmapData bdMask = mask.LockBits(new Rectangle(new Point(0, 0), mask.Size), ImageLockMode.ReadWrite, PixelFormat.Format8bppIndexed);
			MImage mimage = new MImage(bitmap.Width, bitmap.Height, 8, bd.Scan0);
			MImage mimageMask = new MImage(mask.Width, mask.Height, 8, bdMask.Scan0);
			//            mimage.rectRoi = new MRect(roiRect);
            MInspect.WasherThreshold(mimage, mimageMask, lowThreshold, highThreshold);

            mask.UnlockBits(bdMask);
            bitmap.UnlockBits(bd);
        }

        public static void Threshold(Bitmap bitmap, Bitmap dest, Bitmap mask, int highThreshold)
        {
            BitmapData bd = bitmap.LockBits(new Rectangle(new Point(0, 0), bitmap.Size), ImageLockMode.ReadWrite, PixelFormat.Format8bppIndexed);
            BitmapData bdDest = dest.LockBits(new Rectangle(new Point(0, 0), dest.Size), ImageLockMode.ReadWrite, PixelFormat.Format8bppIndexed);
            BitmapData bdMask = mask.LockBits(new Rectangle(new Point(0, 0), mask.Size), ImageLockMode.ReadWrite, PixelFormat.Format8bppIndexed);
			MImage mimage = new MImage(bitmap.Width, bitmap.Height, 8, bd.Scan0);
			MImage mimageDest = new MImage(dest.Width, dest.Height, 8, bdDest.Scan0);
			MImage mimageMask = new MImage(mask.Width, mask.Height, 8, bdMask.Scan0);
            //            mimage.rectRoi = new MRect(roiRect);
            MInspect.WasherThresholdWhite(mimage, mimageDest, mimageMask, highThreshold);

            mask.UnlockBits(bdMask);
            dest.UnlockBits(bdDest);
            bitmap.UnlockBits(bd);
        }

        public static void ThresholdInverse(Bitmap bitmap, Bitmap dest, Bitmap mask, int lowThreshold)
        {
            BitmapData bd = bitmap.LockBits(new Rectangle(new Point(0, 0), bitmap.Size), ImageLockMode.ReadWrite, PixelFormat.Format8bppIndexed);
            BitmapData bdDest = dest.LockBits(new Rectangle(new Point(0, 0), dest.Size), ImageLockMode.ReadWrite, PixelFormat.Format8bppIndexed);
            BitmapData bdMask = mask.LockBits(new Rectangle(new Point(0, 0), mask.Size), ImageLockMode.ReadWrite, PixelFormat.Format8bppIndexed);
			MImage mimage = new MImage(bitmap.Width, bitmap.Height, 8, bd.Scan0);
			MImage mimageDest = new MImage(dest.Width, dest.Height, 8, bdDest.Scan0);
			MImage mimageMask = new MImage(mask.Width, mask.Height, 8, bdMask.Scan0);
			//            mimage.rectRoi = new MRect(roiRect);
            MInspect.WasherThresholdBlack(mimage, mimageDest, mimageMask, lowThreshold);

            mask.UnlockBits(bdMask);
            dest.UnlockBits(bdDest);
            bitmap.UnlockBits(bd);
        }

    }
}
