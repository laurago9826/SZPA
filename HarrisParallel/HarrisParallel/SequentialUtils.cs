using Accord;
using Accord.Imaging.Filters;
using Accord.Statistics.Kernels;
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
    class SequentialUtils
    {
        public static List<IntPoint> FindCorners(Bitmap image, double threshold, int maxSuppressionWindowSize)
        {
            Bitmap smoothed = Convolve(Utils.gaussian_operator, image, 16);
            Bitmap dx = Convolve(Utils.dx_operator, smoothed, 1);
            Bitmap dy = Convolve(Utils.dy_operator, smoothed, 1);
            byte[] x = Utils.ConvertToByteArray(dx);
            byte[] y = Utils.ConvertToByteArray(dy);
            int[] xy = Multiply(x, y);
            int[] x2 = Multiply(x, x);
            int[] y2 = Multiply(y, y);
            int imgStride = Utils.GetStride(dy);
            int[] sumy2 = CalculateSum(y2, imgStride, 1);
            int[] sumx2 = CalculateSum(x2, imgStride, 1);
            int[] sumxy = CalculateSum(xy, imgStride, 1);
            double[] harris = CalculateHarris(sumx2, sumy2, sumxy);
            double[] maxes = NonMaximumSuppression(harris, imgStride, maxSuppressionWindowSize);
            return Threshold(maxes, imgStride, threshold);
        }

        public static Bitmap Convolve(int[,] filter, Bitmap image, int windowWeight)
        {
            int radius = (filter.GetLength(0) - 1) / 2;
            int size = windowWeight;
            Bitmap bmp = Utils.Copy(image);
            Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            BitmapData bmData = bmp.LockBits(rect, ImageLockMode.ReadWrite, bmp.PixelFormat);
            BitmapData bmDataOrig = image.LockBits(rect, ImageLockMode.ReadWrite, bmp.PixelFormat);
            int stride = bmDataOrig.Stride;
            System.IntPtr Scan0_orig = bmDataOrig.Scan0;
            byte[] data_orig = new byte[Math.Abs(stride * bmDataOrig.Height)];
            Marshal.Copy(Scan0_orig, data_orig, 0, data_orig.Length);
            byte[] data = new byte[Math.Abs(stride * bmDataOrig.Height)];

            for (int i = radius; i < bmp.Height - radius; i++)
            {
                for (int j = radius; j < bmp.Width - radius; j++)
                {
                    int sum = 0;
                    int idx = (i * stride) + j;
                    for (int k = -radius; k <= radius; k++)
                    {
                        for (int l = -radius; l <= radius; l++)
                        {
                            sum += data_orig[idx + k * stride + l] * filter[radius + k, radius + l];
                        }
                    }
                    byte val = (byte)Math.Max(Math.Min((sum / size), 255), 0);
                    data[idx] = val;
                }
            }
            Marshal.Copy(data, 0, bmData.Scan0, data.Length);
            bmp.UnlockBits(bmData);
            image.UnlockBits(bmDataOrig);
            return bmp;
        }

        public static int[] CalculateSum(int[] array, int imgStride, int windowRradius)
        {
            int[] data = new int[array.Length];
            int imgHeight = array.Length / imgStride;
            for (int i = windowRradius; i < imgHeight - windowRradius; i++)
            {
                for (int j = windowRradius; j < imgStride - windowRradius; j++)
                {
                    int sum = 0;
                    int idx = (i * imgStride) + j;
                    for (int k = -windowRradius; k <= windowRradius; k++)
                    {
                        for (int l = -windowRradius; l <= windowRradius; l++)
                        {
                            sum += array[idx + k * imgStride + l];
                        }
                    }
                    data[idx] = sum;
                }
            }
            return data;
        }

        public static double[] NonMaximumSuppression(double[] array, int imgStride, int size)
        {
            int windowRradius = (size - 1) / 2;
            int imgHeight = array.Length / imgStride;
            double[] maxes = new double[array.Length];
            for (int i = windowRradius; i < imgHeight - windowRradius; i+= size)
            {
                for (int j = windowRradius; j < imgStride - windowRradius; j+= size)
                {
                    int idx = (i * imgStride) + j;
                    int maxIdx = idx;
                    for (int k = -windowRradius; k <= windowRradius; k++)
                    {
                        for (int l = -windowRradius; l <= windowRradius; l++)
                        {
                            int newIdx = idx + k * imgStride + l;
                            if (array[newIdx] > array[maxIdx])
                            {
                                maxIdx = newIdx;
                            }
                        }
                    }
                    maxes[maxIdx] = array[maxIdx];
                }
            }
            return maxes;
        }

        public static double[] CalculateHarris(int[] x2sum, int[] y2sum, int[] xysum)
        {
            double k = 0.04;
            double[] harris = new double[x2sum.Length];
            for (int i = 0; i < harris.Length; i++)
            {
                harris[i] = (x2sum[i] * y2sum[i] - xysum[i] * xysum[i]) - k * (x2sum[i] + y2sum[i]) * (x2sum[i] + y2sum[i]);
            }
            return harris;
        }

        public static int[] Multiply(byte[] array1, byte[] array2)
        {
            int[] multiplied = new int[array1.Length];
            for (int i = 0; i < array1.Length; i++)
            {
                multiplied[i] = array1[i] * array2[i];
            }
            return multiplied;
        }

        public static List<IntPoint> Threshold(double[] harris, int imgStride, double threshold)
        {
            List<IntPoint> points = new List<IntPoint>();
            for (int i = 0; i < harris.Length; i++)
            {
                if (harris[i] >= threshold)
                {
                    IntPoint point = new IntPoint();
                    point.X = i % imgStride ;
                    point.Y = i / imgStride;
                    points.Add(point);
                }
            }
            return points;
        }
    }
}
