using Accord;
using Accord.Imaging;
using Accord.Imaging.Filters;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace HarrisParallel
{
    class Program
    {

        static void Main(string[] args)
        {
            Bitmap orig = new Bitmap(@"C:\Users\Hp Probook 440 G5\Downloads\image2.jpg");
            Bitmap img = Grayscale.CommonAlgorithms.BT709.Apply(orig);
            double threshold = 20000;
            int winSize = 11;
            List<IntPoint> pointsSeq = HarrisSequential(img, threshold, winSize);
            List<IntPoint> pointsParallel = HarrisParallel(img, threshold, winSize);
            
            //Utils.MarkPoints(SequentialUtils.ApplyFilter(filterX, img, 1), pointsSeq, Color.Green).Save(@"C:\Users\Hp Probook 440 G5\Downloads\sequential.jpg", ImageFormat.Jpeg);
            Utils.MarkPoints(orig, pointsSeq, Color.White).Save(@"C:\Users\Hp Probook 440 G5\Downloads\sequential.jpg", ImageFormat.Jpeg);
            Utils.MarkPoints(orig, pointsParallel, Color.White).Save(@"C:\Users\Hp Probook 440 G5\Downloads\parallel.jpg", ImageFormat.Jpeg);
            Console.ReadLine();
        }

        public static List<IntPoint> HarrisSequential(Bitmap bmp, double threshold, int maxSuppWindowSize)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            List<IntPoint> points = SequentialUtils.FindCorners(bmp, threshold, maxSuppWindowSize);
            sw.Stop();
            Console.WriteLine("SEQUENTIAL: " + sw.ElapsedMilliseconds);
            return points;
        }

        public static List<IntPoint> HarrisParallel(Bitmap bmp, double threshold, int maxSuppWindowSize)
        {
            Stopwatch sw = new Stopwatch();
            ParallelUtils par = new ParallelUtils();
            sw.Start();
            List<IntPoint> points = par.FindCorners(bmp, threshold, maxSuppWindowSize);
            sw.Stop();
            Console.WriteLine("PARALLEL: " + sw.ElapsedMilliseconds);
            return points;
        }
    }
}
