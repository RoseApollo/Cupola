using System.Drawing;

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

            ConsoleKey keyGG = Console.ReadKey().Key;

            if (keyGG == ConsoleKey.S)
            {
                Bitmap final = await Combine(images.ToArray(), 0.6f);

                final.Save(name + ".png");
            }
            else if (keyGG == ConsoleKey.M)
            {
                Dictionary<string, Task<Bitmap>> log = new Dictionary<string, Task<Bitmap>>();

                for (int i = 1; i < images.Count; i++)
                {
                    Console.WriteLine(i.ToString());

                    log.Add(name + "-" + (i - 1).ToString() + ".png", Combine(images.Take(i).ToArray(), 0.6f));
                }

                foreach (KeyValuePair<string, Task<Bitmap>> logItem in log)
                {
                    Console.WriteLine(logItem.Key);

                    Bitmap img = await logItem.Value;

                    img.Save(logItem.Key);
                }
            }
            else if (keyGG == ConsoleKey.F)
            {
                FloatImage[] fImages = new FloatImage[images.Count];

                for (int i = 0; i < images.Count; i++)
                {
                    fImages[i] = new FloatImage(images[i]);
                }

                Console.WriteLine("BLEND");
                FloatImage blend = FloatImage.Blend(fImages, true);
                Console.WriteLine("HEIHGT");
                FloatImage height = FloatImage.Highest(fImages);

                Console.WriteLine("FINAL");
                Bitmap final = FloatImage.Blend(new FloatImage[] { blend, height }, true).ToBitmap();

                Console.WriteLine("SAVE");
                final.Save(name + ".png");
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

        public class FloatImage
        {
            public class FloatColor
            {
                public float red;
                public float green;
                public float blue;

                public FloatColor(Color color)
                {
                    this.red = (float)color.R;
                    this.green = (float)color.G;
                    this.blue = (float)color.B;
                }

                public Color Export()
                {
                    this.red = MathF.Round(this.red);
                    this.green = MathF.Round(this.green);
                    this.blue = MathF.Round(this.blue);

                    byte red, green, blue;

                    red = (byte)Math.Clamp((int)this.red, 0, 256);
                    green = (byte)Math.Clamp((int)this.green, 0, 256);
                    blue = (byte)Math.Clamp((int)this.blue, 0, 256);

                    return Color.FromArgb(red, green, blue);
                }

                public float Brightness()
                {
                    return this.red + this.green + this.blue;
                }

                public static void Spread(FloatColor[] colors, ref float minus, ref float multiply)
                {
                    List<float> allColors = new List<float>();

                    foreach (FloatColor color in colors)
                    {
                        allColors.Add(color.red);
                        allColors.Add(color.blue);
                        allColors.Add(color.green);
                    }

                    float min = 0;
                    float max = 0;

                    foreach (float aColor in allColors)
                    {
                        if (aColor < min)
                            min = aColor;
                        else if (aColor > max)
                            max = aColor;
                    }

                    minus = min;

                    multiply = 255f / (max - minus);
                }
            }

            public uint width { get; private set; }
            public uint height { get; private set; }

            public FloatColor[,] pixels { get; private set; }

            public FloatImage(uint width, uint height)
            {
                this.width = width;
                this.height = height;

                this.pixels = new FloatColor[width, height];

                for (int x = 0; x < this.width; x++)
                {
                    for (int y = 0; y < this.height; y++)
                    {
                        this.pixels[x, y] = new FloatColor(Color.Black);
                    }
                }
            }

            public FloatImage(Bitmap bitmap)
            {
                this.width = (uint)bitmap.Width;
                this.height = (uint)bitmap.Height;

                this.pixels = new FloatColor[this.width, this.height];

                for (int x = 0; x < this.width; x++)
                {
                    for (int y = 0; y < this.height; y++)
                    {
                        this.pixels[x, y] = new FloatColor(bitmap.GetPixel(x, y));
                    }
                }
            }

            public void SetPixel(int x, int y, FloatColor color)
            {
                this.pixels[x, y] = color;
            }

            public Bitmap ToBitmap()
            {
                Bitmap output = new Bitmap((int)this.width, (int)this.height);

                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        output.SetPixel(x, y, pixels[x, y].Export());
                    }
                }

                return output;
            }

            public static void Spread(FloatImage[] images, ref float minus, ref float multiply)
            {
                FloatColor[] colors = new FloatColor[images[0].width * images[0].height * images.Length];

                for (int i = 0; i < images.Length; i++)
                {
                    for (int x = 0; x < images[0].width; x++)
                    {
                        for (int y = 0; y < images[0].height; y++)
                        {
                            colors[(i * images[0].width * images[0].height) + (x * images[0].height) + y] = images[i].pixels[x, y];
                        }
                    }
                }

                FloatColor.Spread(colors, ref minus, ref multiply);
            }

            public static FloatImage Blend(FloatImage[] images, bool spread = false)
            {
                FloatImage output = new FloatImage(images[0].width, images[0].height);

                for (int x = 0; x < output.width; x++)
                {
                    for (int y = 0; y < output.height; y++)
                    {
                        FloatColor pixel = new FloatColor(Color.Black);

                        for (int i = 0; i < images.Length; i++)
                        {
                            FloatColor pixTemp = images[i].pixels[x, y];

                            pixel.red += pixTemp.red;
                            pixel.green += pixTemp.green;
                            pixel.blue += pixTemp.blue;
                        }

                        if (!spread)
                        {
                            pixel.red = pixel.red / images.Length;
                            pixel.green = pixel.green / images.Length;
                            pixel.blue = pixel.blue / images.Length;
                        }

                        output.SetPixel(x, y, pixel);
                    }
                }

                if (spread)
                {
                    float minus = 0;
                    float multiply = 0;

                    FloatImage.Spread(new FloatImage[] { output }, ref minus, ref multiply);

                    for (int x = 0; x < output.width; x++)
                    {
                        for (int y = 0; y < output.height; y++)
                        {
                            FloatColor pixel = output.pixels[x, y];

                            pixel.red = (pixel.red - minus) * multiply;
                            pixel.green = (pixel.green - minus) * multiply;
                            pixel.blue = (pixel.blue - minus) * multiply;

                            output.SetPixel(x, y, pixel);
                        }//hi
                    }
                }

                return output;
            }

            public static FloatImage Highest(FloatImage[] images)
            {
                FloatImage output = new FloatImage(images[0].width, images[0].height);

                for (int x = 0; x < images[0].width; x++)
                {
                    for (int y = 0; y < images[0].height; y++)
                    {
                        FloatColor brightest = new FloatColor(Color.Black);

                        for (int i = 0; i < images.Length; i++)
                        {
                            if (images[i].pixels[x, y].Brightness() > brightest.Brightness())
                            {
                                brightest = images[i].pixels[x, y];
                            }
                        }

                        output.SetPixel(x, y, brightest);
                    }
                }

                return output;
            }
        }
    }
}