using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DFO.Common.Images;
using DFO.Utilities;

namespace DFOToolbox
{
    static class PixelConversion
    {
        public static byte[] Convert(Bitmap image, PixelDataFormat destinationFormat)
        {
            image.ThrowIfNull("image");
            if (destinationFormat == PixelDataFormat.Link)
            {
                throw new ArgumentException("Code tried to convert an image to link format which doesn't make sense.");
            }

            int bytesPerPixel = FrameInfo.GetBytesPerPixel(destinationFormat);
            byte[] convertedPixelBytes = new byte[image.Width * image.Height * bytesPerPixel];

            switch (destinationFormat)
            {
                case PixelDataFormat.OneFiveFiveFive:
                    FillBufferOneFiveFiveFiveLe(image, convertedPixelBytes);
                    break;
                case PixelDataFormat.FourFourFourFour:
                    FillBufferFourFourFourFourLe(image, convertedPixelBytes);
                    break;
                case PixelDataFormat.EightEightEightEight:
                    FillBufferEightEightEightEightLe(image, convertedPixelBytes);
                    break;
                default:
                    throw new NotImplementedException("Oops, missed a pixel format to convert to: {0}".F(destinationFormat));
            }

            return convertedPixelBytes;
            //BitmapData bits = inputImage.LockBits(new Rectangle(0, 0, inputImage.Width, inputImage.Height), ImageLockMode.ReadOnly, PixelFormat.Format16bppArgb1555);
        }

        private static void FillBufferOneFiveFiveFiveLe(Bitmap image, byte[] convertedPixelBytes)
        {
            BitmapData bits = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, PixelFormat.Format16bppArgb1555);

            int index = 0;
            int width = image.Width;
            int height = image.Height;
            const int bytesPerPixel = 2;

            try
            {
                unsafe
                {
                    byte* startPtr = (byte*)bits.Scan0.ToPointer();

                    if (BitConverter.IsLittleEndian)
                    {
                        for (int y = 0; y < height; y++)
                        {
                            // (y * stride) + x
                            byte* rowPtr = startPtr + (y * bits.Stride);

                            for (int x = 0; x < width; x++)
                            {
                                byte* pixelPtr = rowPtr + (x * bytesPerPixel);
                                convertedPixelBytes[index] = *(pixelPtr);
                                convertedPixelBytes[index + 1] = *(pixelPtr + 1);
                                index += bytesPerPixel;
                            }
                        }
                    }
                    else
                    {
                        for (int y = 0; y < height; y++)
                        {
                            // (y * stride) + x
                            byte* rowPtr = startPtr + (y * bits.Stride);

                            for (int x = 0; x < width; x++)
                            {
                                byte* pixelPtr = rowPtr + (x * bytesPerPixel);
                                convertedPixelBytes[index] = *(pixelPtr + 1);
                                convertedPixelBytes[index + 1] = *(pixelPtr);
                                index += bytesPerPixel;
                            }
                        }
                    }
                }
            }
            finally
            {
                image.UnlockBits(bits);
            }
        }

        private static void FillBufferEightEightEightEightLe(Bitmap image, byte[] convertedPixelBytes)
        {
            BitmapData bits = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            int index = 0;
            int width = image.Width;
            int height = image.Height;
            const int bytesPerPixel = 4;

            try
            {
                unsafe
                {
                    byte* startPtr = (byte*)bits.Scan0.ToPointer();

                    if (BitConverter.IsLittleEndian)
                    {
                        for (int y = 0; y < height; y++)
                        {
                            // (y * stride) + x
                            byte* rowPtr = startPtr + (y * bits.Stride);

                            for (int x = 0; x < width; x++)
                            {
                                byte* pixelPtr = rowPtr + (x * bytesPerPixel);
                                convertedPixelBytes[index] = *(pixelPtr);
                                convertedPixelBytes[index + 1] = *(pixelPtr + 1);
                                convertedPixelBytes[index + 2] = *(pixelPtr + 2);
                                convertedPixelBytes[index + 3] = *(pixelPtr + 3);
                                index += bytesPerPixel;
                            }
                        }
                    }
                    else
                    {
                        for (int y = 0; y < height; y++)
                        {
                            // (y * stride) + x
                            byte* rowPtr = startPtr + (y * bits.Stride);

                            for (int x = 0; x < width; x++)
                            {
                                byte* pixelPtr = rowPtr + (x * bytesPerPixel);
                                convertedPixelBytes[index] = *(pixelPtr + 3);
                                convertedPixelBytes[index + 1] = *(pixelPtr + 2);
                                convertedPixelBytes[index + 2] = *(pixelPtr + 1);
                                convertedPixelBytes[index + 3] = *(pixelPtr);
                                index += bytesPerPixel;
                            }
                        }
                    }
                }
            }
            finally
            {
                image.UnlockBits(bits);
            }
        }

        private static void FillBufferFourFourFourFourLe(Bitmap image, byte[] convertedPixelBytes)
        {
            BitmapData bits = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            int index = 0;
            int width = image.Width;
            int height = image.Height;
            const int bytesPerPixel = 2;

            try
            {
                unsafe
                {
                    byte* startPtr = (byte*)bits.Scan0.ToPointer();

                    if (BitConverter.IsLittleEndian)
                    {
                        for (int y = 0; y < height; y++)
                        {
                            // (y * stride) + x
                            byte* rowPtr = startPtr + (y * bits.Stride);

                            for (int x = 0; x < width; x++)
                            {
                                byte* pixelPtr = rowPtr + (x * bytesPerPixel);
                                // We're only taking the most significant 16 bits
                                convertedPixelBytes[index] = *(pixelPtr + 2);
                                convertedPixelBytes[index + 1] = *(pixelPtr + 3);
                                //convertedPixelBytes[index] = *(pixelPtr);
                                //convertedPixelBytes[index + 1] = *(pixelPtr + 1);
                                //convertedPixelBytes[index + 2] = *(pixelPtr + 2);
                                //convertedPixelBytes[index + 3] = *(pixelPtr + 3);
                                index += bytesPerPixel;
                            }
                        }
                    }
                    else
                    {
                        for (int y = 0; y < height; y++)
                        {
                            // (y * stride) + x
                            byte* rowPtr = startPtr + (y * bits.Stride);

                            for (int x = 0; x < width; x++)
                            {
                                byte* pixelPtr = rowPtr + (x * bytesPerPixel);
                                // We're only taking the most significant 16 bites
                                convertedPixelBytes[index] = *(pixelPtr + 1);
                                convertedPixelBytes[index + 1] = *(pixelPtr);
                                //convertedPixelBytes[index] = *(pixelPtr + 3);
                                //convertedPixelBytes[index + 1] = *(pixelPtr + 2);
                                //convertedPixelBytes[index + 2] = *(pixelPtr + 1);
                                //convertedPixelBytes[index + 3] = *(pixelPtr);
                                index += bytesPerPixel;
                            }
                        }
                    }
                }
            }
            finally
            {
                image.UnlockBits(bits);
            }
        }
    }
}
