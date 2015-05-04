using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DFO.NpkReader
{
    public class Image
    {
        /// <summary>
        /// In RGBA format, 1 byte each for R, G, B, A.
        /// </summary>
        public byte[] PixelData { get; private set; }
        public FrameInfo Attributes { get; private set; }

        internal Image(byte[] pixelData, FrameInfo attributes)
        {
            PixelData = pixelData;
            Attributes = attributes;
        }
    }
}
