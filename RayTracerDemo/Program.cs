using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System.Buffers;

namespace RayTracerDemo
{
    class Program
    {
        static WriteableBitmap writeableBitmap;
        static Window w;
        static Image i;

        [STAThread]
        static void Main(string[] args)
        {
            i = new Image();
            RenderOptions.SetBitmapScalingMode(i, BitmapScalingMode.NearestNeighbor);
            RenderOptions.SetEdgeMode(i, EdgeMode.Aliased);

            w = new Window();
            w.Width = 600;
            w.Height = 600;
            w.Content = i;
            w.Show();

            writeableBitmap = new WriteableBitmap(
                (int)w.ActualWidth,
                (int)w.ActualHeight,
                96,
                96,
                PixelFormats.Bgr32,
                null);

            i.Source = writeableBitmap;

            i.Stretch = Stretch.None;
            i.HorizontalAlignment = HorizontalAlignment.Left;
            i.VerticalAlignment = VerticalAlignment.Top;

            Application app = new Application();

            Task.Run(() => RenderLoop());
            app.Run();
        }

        static void RenderLoop()
        {
            IntPtr pBackBuffer = default;
            int backBufferStride = 0;
            int pixelHeight = 0;

            RayTracer rayTracer = new RayTracer(600, 600);

            for (;;)
            {
                for (double x = 1; x < 360; x += 5)
                {
                    Thread.Sleep(10);

                    // Reserve the back buffer for updates.
                    writeableBitmap.Dispatcher.Invoke(() =>
                    {
                        writeableBitmap.Lock();
                        pBackBuffer = writeableBitmap.BackBuffer;
                        backBufferStride = writeableBitmap.BackBufferStride;
                        pixelHeight = writeableBitmap.PixelHeight;
                    });

                    var sw = System.Diagnostics.Stopwatch.StartNew();

                    using (var frameBuffer = new NativeBuffer(backBufferStride * pixelHeight, pBackBuffer))
                    {
                        var scene = rayTracer.DefaultScene(x);
                        rayTracer.Render(scene, frameBuffer, backBufferStride);
                    }

                    sw.Stop();
                    ReportTime(sw.ElapsedMilliseconds);

                    // Release the back buffer and make it available for display.
                    writeableBitmap.Dispatcher.Invoke(() =>
                    {
                        // Specify the area of the bitmap that changed.
                        writeableBitmap.AddDirtyRect(new Int32Rect(0, 0, 600, 600));
                        writeableBitmap.Unlock();
                    });
                }
            }
        }

        private static readonly Queue<long> times = new Queue<long>();

        private static void ReportTime(long msec)
        {
            times.Enqueue(msec);
            if (times.Count > 10)
            {
                times.Dequeue();
            }

            long sum = 0;
            foreach (var t in times)
            {
                sum += t;
            }

            System.Console.WriteLine(sum / times.Count);
        }
    }
}