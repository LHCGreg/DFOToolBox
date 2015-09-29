using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DFO.Utilities;

namespace DFO.Common.Images
{
    public class FrameInfo
    {
        /// <summary>
        /// Gets the index of the frame that this frame links to or null if this frame is not a link.
        /// </summary>
        public int? LinkFrame { get; private set; }

        public bool IsCompressed { get; private set; }
        public int CompressedLength { get; private set; }
        public PixelDataFormat PixelFormat { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int LocationX { get; private set; }
        public int LocationY { get; private set; }
        public int MaxWidth { get; private set; }
        public int MaxHeight { get; private set; }

        public FrameInfo(int linkFrame)
        {
            LinkFrame = linkFrame;
            PixelFormat = PixelDataFormat.Link;
        }

        public FrameInfo(bool isCompressed, int compressedLength, PixelDataFormat pixelFormat, int width, int height, int locationX, int locationY, int maxWidth,
            int maxHeight)
        {
            IsCompressed = isCompressed;
            CompressedLength = compressedLength;
            PixelFormat = pixelFormat;
            Width = width;
            Height = height;
            LocationX = locationX;
            LocationY = locationY;
            MaxWidth = maxWidth;
            MaxHeight = maxHeight;
        }

        public int GetPixelDataLength()
        {
            if (PixelFormat == PixelDataFormat.Link)
            {
                return 0;
            }

            if (IsCompressed)
            {
                return CompressedLength;
            }
            else
            {
                return Width * Height * GetBytesPerPixel();
            }
        }

        public static int GetBytesPerPixel(PixelDataFormat pixelFormat)
        {
            switch(pixelFormat)
            {
                case PixelDataFormat.EightEightEightEight:
                    return 4;
                case PixelDataFormat.FourFourFourFour:
                    return 2;
                case PixelDataFormat.OneFiveFiveFive:
                    return 2;
                case PixelDataFormat.Link:
                    return 0;
                default:
                    throw new Exception("Invalid pixel format {0}.".F(pixelFormat));
            }
        }

        public int GetBytesPerPixel()
        {
            return GetBytesPerPixel(PixelFormat);
        }

        public static void GetNormalizedCoordinates(IEnumerable<FrameInfo> frames, out int smallestX, out int largestX, out int smallestY, out int largestY)
        {
            smallestX = int.MaxValue;
            largestX = 0;
            smallestY = int.MaxValue;
            largestY = 0;

            foreach (FrameInfo frame in frames)
            {
                int startX = frame.LocationX;
                int endX = startX + frame.Width - 1;
                int startY = frame.LocationY;
                int endY = startY + frame.Height - 1;

                if (startX < smallestX)
                {
                    smallestX = startX;
                }
                if (endX > largestX)
                {
                    largestX = endX;
                }
                if (startY < smallestY)
                {
                    smallestY = startY;
                }
                if (endY > largestY)
                {
                    largestY = endY;
                }
            }
        }
    }
}
