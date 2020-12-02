using Accord;
using Accord.Imaging.Filters;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HarrisParallel
{
    [TestFixture]
    public class Test
    {
        [Test]
        public void TestHarrisResult()
        {
            Bitmap orig = new Bitmap(@"C:\Users\Hp Probook 440 G5\Documents\kepek\custom2.jpg");
            Bitmap img = Grayscale.CommonAlgorithms.BT709.Apply(orig);
            double threshold = 20000;
            int winSize = 3;
            //TEST CONVOLVE METHOD
            Bitmap s_smoothed = SequentialUtils.Convolve(Utils.gaussian_operator, img, 16);
            Bitmap s_dx = SequentialUtils.Convolve(Utils.dx_operator, s_smoothed, 1);
            Bitmap s_dy = SequentialUtils.Convolve(Utils.dy_operator, s_smoothed, 1);
            byte[] s_x = Utils.ConvertToByteArray(s_dx);
            byte[] s_y = Utils.ConvertToByteArray(s_dy);
            Bitmap p_smoothed = ParallelUtils.Convolve(Utils.gaussian_operator, img, 16);
            Bitmap p_dx = ParallelUtils.Convolve(Utils.dx_operator, p_smoothed, 1);
            Bitmap p_dy = ParallelUtils.Convolve(Utils.dy_operator, p_smoothed, 1);
            byte[] p_x = Utils.ConvertToByteArray(p_dx);
            byte[] p_y = Utils.ConvertToByteArray(p_dy);
            CollectionAssert.AreEqual(s_x, p_x);
            CollectionAssert.AreEqual(s_y, p_y);
            //TEST MULTIPLY METHOD
            int[] s_xy = SequentialUtils.Multiply(s_x, s_y);
            int[] s_x2 = SequentialUtils.Multiply(s_x, s_x);
            int[] s_y2 = SequentialUtils.Multiply(s_y, s_y);
            int[] p_xy = ParallelUtils.Multiply(p_x, p_y);
            int[] p_x2 = ParallelUtils.Multiply(p_x, p_x);
            int[] p_y2 = ParallelUtils.Multiply(p_y, p_y);
            CollectionAssert.AreEqual(s_xy, p_xy);
            CollectionAssert.AreEqual(s_x2, p_x2);
            CollectionAssert.AreEqual(s_y2, p_y2);
            //TEST SUM CALCULATION
            int imgStride = Utils.GetStride(s_smoothed);
            int imgWidth = s_smoothed.Width;
            int imgHeight = s_smoothed.Height;
            int[] s_sumy2 = SequentialUtils.CalculateSum(s_y2, imgStride, 1);
            int[] s_sumx2 = SequentialUtils.CalculateSum(s_x2, imgStride, 1);
            int[] s_sumxy = SequentialUtils.CalculateSum(s_xy, imgStride, 1);
            int[] p_sumy2 = ParallelUtils.CalculateSum(p_y2, imgStride, 1);
            int[] p_sumx2 = ParallelUtils.CalculateSum(p_x2, imgStride, 1);
            int[] p_sumxy = ParallelUtils.CalculateSum(p_xy, imgStride, 1);
            CollectionAssert.AreEqual(s_sumxy, p_sumxy);
            CollectionAssert.AreEqual(s_sumx2, p_sumx2);
            CollectionAssert.AreEqual(s_sumy2, p_sumy2);
            //TEST CORNER STRENGTH VALUES
            double[] s_harris = SequentialUtils.CalculateHarris(s_sumx2, s_sumy2, s_sumxy);
            double[] p_harris = ParallelUtils.CalculateHarris(p_sumx2, p_sumy2, p_sumxy);
            CollectionAssert.AreEqual(s_harris, p_harris);
            //TEST NON MAXIMUM SUPPRESSION
            double[] s_maxes = SequentialUtils.NonMaximumSuppression(s_harris, imgStride, winSize);
            double[] p_maxes = ParallelUtils.NonMaximumSuppression(p_harris, imgStride, winSize);
            CollectionAssert.AreEqual(s_maxes, p_maxes);
            //TEST THRESHOLD
            List<IntPoint> s_result = SequentialUtils.Threshold(s_maxes, imgStride, threshold);
            ParallelUtils par = new ParallelUtils();
            List<IntPoint> p_result = par.Threshold(p_maxes, imgStride, threshold);
            CollectionAssert.AreEquivalent(s_result, p_result);
        }

        [Test]
        public void TestCalculateSum()
        {
            int[] array =
                { 1,2,3,4,
                  2,3,4,5,
                  3,4,5,6,
                  4,5,6,7};
            int[] output =
                { 0,0,0,0,
                  0,27,36,0,
                  0,36,45,0,
                  0,0,0,0};
            int imgStride = 4;
            int[] s_sum = SequentialUtils.CalculateSum(array, imgStride, 1);
            CollectionAssert.AreEqual(s_sum, output);
            int[] p_sum = ParallelUtils.CalculateSum(array, imgStride, 1);
            CollectionAssert.AreEqual(p_sum, output);
        }

        [Test]
        public void TestThreshold()
        {
            double[] array = { 1, 2, 3, 4, 5, 6 };
            int threshold = 3;
            int imgStride = 3;
            ParallelUtils par = new ParallelUtils();
            List<IntPoint> s_result = SequentialUtils.Threshold(array, imgStride, threshold);
            List<IntPoint> p_result = par.Threshold(array, imgStride, threshold);
            CollectionAssert.AreEquivalent(s_result, p_result);
        }

        [Test]
        public void TestNonMaximumSuppression()
        {
            double[] array = { 1,2,3,2,1,
                               2,2,4,5,1,
                               1,2,3,4,6,
                               3,4,5,6,7};
            int imgWidth = 5;
            int imgStride = 5;
            int imgHeight = 4;
            double[] s_maxes = SequentialUtils.NonMaximumSuppression(array, imgStride, 3);
            double[] p_maxes = ParallelUtils.NonMaximumSuppression(array, imgStride, 3);
            CollectionAssert.AreEqual(s_maxes, p_maxes);
        }
    }
}
