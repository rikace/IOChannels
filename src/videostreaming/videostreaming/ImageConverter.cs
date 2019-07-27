using System;
namespace avcapturesessiontest
{
    using System;
    using System.Drawing;
    using System.IO;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;

    using SixLabors.ImageSharp;
    using SixLabors.ImageSharp.Processing;
    using SixLabors.ImageSharp.PixelFormats;


    public class ImageConverter
    {
        private const int _asciiWidth = 150;
        private static string[] _asciiChars = { "#", "@", "%", "=", "+", "*", ":", "-", ".", " ", "?"};
        
        public static string ImageToAsciiArt(Image<Rgb24> image)
        {
            image = GetReSizedImage(image, _asciiWidth);

            //Convert the resized image into ASCII
            string ascii = ConvertToAscii(image);
            return ascii;
        }

        private static Image<Rgb24> GetReSizedImage(Image<Rgb24> inputBitmap, int asciiWidth)
        {
            int asciiHeight = 0;
            //Calculate the new Height of the image from its width
            asciiHeight = (int)Math.Ceiling((double)inputBitmap.Height * asciiWidth / inputBitmap.Width);
            inputBitmap.Mutate(ctx => ctx.Resize(asciiWidth, asciiHeight));
            return inputBitmap;
        }
        
        static StringBuilder sb = new StringBuilder();
        private static string ConvertToAscii(Image<Rgb24> image)
        {
            Boolean toggle = false;
            sb.Clear();

            for (int h = 0; h < image.Height; h++)
            {
                for (int w = 0; w < image.Width; w++)
                {
                    var pixelColor = image[w, h];
                    //Average out the RGB components to find the Gray Color
                    int gray = (pixelColor.R + pixelColor.G + pixelColor.B) / 3;

                    //Use the toggle flag to minimize height-wise stretch
                    if (!toggle)
                    {
                        int index = (gray * 10) / 255;
                        sb.Append(_asciiChars[index]);
                    }
                }

                if (!toggle)
                {
                    sb.Append(Environment.NewLine);
                    toggle = true;
                }
                else
                    toggle = false;
            }

            return sb.ToString();
        }
    }
}
