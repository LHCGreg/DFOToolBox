using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DFO.Common;
using DFO.Common.Images;
using GraphicsMagick;

namespace DFO.Images
{
    public static class Export
    {
        /// <summary>
        /// Exports a frame as a PNG file.
        /// </summary>
        /// <param name="imageSource">Source of images. Normally an NPK reader, but could be also be a source that reads from
        /// an extraction or a mock source.</param>
        /// <param name="imgPath"></param>
        /// <param name="frameIndex"></param>
        /// <param name="outputStream">stream to write the PNG to</param>
        /// <exception cref="System.IO.FileNotFoundException">Image with the given path and frame index does not exist.</exception>
        public static void ToPng(IImageSource imageSource, NpkPath imgPath, int frameIndex, Stream outputStream)
        {
            Image image = imageSource.GetImage(imgPath, frameIndex);

            MagickReadSettings pixelDataSettings = new MagickReadSettings()
            {
                ColorSpace = ColorSpace.RGB,
                Width = image.Attributes.Width,
                Height = image.Attributes.Height,
                PixelStorage = new PixelStorageSettings(StorageType.Char, "RGBA")
            };

            using (MagickImage magickImage = new MagickImage(image.PixelData, pixelDataSettings))
            {
                magickImage.Write(outputStream, MagickFormat.Png);
            }
        }
    }
}
