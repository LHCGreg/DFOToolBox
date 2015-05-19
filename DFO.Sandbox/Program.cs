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
using DFO.Npk;

namespace DFO.Sandbox
{
    class Program
    {
        static void Main(string[] args)
        {
            foreach (string path in Directory.GetFiles(@"C:\Neople\DFO\ImagePacks2", "*.NPK"))
            {
                using (NpkReader npk = new NpkReader(path))
                {
                    if (npk.Images.Keys.Any(img => img.Path.EndsWith("bufficon.img")))
                    {
                        Console.WriteLine(path);
                        Environment.Exit(0);
                    }

                    Console.WriteLine("Read {0}", path);
                }
            }

            Console.WriteLine("Not found!");
            Environment.Exit(0);

            using (NpkReader npkReader = new NpkReader(@"C:\Neople\DFO\ImagePacks2\sprite_monster_impossible_bakal.NPK"))
            using (NpkReader coolReader = new NpkReader(@"C:\Neople\DFO\ImagePacks2\sprite_character_swordman_effect_sayaex.NPK"))
            {
                DFO.Common.Images.Image image = npkReader.GetImage("monster/impossible_bakal/ashcore.img", 0);
                //Image image2 = npkReader.GetImage("worldmap/act1/elvengard.img", 1);
                using (Bitmap bitmap = new Bitmap(image.Attributes.Width, image.Attributes.Height))
                {
                    BitmapData raw = bitmap.LockBits(new Rectangle(0, 0, image.Attributes.Width, image.Attributes.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                    unsafe
                    {
                        byte* ptr = (byte*)raw.Scan0;
                        // RGBA -> BGRA (pixels in the bitmap have endianness)
                        int width = image.Attributes.Width;
                        int height = image.Attributes.Height;
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
