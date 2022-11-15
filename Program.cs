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
                Bitmap bout = await Combine(images.ToArray());
                bout.Save(name + ".png");
            }
            else
            {
                Bitmap previousBit = images[0];

                for (int i = 1; i < images.Count; i++)
                {
                    previousBit.Save(name + (i - 1).ToString());

                    Console.WriteLine(i.ToString());

                    previousBit = await Combine(new Bitmap[] { previousBit, images[i] });
                }
            }
        }

        public static async Task<Bitmap> Combine(Bitmap[] images, float oldWeight = 0f)
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

                    if (oldWeight < 1f)
                        resultBright[x, y] = GetBrighter(potentialColors);

                    if (oldWeight > 0f)
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

                    if (oldWeight < 1f)
                        brightColor = await resultBright[x, y];

                    if (oldWeight > 0f)
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

            return Color.FromArgb(255, red / colors.Length, green / colors.Length, blue / colors.Length);
        }

        public static async Task<Color> GetBlend(Color color1, Color color2, float weight)
        {
            return Color.FromArgb(255, (int)(((weight) * color1.R) + ((1 - weight) * color2.R)), (int)(((weight) * color1.G) + ((1 - weight) * color2.G)), (int)(((weight) * color1.B) + ((1 - weight) * color2.B)));
        }
    }
}