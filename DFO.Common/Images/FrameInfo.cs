using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DFO.Common.Images
{
    public class FrameInfo
    {
        /// <summary>
        /// Gets the index of the frame that this frame links to or null if this frame is not a link.
        /// </summary>
        public uint? LinkFrame { get; private set; }

        public bool IsCompressed { get; private set; }
        public uint CompressedLength { get; private set; }
        public uint Mode { get; private set; }
        public uint Width { get; private set; }
        public uint Height { get; private set; }
        public uint LocationX { get; private set; }
        public uint LocationY { get; private set; }
        public uint MaxWidth { get; private set; }
        public uint MaxHeight { get; private set; }

        public FrameInfo(uint linkFrame)
        {
            LinkFrame = linkFrame;
        }

        public FrameInfo(bool isCompressed, uint compressedLength, uint mode, uint width, uint height, uint locationX, uint locationY, uint maxWidth,
            uint maxHeight)
        {
            IsCompressed = isCompressed;
            CompressedLength = compressedLength;
            Mode = mode;
            Width = width;
            Height = height;
            LocationX = locationX;
            LocationY = locationY;
            MaxWidth = maxWidth;
            MaxHeight = maxHeight;
        }
    }
}
