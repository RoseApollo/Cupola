using System;
using System.Drawing;
using System.IO;

namespace Cupola
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            List<Bitmap> images = new List<Bitmap>();

            Console.WriteLine("Image Dir?");
            string fileLoc = Console.ReadLine();

            if (fileLoc == null)
                throw new ArgumentException("input should not be NULL");

            string[] files = Directory.GetFiles(fileLoc);

            for (int i = 0; i < files.Length; i++)
            {
                Console.WriteLine(files[i]);
                images.Add(new Bitmap(files[i]));
            }

            Console.WriteLine("Output: ");

            string name = Console.ReadLine();

            Console.WriteLine("mode?");

            if (Console.ReadKey().Key == ConsoleKey.S)
            {
                Bitmap final = await Combine(images.ToArray(), 0.6f);

                final.Save(name + ".png");
            }
            else
            {
                Bitmap previousBit = images[0];

                List<Bitmap> oldBitmaps = new List<Bitmap>();

                for (int i = 1; i < images.Count; i++)
                {
                    oldBitmaps.Add(previousBit);

                    previousBit.Save(name + "-" + (i - 1).ToString() + ".png");

                    Console.WriteLine(i.ToString());

                    previousBit = await Combine(new Bitmap[] { previousBit, images[i] }, 1f);
                }
            }
        }

        public static async Task<Bitmap> Combine(Bitmap[] images, float oldWeight = 1f) // default to brightest
        {
            int width = images[0].Width;
            int height = images[0].Height;

            for (int i = 0; i < images.Length; i++)
            {
                if (images[i].Width != width || images[i].Height != height)
                    throw new ArgumentException("images not same size");
            }

            Bitmap final = new Bitmap(width, height);

            Task<Color>[,] resultBright = new Task<Color>[width, height];
            Task<Color>[,] resultBlend = new Task<Color>[width, height];


            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Color[] potentialColors = new Color[images.Length];

                    for (int i = 0; i < potentialColors.Length; i++)
                        potentialColors[i] = images[i].GetPixel(x, y);

                    if (oldWeight > 0f)
                        resultBright[x, y] = GetBrighter(potentialColors);

                    if (oldWeight < 1f)
                        resultBlend[x, y] = GetAverage(potentialColors);
                }
            }

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Color brightColor = new Color();
                    Color blendColor = new Color();
                    Color finalColor = new Color();

                    if (oldWeight > 0f)
                        brightColor = await resultBright[x, y];

                    if (oldWeight < 1f)
                        blendColor = await resultBlend[x, y];

                    finalColor = await GetBlend(brightColor, blendColor, oldWeight);

                    final.SetPixel(x, y, finalColor);
                }
            }

            return final;
        }

        public static async Task<Color> GetBrighter(Color[] colors)
        {
            Color brightest = new Color();
            int brightestScore = 0;

            for (int i = 0; i < colors.Length; i++)
            {
                if (ColorToInt(colors[i]) > brightestScore)
                {
                    brightest = colors[i];
                    brightestScore = ColorToInt(colors[i]);
                }
            }

            return brightest;
        }

        public static int ColorToInt(Color color)
        {
            return color.R + color.B + color.G;
        }

        public static async Task<Color> GetAverage(Color[] colors)
        {
            int red = 0;
            int green = 0;
            int blue = 0;

            for (int i = 0; i < colors.Length; i++)
            {
                red += colors[i].R;
                green += colors[i].G;
                blue += colors[i].B;
            }

            red = red / colors.Length;
            green = green / colors.Length;
            blue = blue / colors.Length;

            if (red > 255)
                red = 255;

            if (green > 255)
                green = 255;

            if (blue > 255)
                blue = 255;

            return Color.FromArgb(red, green, blue);
        }

        public static async Task<Color> GetBlend(Color color1, Color color2, float weight)
        {
            return Color.FromArgb((int)(((weight) * color1.R) + ((1 - weight) * color2.R)), (int)(((weight) * color1.G) + ((1 - weight) * color2.G)), (int)(((weight) * color1.B) + ((1 - weight) * color2.B)));
        }
    }
}