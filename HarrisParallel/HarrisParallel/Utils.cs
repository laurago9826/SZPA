using Accord;
using Accord.Imaging.Filters;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace HarrisParallel
{
    public class Utils
    {
        public static int[,] dx_operator = { { 1, 0, -1 }, { 2, 0, -2 }, { 1, 0, -1 } };
        public static int[,] dy_operator = { { 1, 2, 1 }, { 0, 0, 0 }, { -1, -2, -1 } };
        public static int[,] gaussian_operator = { { 1, 2, 1 }, { 2, 4, 2 }, { 1, 2, 1 } };

        public static Bitmap MarkPoints(Bitmap bmp, List<IntPoint> points, Color color)
        {
            PointsMarker pm = new PointsMarker(points, color);
            return pm.Apply(bmp);
        }

        public static byte[] ConvertToByteArray(Bitmap bmp)
        {
            Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            BitmapData bmpData = bmp.LockBits(rect, ImageLockMode.ReadOnly, bmp.PixelFormat);
            IntPtr Scan0 = bmpData.Scan0;
            int bytes = Math.Abs(bmpData.Stride) * bmp.Height;
            byte[] data = new byte[bytes];
            Marshal.Copy(Scan0, data, 0, bytes);
            bmp.UnlockBits(bmpData);
            return data;
        }


        public static Bitmap Copy(Bitmap bmp)
        {
            return bmp.Clone(new Rectangle(0, 0, bmp.Width, bmp.Height), bmp.PixelFormat);
        }

        public static int GetStride(Bitmap bmp)
        {
            Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            BitmapData bmpData = bmp.LockBits(rect, ImageLockMode.ReadOnly, bmp.PixelFormat);
            int stride = Math.Abs(bmpData.Stride);
            bmp.UnlockBits(bmpData);
            return stride;
        }
    }
}
