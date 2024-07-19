using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Drawing.Imaging;

namespace IVMCommon
{
    public partial class CImageViewer : UserControl
    {
        private Bitmap image = null;
		private int imageWidth = 0;
		private int imageHeight = 0;
		private Rectangle rectScreen = new Rectangle();
        byte[] mpalette = null;

		Mutex refreshMutex = new Mutex();

        public double ZoomX
        {
			get { return (double)rectScreen.Width / (double)ClientRectangle.Width; }
        }

        public double ZoomY
        {
			get { return (double)rectScreen.Height / (double)ClientRectangle.Height; }
        }

        public Point RealToScreen(Point realPoint)
        {
			return new Point((int)((realPoint.X - rectScreen.X) / ZoomX),
				(int)((realPoint.Y - rectScreen.Y) / ZoomY));
        }

		public Point RealToScreen(PointF realPoint)
		{
			return new Point((int)((realPoint.X - rectScreen.X) / ZoomX),
				(int)((realPoint.Y - rectScreen.Y) / ZoomY));
		}

        public Size RealToScreen(Size realSize)
        {
            return new Size((int)(realSize.Width / ZoomX),
                (int)(realSize.Height / ZoomY));
        }

        public Rectangle RealToScreen(Rectangle realRect)
        {
            return new Rectangle(RealToScreen(realRect.Location), RealToScreen(realRect.Size));
        }

		public Point ScreenToReal(Point screenPoint)
		{
			return new Point((int)(((double)screenPoint.X * ZoomX) + rectScreen.X),
				(int)(((double)screenPoint.Y * ZoomY) + rectScreen.Y));
		}

		public Size ScreenToReal(Size screenSize)
		{
			return new Size((int)(screenSize.Width * ZoomX),
				(int)(screenSize.Height * ZoomY));
		}

		public Rectangle ScreenToReal(Rectangle screenRect)
		{
			return new Rectangle(ScreenToReal(screenRect.Location), ScreenToReal(screenRect.Size));
		}


        /// <summary>
        /// 현재 마우스 위치를 영상에서의 위치로 표현한다.
        /// </summary>
        public Point RealMousePosition
        {
            get
            {
                Point clientPosition = PointToClient(Control.MousePosition);
				return new Point((int)Math.Round(clientPosition.X* ZoomX, 0) + rectScreen.X,
					(int)Math.Round(clientPosition.Y * ZoomY, 0) + rectScreen.Y);
            }
        }

		public Point GetRealMousePosition(int X, int Y)
		{
			return new Point((int)Math.Round(X * ZoomX) + rectScreen.X,
				(int)Math.Round(Y * ZoomY) + rectScreen.Y);
		}

		public Rectangle ScreenRectangle
		{
			get { return rectScreen; }
			set { rectScreen = value; }
		}

        public CImageViewer()
        {
            InitializeComponent();

            mpalette = new byte[1024];
            int index = 0;
            for (int i = 0; i < 256; i++)
            {
                mpalette[index++] = (byte)i; // B
                mpalette[index++] = (byte)i; // G
                mpalette[index++] = (byte)i; // R
                mpalette[index++] = 0;
            }
        }

        public void ChangePalette(Color[] color)
        {
			if (image == null)
				return;
            //Image myImage = (Image)this.image;
            //ColorPalette palette = myImage.Palette;
			if (image.PixelFormat == PixelFormat.Format8bppIndexed)
			{
				ColorPalette palette = image.Palette;
				int index = 0;
				for (int i = 0; i < 256; i++)
				{
					palette.Entries[i] = color[i];
					mpalette[index++] = color[i].B;
					mpalette[index++] = color[i].G;
					mpalette[index++] = color[i].R;
					mpalette[index++] = 0;
				}
				image.Palette = palette;
			}
        }

		public Bitmap Image
		{
			get { return image; }
			set
			{
				image = value;
				if (image == null)
				{
					imageWidth = 0;
					imageHeight = 0;
					rectScreen = new Rectangle();
				}
				else
				{
					if (imageWidth != image.Width || imageHeight != image.Height)
					{
						imageWidth = image.Width;
						imageHeight = image.Height;
						rectScreen = new Rectangle(0, 0, imageWidth, imageHeight);
					}
				}
			}
		}

		public void FullImageView()
		{
			if (image != null)
				rectScreen = new Rectangle(0, 0, imageWidth, imageHeight);
		}

		private void ImageViewer_Load(object sender, EventArgs e)
		{
			//this.MouseWheel += new MouseEventHandler(CtlImageViewer_MouseWheel);
		}

		void CtlImageViewer_MouseWheel(object sender, MouseEventArgs e)
		{
			int count = e.Delta / 120;

			if (Control.ModifierKeys == Keys.Shift)
			{
				rectScreen.Y -= count * 300;
				rectScreen.Height += count * 600;
				if (rectScreen.Y < 0)
					rectScreen.Y = 0;
				if (rectScreen.Bottom > imageHeight)
					rectScreen.Height = imageHeight - rectScreen.Y;
				if (ZoomY < ZoomX && count < 0)
				{	// 이전 값으로 되돌리지 말고 정확히 가로세로 1:1 비율로 만들어버리자.
					//rectScreen.Y += count * 300;
					//rectScreen.Height -= count * 600;
					rectScreen.Y += (int)((ZoomY - ZoomX) * (double)ClientRectangle.Height / 2.0);
					rectScreen.Height -= (int)((ZoomY - ZoomX) * (double)ClientRectangle.Height);
				}
			}
			else
			{
				rectScreen.Y -= count * 300;
				if (rectScreen.Y < 0)
				{
					rectScreen.Y = 0;
				}
				if (rectScreen.Bottom > imageHeight)
				{
					rectScreen.Y += imageHeight - rectScreen.Bottom;
				}
			}
			Invalidate();
		}

 		private void ImageViewer_Paint(object sender, PaintEventArgs e)
		{
			try
			{
				refreshMutex.WaitOne();
				// 그리기 모드를 선택한다.
				Graphics g = e.Graphics;

				if (image != null)
				{
					if (image.GetType() == typeof(Bitmap))
					{
						//g.DrawImage((Bitmap)image, dcRect, bitmapRect, GraphicsUnit.Pixel);
						lock (image)
						{
							Bitmap bitmap = (Bitmap)image;
							if (bitmap.PixelFormat == PixelFormat.Format24bppRgb)
							{
								g.DrawImage(bitmap, ClientRectangle, rectScreen, GraphicsUnit.Pixel);
							}
							else
							{
								BitmapData bd = bitmap.LockBits(new Rectangle(new Point(0, 0), bitmap.Size), ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed);
								MImage mimage = new MImage(bitmap.Width, bitmap.Height, 8, bd.Scan0);

								mimage.Draw(g, ClientRectangle, rectScreen, mpalette);

								bitmap.UnlockBits(bd);
							}
						}
					}
				}
			}
			catch(Exception exc)
			{
				Log.AddLogMessage(Log.LogType.ERROR, 0, "[ImageViewer_Paint] " + exc.Message);
			}
			finally
			{
				refreshMutex.ReleaseMutex();
			}
		}
	}
}
