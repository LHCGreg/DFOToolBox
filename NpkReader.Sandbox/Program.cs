using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DFO.NpkReader.Sandbox
{
    class Program
    {
        static void Main(string[] args)
        {
            using (NpkReader npkReader = new NpkReader(@"C:\Neople\DFO\ImagePacks2\sprite_worldmap_act1.NPK"))
            {
                Image image = npkReader.GetImage("worldmap/act1/elvengard.img", 0);
                using (Bitmap bitmap = new Bitmap((int)image.Attributes.Width, (int)image.Attributes.Height))
                {
                    BitmapData raw = bitmap.LockBits(new Rectangle(0, 0, (int)image.Attributes.Width, (int)image.Attributes.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                    unsafe
                    {
                        byte* ptr = (byte*)raw.Scan0;
                        // RGBA -> BGRA (pixels in the bitmap have endianness)
                        int width = (int)image.Attributes.Width;
                        int height = (int)image.Attributes.Height;
                        int stride = raw.Stride;
                        for (int x = 0; x < width; x++)
                        {
                            for (int y = 0; y < height; y++)
                            {
                                ptr[y * stride + x * 4 + 0] = image.PixelData[y * width * 4 + x * 4 + 2];
                                ptr[y * stride + x * 4 + 1] = image.PixelData[y * width * 4 + x * 4 + 1];
                                ptr[y * stride + x * 4 + 2] = image.PixelData[y * width * 4 + x * 4 + 0];
                                ptr[y * stride + x * 4 + 3] = image.PixelData[y * width * 4 + x * 4 + 3];
                            }
                        }
                    }
                    bitmap.UnlockBits(raw);
                    bitmap.Save(@"output.png", System.Drawing.Imaging.ImageFormat.Png);
                }
                Console.WriteLine("Success!");
            }
        }
    }
}
