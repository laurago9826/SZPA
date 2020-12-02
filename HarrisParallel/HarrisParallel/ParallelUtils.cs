using Accord;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HarrisParallel
{
    class ParallelUtils
    {
        private readonly object lockObject = new object();
        private const int THREAD_NUM = 64;

        public List<IntPoint> FindCorners(Bitmap image, double threshold, int maxSuppressionWindowSize)
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

            int innerIterationMax = bmp.Width - radius;
            Parallel.For(radius, bmp.Height - radius, i => 
            {
                for (int j = radius; j < innerIterationMax; j++)
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
            });
            Marshal.Copy(data, 0, bmData.Scan0, data.Length);
            bmp.UnlockBits(bmData);
            image.UnlockBits(bmDataOrig);
            return bmp;
        }

        public static int[] CalculateSum(int[] array, int imgStride, int radius)
        {
            int[] data = new int[array.Length];
            int imgHeight = array.Length / imgStride;
            Parallel.For(radius, imgStride - radius, i =>
            {
                int tempSum = 0;
                Queue<int> previousRowSums = new Queue<int>();
                for (int start = 0; start < radius * 2; start++)
                {
                    int firstRowIdx = i;
                    int val = CalculateSumInRow(radius, array, firstRowIdx + start * imgStride);
                    previousRowSums.Enqueue(val);
                    tempSum += val;
                }
                for (int j = radius; j < imgHeight - radius; j++)
                {
                    int idx = (j * imgStride) + i;
                    int lastRowSum = CalculateSumInRow(radius, array, idx + radius * imgStride);
                    previousRowSums.Enqueue(lastRowSum);
                    tempSum += lastRowSum;
                    data[idx] = tempSum;
                    tempSum -= previousRowSums.Dequeue();
                }
            });
            return data;
        }
        private static int CalculateSumInRow(int windowRradius, int[] array, int forIndex)
        {
            int sum = 0;
            for (int l = -windowRradius; l <= windowRradius; l++)
            {
                sum += array[forIndex + l];
            }
            return sum;
        }

        public static double[] NonMaximumSuppression(double[] array, int imgStride, int size)
        {
            int radius = (size - 1) / 2;
            int imgHeight = array.Length / imgStride;
            double[] maxes = new double[array.Length];
            int rowsPerThread = (int)Math.Ceiling((double)imgHeight / size / THREAD_NUM);
            int actualThreadNum = (imgHeight / size) / rowsPerThread;
            Task[] tasks = new Task[actualThreadNum];
            for (int i = 0; i < actualThreadNum; i++)
            {
                int threadIdx = i;
                Task t = new Task(() =>
                {
                    for (int j = 0; j < rowsPerThread; j++)
                    {
                        int rowIdx = (threadIdx * rowsPerThread + j) * size + radius;
                        for (int k = radius; k < imgStride - radius; k += size)
                        {
                            int idx = (rowIdx * imgStride) + k;
                            int maxIdx = idx;
                            for (int l = -radius; l <= radius; l++)
                            {
                                for (int m = -radius; m <= radius; m++)
                                {
                                    int newIdx = idx + l * imgStride + m;
                                    if (array[newIdx] > array[maxIdx])
                                    {
                                        maxIdx = newIdx;
                                    }
                                }
                            }
                            maxes[maxIdx] = array[maxIdx];
                        }
                    }
                });
                t.Start();
                tasks[i] = t;
            }
            Task.WaitAll(tasks);
            return maxes;
        }

        public static double[] CalculateHarris(int[] x2sum, int[] y2sum, int[] xysum)
        {
            double k = 0.04;
            double[] harris = new double[x2sum.Length];
            Parallel.For(0, harris.Length, i =>
            {
                int trace = x2sum[i] + y2sum[i];
                harris[i] = (x2sum[i] * y2sum[i] - xysum[i] * xysum[i]) - k * trace * trace;
            });
            return harris;
        }

        public static int[] Multiply(byte[] array1, byte[] array2)
        {
            int[] multiplied = new int[array1.Length];
            Parallel.For(0, array1.Length, i =>
            {
                multiplied[i] = array1[i] * array2[i];
            });
            return multiplied;
        }

        public List<IntPoint> Threshold(double[] harris, int imgStride, double threshold)
        {
            HashSet<IntPoint> points = new HashSet<IntPoint>();
            int iterationPerThread = (int)Math.Ceiling((double)harris.Length / THREAD_NUM);
            int threadCount = Math.Min(THREAD_NUM, harris.Length);
            Task[] tasks = new Task[threadCount];
            for (int i = 0; i < threadCount; i++)
            {
                int threadIdx = i;
                Task t = new Task(() =>
                {
                    List<IntPoint> pointsPerThread = new List<IntPoint>();
                    int startIdx = threadIdx * iterationPerThread;
                    for (int j = startIdx; j < Math.Min(startIdx + iterationPerThread, harris.Length); j++)
                    {
                        if (harris[j] >= threshold)
                        {
                            IntPoint point = new IntPoint();
                            point.X = j % imgStride;
                            point.Y = j / imgStride;
                            pointsPerThread.Add(point);
                        }
                    }
                    lock (lockObject)
                    {
                        pointsPerThread.ForEach(x => points.Add(x));
                    }
                });
                t.Start();
                tasks[i] = t;
            }
            Task.WaitAll(tasks);
            return points.ToList();
        }

    }
}
