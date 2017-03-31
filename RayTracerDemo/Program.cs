using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;

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

        // The DrawPixel method updates the WriteableBitmap by using
        // unsafe code to write a pixel into the back buffer.
        static void DrawPixel(IntPtr pBackBuffer, int backBufferStride, int x, int y, RGB c)
        {
            unsafe
            {
                // Find the address of the pixel to draw.
                pBackBuffer += y * backBufferStride;
                pBackBuffer += x * 4;

                // Compute the pixel's color.
                int color_data = c.R << 16; // R
                color_data |= c.G << 8;   // G
                color_data |= c.B << 0;   // B

                // Assign the color data to the pixel.
                *((int*)pBackBuffer) = color_data;
            }
        }

        static void RenderLoop()
        {
            IntPtr pBackBuffer = default(IntPtr);
            int backBufferStride = 0;


            RayTracer rayTracer = new RayTracer(600, 600, (int x, int y, RGB color) =>
            {
                DrawPixel(pBackBuffer, backBufferStride, x, y, color);
            });

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
                    });

                    var sw = System.Diagnostics.Stopwatch.StartNew();

                    rayTracer.Render(rayTracer.DefaultScene(x));

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