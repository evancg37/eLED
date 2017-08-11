using System;
using System.Threading;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows;

namespace eLED
{
    class ScreenMatcher
    {
        int frequency = 24;
        public int color = 0;
        Bitmap capture;
        Graphics g;
        bool running = true;

        public ScreenMatcher()
        {
            capture = new Bitmap((int) SystemParameters.VirtualScreenWidth, (int) SystemParameters.VirtualScreenHeight);
            g = Graphics.FromImage(capture);
        }

        public void startWatching()
        {
            int avgB, avgG, avgR;
            BitmapData srcData;

            int stride;
            IntPtr Scan0;
            long[] totals;

            int width, height, idx;

            while (running)
            {
                totals = new long[] { 0, 0, 0 };

                Thread.Sleep(1000/frequency);
                g.CopyFromScreen(0, 0, 0, 0, new Size(1920, 1080));

                srcData = capture.LockBits(
                new Rectangle(0, 0, capture.Width, capture.Height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);

                stride = srcData.Stride;

                Scan0 = srcData.Scan0;

                width = capture.Width;
                height = capture.Height;


                unsafe
                {
                    byte* p = (byte*)(void*)Scan0;

                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            for (int color = 0; color < 3; color++)
                            {
                                idx = (y * stride) + x * 4 + color;

                                totals[color] += p[idx];
                            }
                        }
                    }
                }

                avgB = (int)totals[0] / (width * height);
                avgG = (int)totals[1] / (width * height);
                avgR = (int)totals[2] / (width * height);

                color = computeColor(avgR, avgG, avgB);

                avgB = 0; avgG = 0; avgR = 0;

                capture.UnlockBits(new BitmapData()); 
            }
           
        }

        public void stopWatching()
        {
            running = false;
        }

        private int computeColor(int r, int g, int b)
        {
            RGBHSLHelper helper = FromRGB(r, g, b);
            float h = helper.h;
            float s = helper.s;
            float l = helper.l;

            if (l < 1)
                return -99;

            if (s < 15)
                return -1;

            h = h * (60f);
            h = h * (256f / 360f);

            int hue = (int) Math.Floor(h+0.5f);
            hue += 27;

            if (hue <= 0)
            {
                hue = 256 + hue;
            }
            if (hue >= 255)
            {
                hue = hue - 256;
            }

            return hue;
        }

        public static RGBHSLHelper FromRGB(int R, int G, int B)
        {
            float _R = (R / 255f);
            float _G = (G / 255f);
            float _B = (B / 255f);

            float _Min = Math.Min(Math.Min(_R, _G), _B);
            float _Max = Math.Max(Math.Max(_R, _G), _B);
            float _Delta = _Max - _Min;

            float H = 0;
            float S = 0;
            float L = (float)((_Max + _Min) / 2.0f);

            if (_Delta != 0)
            {
                if (L < 0.5f)
                {
                    S = (float)(_Delta / (_Max + _Min));
                }
                else
                {
                    S = (float)(_Delta / (2.0f - _Max - _Min));
                }

                if (_R == _Max)
                {
                    H = (_G - _B) / _Delta;
                }
                else if (_G == _Max)
                {
                    H = 2f + (_B - _R) / _Delta;
                }
                else if (_B == _Max)
                {
                    H = 4f + (_R - _G) / _Delta;
                }
            }

            S = S * 100; L = L * 100;
            return new RGBHSLHelper(H, S, L, R, G, B);
        }
    }

    public class RGBHSLHelper
    {
        public float h;
        public float s;
        public float l;
        public int r;
        public int g;
        public int b;

        public RGBHSLHelper(float h, float s, float l, int r, int g, int b)
        {
            this.h = h; this.s = s; this.l = l;  this.r = r; this.g = g; this.b = b;
        }
        public RGBHSLHelper()
        {

        }
    }
}
