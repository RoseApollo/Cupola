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
            Console.WriteLine();

            ConsoleKey keyGG = Console.ReadKey().Key;

            if (keyGG == ConsoleKey.F)
            {
                FloatImage[] fImages = new FloatImage[images.Count];

                for (int i = 0; i < images.Count; i++)
                {
                    fImages[i] = new FloatImage(images[i]);
                }

                Console.WriteLine("BLEND");
                FloatImage blend = await FloatImage.Blend(fImages, true);
                Console.WriteLine("HEIHGT");
                FloatImage height = await FloatImage.Highest(fImages);

                Console.WriteLine("FINAL");
                Bitmap final = (await FloatImage.Blend(new FloatImage[] { blend, height }, true)).ToBitmap();

                Console.WriteLine("SAVE");
                final.Save(name + ".png");
            }
            else if (keyGG == ConsoleKey.V)
            {
                FloatImage previous = new FloatImage(images[0]);
                FloatImage brightest = new FloatImage(images[0]);

                for (int i = 1; i < images.Count; i++)
                {
                    Console.Write(i.ToString());

                    previous.ToBitmap().Save(name + i.ToString() + ".png");


                    Task<FloatImage> blend = FloatImage.Blend(new FloatImage[] { previous, new FloatImage(images[i]) }, true);
                    Task<FloatImage> height = FloatImage.Highest(new FloatImage[] { brightest, new FloatImage(images[i]) });

                    FloatImage blendImage = await blend;
                    FloatImage heightImage = await height;
                    brightest = heightImage;

                    Console.Write("a");
                    previous = await FloatImage.Blend(new FloatImage[] { blendImage, heightImage }, true);
                    
                    Console.WriteLine();
                }

                previous.ToBitmap().Save(name + images.Count.ToString() + ".png");
            }
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

            public static async Task<FloatImage> Blend(FloatImage[] images, bool spread = false)
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

                Console.Write("b");

                return output;
            }

            public static async Task<FloatImage> Highest(FloatImage[] images)
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

                Console.Write("h");

                return output;
            }
        }
    }
}