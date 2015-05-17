using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DFO.Common;
using DFO.Common.Images;
using GraphicsMagick;

namespace DFO.Gif
{
    public class GifMaker : IDisposable
    {
        private IImageSource m_imageSource;
        private bool m_disposeImageSource;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="imageSource">Source of images. Normally an NPK reader, but could be also be a source that reads from
        /// an extraction or a mock source.</param>
        /// <param name="disposeImageSource">If true, Dispose() the image source when this object is disposed.</param>
        public GifMaker(IImageSource imageSource, bool disposeImageSource)
        {
            m_imageSource = imageSource;
            m_disposeImageSource = disposeImageSource;
        }

        public void Dispose()
        {
            if (m_disposeImageSource)
            {
                m_imageSource.Dispose();
            }
        }

        /// <summary>
        /// Creates an animated GIF and writes it to <paramref name="outputStream"/>.
        /// </summary>
        /// <param name="animation"></param>
        /// <param name="outputStream"></param>
        public void Create(ConstRawAnimation animation, Stream outputStream)
        {
            List<Image> rawFrames = new List<Image>();

            // Load each image in the animation.
            foreach (ConstAnimationFrame frame in animation.Frames)
            {
                NpkPath frameImagePath = frame.Image.ImageFilePath;
                DFO.Common.Images.Image rawFrame = m_imageSource.GetImage(frameImagePath, frame.Image.FrameIndex);
                rawFrames.Add(rawFrame);
            }

            uint smallestX;
            uint largestX;
            uint smallestY;
            uint largestY;

            // Frames can have different start positions and widths/heights. Normalize the images to a common coordinate system.
            GetNormalizedCoordinates(rawFrames, out smallestX, out largestX, out smallestY, out largestY);

            uint normalizedWidth = largestX - smallestX;
            uint normalizedHeight = largestY - smallestY;

            List<MagickImage> renderedFrames = new List<MagickImage>();

            try
            {
                // Composite each frame on top of a canvas of normalized width and height.
                for (int frameIndex = 0; frameIndex < animation.Frames.Count; frameIndex++)
                {
                    Image rawFrameImage = rawFrames[frameIndex];
                    ConstAnimationFrame frameAnimationInfo = animation.Frames[frameIndex];

                    MagickImage renderedFrame = RenderFrame(rawFrameImage, frameAnimationInfo, smallestX, largestX, smallestY, largestY, normalizedWidth, normalizedHeight);
                    renderedFrames.Add(renderedFrame);
                }

                // Make the GIF from the frames and write it out to the stream.
                using (MagickImageCollection frameCollection = new GraphicsMagick.MagickImageCollection(renderedFrames))
                {
                    frameCollection.Write(outputStream, MagickFormat.Gif);
                }
            }
            finally
            {
                renderedFrames.ForEach(f => f.Dispose());
            }
        }

        private void GetNormalizedCoordinates(List<Image> rawFrames, out uint smallestX, out uint largestX, out uint smallestY, out uint largestY)
        {
            smallestX = uint.MaxValue;
            largestX = 0;
            smallestY = uint.MaxValue;
            largestY = 0;

            foreach (Image rawFrame in rawFrames)
            {
                uint startX = rawFrame.Attributes.LocationX;
                uint endX = startX + rawFrame.Attributes.Width;
                uint startY = rawFrame.Attributes.LocationY;
                uint endY = startY + rawFrame.Attributes.Height;

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

        private MagickImage RenderFrame(Image rawFrameImage, ConstAnimationFrame frameAnimationInfo, uint smallestX, uint largestX, uint smallestY, uint largestY, uint normalizedWidth, uint normalizedHeight)
        {
            MagickImage renderedFrame = new MagickImage(new MagickColor(0, 0, 0, 0), (int)normalizedWidth, (int)normalizedHeight);

            uint normalizedFrameX = rawFrameImage.Attributes.LocationX - smallestX;
            uint normalizedFrameY = rawFrameImage.Attributes.LocationY - smallestY;

            if (rawFrameImage.PixelData.Length > 0)
            {
                MagickReadSettings pixelDataSettings = new MagickReadSettings()
                {
                    ColorSpace = ColorSpace.RGB,
                    Width = (int)rawFrameImage.Attributes.Width,
                    Height = (int)rawFrameImage.Attributes.Height,
                    PixelStorage = new PixelStorageSettings(StorageType.Char, "RGBA")
                };

                using (MagickImage rawFrameMagickImage = new MagickImage(rawFrameImage.PixelData, pixelDataSettings))
                {
                    rawFrameMagickImage.Format = MagickFormat.Gif;
                    rawFrameMagickImage.MatteColor = new MagickColor(0, 0, 0, 0);
                    renderedFrame.Composite(rawFrameMagickImage, (int)normalizedFrameX, (int)normalizedFrameY, CompositeOperator.Over);
                }
            }

            renderedFrame.Format = MagickFormat.Gif;
            renderedFrame.AnimationDelay = (int)frameAnimationInfo.DelayInMs / 10;
            renderedFrame.GifDisposeMethod = GifDisposeMethod.Background;
            renderedFrame.AnimationIterations = 0;
            renderedFrame.MatteColor = new MagickColor(0, 0, 0, 0);

            return renderedFrame;
        }
    }
}
