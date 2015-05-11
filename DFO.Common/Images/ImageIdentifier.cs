using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DFO.Utilities;

namespace DFO.Common.Images
{
    public class ImageIdentifier
    {
        public NpkPath ImageFilePath { get; private set; }
        public uint FrameIndex { get; private set; }

        public ImageIdentifier(NpkPath imageFilePath, uint frameIndex)
        {
            ImageFilePath = imageFilePath;
            FrameIndex = frameIndex;
        }

        public override string ToString()
        {
            return "{0} {1}".F(ImageFilePath, FrameIndex);
        }

        // Don't make it public because this is doing a string comparison of the image file path,
        // not a true path comparison. This is only for use by tests.
        internal bool Equals(ImageIdentifier other)
        {
            if (other == null)
            {
                return false;
            }
            return this.FrameIndex == other.FrameIndex && this.ImageFilePath.Equals(other.ImageFilePath);
        }
    }
}
