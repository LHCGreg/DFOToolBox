using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using DFO.Common;
using DFO.Common.Images;
using DFO.Gif;

namespace DFO.NpkReader.Sandbox
{
    class Program
    {
        static void Main(string[] args)
        {
            using (NpkReader npkReader = new NpkReader(@"C:\Neople\DFO\ImagePacks2\sprite_worldmap_act1.NPK"))
            using (NpkReader coolReader = new NpkReader(@"C:\Neople\DFO\ImagePacks2\sprite_character_swordman_effect_sayaex.NPK"))
            {
                DFO.Common.Images.Image image = npkReader.GetImage("worldmap/act1/elvengard.img", 0);
                //Image image2 = npkReader.GetImage("worldmap/act1/elvengard.img", 1);
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


                    RawAnimation animationData = new RawAnimation();
                    animationData.Loop = true;
                    animationData.Frames = new List<ConstAnimationFrame>()
                    {
                        new AnimationFrame() { DelayInMs = 1000, Image = new ImageIdentifier("worldmap/act1/elvengard.img", 0) }.AsConst(),
                        new AnimationFrame() { DelayInMs = 1000, Image = new ImageIdentifier("worldmap/act1/elvengard.img", 1) }.AsConst()
                    };

                    RawAnimation cool = new RawAnimation();
                    cool.Loop = true;
                    cool.Frames = new List<ConstAnimationFrame>()
                    {
                        new AnimationFrame() { DelayInMs = 100, Image = new ImageIdentifier("character/swordman/effect/sayaex/wingdodge.img", 0) }.AsConst(),
                        new AnimationFrame() { DelayInMs = 100, Image = new ImageIdentifier("character/swordman/effect/sayaex/wingdodge.img", 1) }.AsConst(),
                        new AnimationFrame() { DelayInMs = 100, Image = new ImageIdentifier("character/swordman/effect/sayaex/wingdodge.img", 2) }.AsConst(),
                        new AnimationFrame() { DelayInMs = 100, Image = new ImageIdentifier("character/swordman/effect/sayaex/wingdodge.img", 3) }.AsConst(),
                        new AnimationFrame() { DelayInMs = 100, Image = new ImageIdentifier("character/swordman/effect/sayaex/wingdodge.img", 4) }.AsConst(),
                        new AnimationFrame() { DelayInMs = 100, Image = new ImageIdentifier("character/swordman/effect/sayaex/wingdodge.img", 5) }.AsConst(),
                        new AnimationFrame() { DelayInMs = 100, Image = new ImageIdentifier("character/swordman/effect/sayaex/wingdodge.img", 6) }.AsConst(),
                        new AnimationFrame() { DelayInMs = 100, Image = new ImageIdentifier("character/swordman/effect/sayaex/wingdodge.img", 7) }.AsConst(),
                        new AnimationFrame() { DelayInMs = 100, Image = new ImageIdentifier("character/swordman/effect/sayaex/wingdodge.img", 8) }.AsConst(),
                        new AnimationFrame() { DelayInMs = 100, Image = new ImageIdentifier("character/swordman/effect/sayaex/wingdodge.img", 9) }.AsConst(),
                    };

                    using (GifMaker giffer = new GifMaker(npkReader, disposeImageSource: false))
                    using (GifMaker coolGiffer = new GifMaker(coolReader, disposeImageSource: false))
                    using (FileStream gifOutputStream = new FileStream("output.gif", FileMode.Create, FileAccess.ReadWrite, FileShare.None))
                    using (FileStream coolGifOutputStream = new FileStream("cool.gif", FileMode.Create, FileAccess.ReadWrite, FileShare.None))
                    {
                        giffer.Create(animationData.AsConst(), gifOutputStream);
                        coolGiffer.Create(cool.AsConst(), coolGifOutputStream);
                    }
                }

                Console.WriteLine("Success!");
            }
        }
    }
}
