using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using ICSharpCode.SharpZipLib;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using DFO.Utilities;
using DFO.Common;
using DFO.Common.Images;

namespace DFO.NpkReader
{
    public class NpkReader : IImageSource, IDisposable
    {
        private FileStream m_npkStream;
        private byte[] m_intBuffer = new byte[4];

        private static string s_keyString = "puchikon@neople dungeon and fighter DNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNF\0";
        private static byte[] s_key = Encoding.ASCII.GetBytes(s_keyString);

        private IDictionary<NpkPath, NpkByteRange> m_imageFileLocations = new Dictionary<NpkPath, NpkByteRange>();
        private IDictionary<NpkPath, NpkByteRange> m_soundFileLocations = new Dictionary<NpkPath, NpkByteRange>();

        private IDictionary<NpkPath, IList<NpkByteRange>> m_frameLocations = new Dictionary<NpkPath, IList<NpkByteRange>>();

        // If this is re-set, Images must be updated to point to the new value.
        private IDictionary<NpkPath, IList<FrameInfo>> m_images = new Dictionary<NpkPath, IList<FrameInfo>>();
        // Initialized in constructor
        public DFO.Utilities.IReadOnlyDictionary<NpkPath, System.Collections.ObjectModel.ReadOnlyCollection<FrameInfo>> Images { get; private set; }

        private IDictionary<NpkPath, SoundInfo> m_sounds = new Dictionary<NpkPath, SoundInfo>();
        // Initialized in constructor
        public DFO.Utilities.IReadOnlyDictionary<NpkPath, SoundInfo> Sounds { get; private set; }

        public string PathOfNpkFile { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pathOfNpkFile"></param>
        /// <exception cref="System.ArgumentNullException"><paramref name="pathOfNpkFile"/> is null.</exception>
        /// <exception cref="System.IO.FileNotFoundException">The .npk file does not exist.</exception>
        /// <exception cref="System.IO.IOException">An I/O error occurred.</exception>
        /// <exception cref="Dfo.Parser.NpkException">The .npk file is corrupt or the format changed.</exception>
        /// <exception cref="System.UnauthorizedAccessException">The caller does not have the required
        /// permission or the file is actually a directory.</exception>
        public NpkReader(string pathOfNpkFile)
        {
            try
            {
                pathOfNpkFile.ThrowIfNull("pathOfNpkFile");
                PathOfNpkFile = pathOfNpkFile;

                Images = new DeepReadOnlyDictionary<NpkPath, IList<FrameInfo>, System.Collections.ObjectModel.ReadOnlyCollection<FrameInfo>>(
                    m_images, (IList<FrameInfo> mutableList) => new System.Collections.ObjectModel.ReadOnlyCollection<FrameInfo>(mutableList));
                Sounds = new ReadOnlyDictionary<NpkPath, SoundInfo>(m_sounds);

                m_npkStream = OpenNpkFile();
                LoadNpkHeader();
            }
            catch (Exception)
            {
                Dispose();
                throw;
            }
        }

        /// <summary>
        /// Returns a file stream from opening the .npk file.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.IO.FileNotFoundException">The .npk file does not exist.</exception>
        /// <exception cref="System.IO.IOException">An I/O error occurred.</exception>
        /// <exception cref="System.UnauthorizedAccessException">The caller does not have the required
        /// permission or the file is actually a directory.</exception>
        private FileStream OpenNpkFile()
        {
            try
            {
                return File.Open(PathOfNpkFile, FileMode.Open, FileAccess.Read, FileShare.Read);
            }
            catch (Exception ex)
            {
                // Wrap these as a FileNotFound
                if (ex is ArgumentException || ex is PathTooLongException || ex is DirectoryNotFoundException
                || ex is NotSupportedException)
                {
                    throw new FileNotFoundException("{0} does not exist.".F(PathOfNpkFile), PathOfNpkFile, ex);
                }
                else
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Helper function that loads the first part of the .npk header, initializing m_imagefileLocations and
        /// m_soundFileLocations are loaded when this function completes. The file is assumed to be opened
        /// with the read pointer at the beginning of the file.
        /// </summary>
        /// <exception cref="System.IO.IOException">An I/O error occurred.</exception>
        /// <exception cref="Dfo.Parser.NpkException">The .npk file is corrupt or the format changed.</exception>
        private void LoadNpkHeader()
        {
            try
            {
                LoadNpkFileTable();

                // TODO: Profile to determine whether to preload the metadata or get it on-demand.
                foreach (KeyValuePair<NpkPath, NpkByteRange> pathLocationPair in m_imageFileLocations)
                {
                    LoadSpriteFileMetaData(pathLocationPair.Key, pathLocationPair.Value);
                }
            }
            catch (EndOfStreamException ex)
            {
                throw new NpkException("Unexpected end of file.", ex);
            }
        }

        /// <summary>
        /// Helper function that loads the first part of the .npk header. m_imagefileLocations and
        /// m_soundFileLocations are loaded when this function completes. The stream's read pointer will
        /// be right after the file table part of the header after completion. This function is only
        /// intended to be called from LoadNpkHeader().
        /// </summary>
        /// <exception cref="System.IO.EndOfStreamException">Unexpected end of file.</exception>
        /// <exception cref="System.IO.IOException">I/O error.</exception>
        private void LoadNpkFileTable()
        {
            // file starts with "NeoplePack_Bill\0" in ASCII
            string dirListingHeader = "NeoplePack_Bill\0";
            byte[] headerBuffer = new byte[dirListingHeader.Length];
            m_npkStream.ReadOrDie(headerBuffer, headerBuffer.Length);
            string headerString = Encoding.ASCII.GetString(headerBuffer);
            if (!string.Equals(dirListingHeader, headerString, StringComparison.Ordinal))
            {
                throw new NpkException("Did not find expected directory listing header.");
            }

            // Next is a 32-bit unsigned int that is the number of files packed in the .npk
            uint numFiles = GetUnsigned32Le();

            byte[] subNameBuffer = new byte[256];
            // Next is a listing of all the files and their location inside the file.
            for (uint fileIndex = 0; fileIndex < numFiles; fileIndex++)
            {
                // First is a 32-bit unsigned int that is the byte offset in the .npk of where the file is located.
                uint absoluteLocation = GetUnsigned32Le();
                // Followed by the size of the file in bytes
                uint size = GetUnsigned32Le();
                // And then the path of the file, including the prefix indicating whether it is an image
                // (sprite/) or a sound (sounds/)
                // There are always 256 bytes to be read here.
                // Each byte read is XOR'ed with the corresponding byte in the key.
                // Then the bytes can be treated as a null-terminated ASCII string.
                m_npkStream.ReadOrDie(subNameBuffer, subNameBuffer.Length);

                for (int keyIndex = 0; keyIndex < subNameBuffer.Length; keyIndex++)
                {
                    subNameBuffer[keyIndex] ^= s_key[keyIndex];
                }

                string subNameString = Encoding.ASCII.GetString(subNameBuffer);
                subNameString = subNameString.TrimEnd('\0');
                NpkPath pathWithPrefix = new NpkPath(subNameString);

                // That gives a path like sprite/character/gunner/effect/aerialdashattack.img
                // We need to strip off the sprite/ or sound/ prefix.
                // The following code assumes that a prefix of, say, sprite// or sprite\ is still valid.
                // If not, the code could be simplified.
                IList<NpkPath> pathComponents = pathWithPrefix.GetPathComponents();
                if (pathComponents.Count >= 1)
                {
                    // Build up the NpkPath that is the same NpkPath but without the first component
                    NpkPath pathWithoutPrefix = new NpkPath("");
                    for (int i = 1; i < pathComponents.Count; i++)
                    {
                        pathWithoutPrefix = NpkPath.Combine(pathWithoutPrefix, pathComponents[i]);
                    }

                    NpkByteRange fileLocation = new NpkByteRange(absoluteLocation, size);
                    if (pathComponents[0].Equals("sprite"))
                    {
                        m_imageFileLocations[pathWithoutPrefix] = fileLocation;
                    }
                    else if (pathComponents[0].Equals("sounds"))
                    {
                        m_soundFileLocations[pathWithoutPrefix] = fileLocation;
                    }
                    else
                    {
                        ; // Not an image or a sound. Ignore it I guess, no sense throwing an exception.
                        // Don't break any programs just because a new file type was added or something.
                    }
                }
                else
                {
                    ; // empty path? O_o Ignore it I guess.
                }
            }
        }

        /// <summary>
        /// Helper function that loads a sprite file's metadata, setting its value in m_images and
        /// m_frameLocations. The .npk file is assumed to be open.
        /// </summary>
        /// <exception cref="System.IO.IOException">I/O error.</exception>
        /// <exception cref="System.IO.EndOfStreamException">Unexpected end of file.</exception>
        /// <exception cref="Dfo.Parser.NpkException">The .npk file appears to be corrupt.</exception>
        private void LoadSpriteFileMetaData(NpkPath spriteFilePath, NpkByteRange spriteFileLocation)
        {
            // Seek to the sprite file's location in the .npk
            Seek(spriteFileLocation.FileOffset, SeekOrigin.Begin);

            // .img files begin with "Neople Img File\0" in ASCII
            string imageFileHeader = "Neople Img File\0";
            byte[] headerBuffer = new byte[imageFileHeader.Length];
            m_npkStream.ReadOrDie(headerBuffer, headerBuffer.Length);
            string headerString = Encoding.ASCII.GetString(headerBuffer);
            if (!string.Equals(imageFileHeader, headerString, StringComparison.Ordinal))
            {
                throw new NpkException("Did not find expected image file header.");
            }

            // Don't know what these 4 bytes, 4 bytes, and 4 bytes are
            uint unknown1 = GetUnsigned32Le();
            uint unknown2 = GetUnsigned32Le();
            uint unknown3 = GetUnsigned32Le();

            // 32-bit unsigned int - number of frames in the .img file
            uint numFrames = GetUnsigned32Le();

            List<FrameInfo> frames = new List<FrameInfo>();
            List<NpkByteRange> frameLocations = new List<NpkByteRange>();

            // Next is each frame's metadata, one after the other.
            for (uint frameIndex = 0; frameIndex < numFrames; frameIndex++)
            {
                FrameInfo frame = ReadFrameMetadata();
                frames.Add(frame);
            }

            // Next is each non-reference frame's pixel data, one after the other.
            for (uint frameIndex = 0; frameIndex < numFrames; frameIndex++)
            {
                FrameInfo frame = frames[(int)frameIndex];
                if (frame.LinkFrame != null)
                {
                    // Link frames have no pixel data
                    continue;
                }

                NpkByteRange frameByteRange = null;
                if (frame.IsCompressed)
                {
                    frameByteRange = new NpkByteRange(m_npkStream.Position, frame.CompressedLength);
                }
                else
                {
                    frameByteRange = new NpkByteRange(m_npkStream.Position, frame.CompressedLength / 2);
                }

                frameLocations.Add(frameByteRange);
                Seek(frameByteRange.Size, SeekOrigin.Current);
            }

            m_images[spriteFilePath] = frames;
            m_frameLocations[spriteFilePath] = frameLocations;
        }

        /// <summary>
        /// Reads an image frame metadata at the current position, advancing the read pointer to the next frame's metadata.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.IO.IOException">I/O error.</exception>
        /// <exception cref="System.IO.EndOfStreamException">Unexpected end of file.</exception>
        /// <exception cref="Dfo.Parser.NpkException">The .npk appears to be corrupt.</exception>
        private FrameInfo ReadFrameMetadata()
        {
            uint mode = GetUnsigned32Le();
            if (mode == 0x11u)
            {
                // reference
                uint imageLink = GetUnsigned32Le();
                return new FrameInfo(imageLink);
            }
            else // mode indicates the format of the pixel data
            {
                uint compressedField = GetUnsigned32Le();
                bool isCompressed = compressedField != 5; // 6 = true, 5 = false

                uint width = GetUnsigned32Le();
                uint height = GetUnsigned32Le();
                uint compressedLength = GetUnsigned32Le();
                uint keyX = GetUnsigned32Le();
                uint keyY = GetUnsigned32Le();
                uint maxWidth = GetUnsigned32Le();
                uint maxHeight = GetUnsigned32Le();

                long imageLocation = m_npkStream.Position;
                return new FrameInfo(isCompressed, compressedLength, mode, width, height, keyX, keyY, maxWidth, maxHeight);
            }
        }

        /// <summary>
        /// Reads an unsigned 32-bit little-endian integer from the .npk file.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.IOException">An I/O error occurred.</exception>
        /// <exception cref="System.IO.EndOfStreamException">Fewer than 4 bytes were read.</exception>
        private uint GetUnsigned32Le()
        {
            return m_npkStream.GetUnsigned32Le(m_intBuffer);
        }

        /// <summary>
        /// Reads an unsigned 16-bit little-endian integer from the .npk file.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.IOException">An I/O error occurred.</exception>
        /// <exception cref="System.IO.EndOfStreamException">Fewer than 2 bytes were read.</exception>
        private ushort GetUnsigned16Le()
        {
            return m_npkStream.GetUnsigned16Le(m_intBuffer);
        }

        /// <summary>
        /// Seeks to the given offset in the .npk file. Wraps NotSupportedException (file doesn't support seeking)
        /// as an IOException and ArgumentException (tried to seek before the beginning of the file) as an
        /// NpkException.
        /// </summary>
        /// <param name="absolutePosition"></param>
        /// <param name="origin"></param>
        /// <returns></returns>
        /// <exception cref="System.IO.IOException">An I/O error occurred or the stream does not support seeking.</exception>
        /// <exception cref="Dfo.Parser.NpkException">Tried to seek before the beginning of the file.</exception>
        private long Seek(long absolutePosition, SeekOrigin origin)
        {
            try
            {
                return m_npkStream.Seek(absolutePosition, origin);
            }
            catch (NotSupportedException ex)
            {
                throw new IOException("{0} does not support seeking.".F(PathOfNpkFile), ex);
            }
            catch (ArgumentException)
            {
                throw new NpkException("A seek tried to seek before the beginning of the file.");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="imgPath">The Npk Path of the .img file, WITHOUT the leading sprite/</param>
        /// <param name="frameIndex"></param>
        /// <returns></returns>
        /// <exception cref="System.IO.FileNotFoundException">The img file does not exist in this .npk file
        /// or no frame with the given index exists in the img file.</exception>
        /// <exception cref="System.IO.IOException">An I/O error occurred.</exception>
        /// <exception cref="Dfo.Parser.NpkException">The .npk file is corrupt or the format changed.</exception>
        public Image GetImage(NpkPath imgPath, uint frameIndex)
        {
            imgPath.ThrowIfNull("imgPath");

            try
            {
                if (!m_images.ContainsKey(imgPath))
                {
                    throw new FileNotFoundException("{0} does not exist in {1}.".F(imgPath, PathOfNpkFile));
                }

                IList<FrameInfo> imgFrames = m_images[imgPath];

                if (frameIndex >= imgFrames.Count)
                {
                    throw new FileNotFoundException("Cannot get frame index {0} of {1}. It only has {2} frames."
                        .F(frameIndex, imgPath, imgFrames.Count));
                }

                FrameInfo frameData = imgFrames[(int)frameIndex];
                uint realFrameIndex = frameIndex;

                // Follow frame links
                while (frameData.LinkFrame != null)
                {
                    // TODO: Detect infinite links - or maybe only allow one link to be followed.
                    realFrameIndex = frameData.LinkFrame.Value;
                    if (realFrameIndex >= imgFrames.Count)
                    {
                        throw new FileNotFoundException("Cannot get linked frame index {0} of {1}. It only has {2} frames."
                            .F(realFrameIndex, imgPath, imgFrames.Count));
                    }
                    frameData = imgFrames[(int)realFrameIndex];
                }

                NpkByteRange pixelDataLocation = m_frameLocations[imgPath][(int)realFrameIndex];
                // Seek to the pixel data and read it
                Seek(pixelDataLocation.FileOffset, SeekOrigin.Begin);
                byte[] pixelData = new byte[pixelDataLocation.Size];
                m_npkStream.ReadOrDie(pixelData, pixelData.Length);

                if (frameData.IsCompressed)
                {
                    using (MemoryStream pixelDataMemoryStream = new MemoryStream(pixelData))
                    {
                        try
                        {
                            using (InflaterInputStream decompressStream = new InflaterInputStream(pixelDataMemoryStream))
                            {
                                byte[] decompressedPixelData = decompressStream.ReadFully();
                                pixelData = decompressedPixelData;
                            }
                        }
                        catch (SharpZipBaseException ex)
                        {
                            throw new NpkException(string.Format("Inflate error: {0}", ex.Message), ex);
                        }
                    }
                }

                pixelData = ExpandPixelData(pixelData, frameData);

                return new Image(pixelData, frameData);
            }
            catch (EndOfStreamException ex)
            {
                throw new NpkException("Unexpected end of file.", ex);
            }
        }

        /// <summary>
        /// Converts the raw pixel data of a frame into 32-bit RGBA format.
        /// </summary>
        /// <param name="rawPixelData"></param>
        /// <param name="frameData"></param>
        /// <returns></returns>
        /// <exception cref="NpkException">The raw pixel data is not the expected size.</exception>
        private byte[] ExpandPixelData(byte[] rawPixelData, FrameInfo frameData)
        {
            byte[] expandedPixelData = new byte[frameData.Width * frameData.Height * 4];
            switch (frameData.Mode)
            {
                case 0x0E: // ARGB 1555
                    // Make sure sizes match
                    if (rawPixelData.Length != frameData.Width * frameData.Height * 2)
                    {
                        throw new NpkException(
                            "Raw pixel data in 1555 format is {0} bytes. Expected it to be {1} bytes."
                            .F(rawPixelData.Length, frameData.Width * frameData.Height * 2));
                    }

                    for (int rawIndex = 0; rawIndex < frameData.Width * frameData.Height * 2; rawIndex += 2)
                    {
                        byte byte0 = rawPixelData[rawIndex];
                        byte byte1 = rawPixelData[rawIndex + 1];
                        byte a = (byte)(((byte1 & 0x80) >> 7) * 0xFF);
                        byte r = (byte)(((byte1 & 0x7C) << 1) | ((byte1 & 0x7C) >> 4));
                        byte g = (byte)(((byte1 & 0x03) << 6) | ((byte0 & 0xE0) >> 2));
                        g = (byte)(g | (g >> 5));
                        byte b = (byte)(((byte0 & 0x1F) << 3) | ((byte0 & 0x1F) >> 2));

                        int pixelIndex = rawIndex / 2;
                        int expandedIndex = pixelIndex * 4;
                        expandedPixelData[expandedIndex] = r;
                        expandedPixelData[expandedIndex + 1] = g;
                        expandedPixelData[expandedIndex + 2] = b;
                        expandedPixelData[expandedIndex + 3] = a;
                    }
                    break;
                case 0x0F: // 4444
                    if (rawPixelData.Length != frameData.Width * frameData.Height * 2)
                    {
                        throw new NpkException(
                            "Raw pixel data in 4444 format is {0} bytes. Expected it to be {1} bytes."
                            .F(rawPixelData.Length, frameData.Width * frameData.Height * 2));
                    }

                    for (int rawIndex = 0; rawIndex < frameData.Width * frameData.Height * 2; rawIndex += 2)
                    {
                        byte byte0 = rawPixelData[rawIndex];
                        byte byte1 = rawPixelData[rawIndex + 1];
                        byte a = (byte)(byte1 & 0xF0);
                        a = (byte)(a | (a >> 4));
                        byte r = (byte)(byte1 & 0x0F);
                        r = (byte)(r | (r << 4));
                        byte g = (byte)(byte0 & 0xF0);
                        g = (byte)(g | (g >> 4));
                        byte b = (byte)(byte0 & 0x0F);
                        b = (byte)(b | (b << 4));

                        int pixelIndex = rawIndex / 2;
                        int expandedIndex = pixelIndex * 4;
                        expandedPixelData[expandedIndex] = r;
                        expandedPixelData[expandedIndex + 1] = g;
                        expandedPixelData[expandedIndex + 2] = b;
                        expandedPixelData[expandedIndex + 3] = a;
                    }
                    break;
                case 0x10: // 8888
                    if (rawPixelData.Length != frameData.Width * frameData.Height * 4)
                    {
                        throw new NpkException(
                            "Raw pixel data in 8888 format is {0} bytes. Expected it to be {1} bytes."
                            .F(rawPixelData.Length, frameData.Width * frameData.Height * 4));
                    }

                    for (int rawIndex = 0; rawIndex < frameData.Width * frameData.Height * 4; rawIndex += 4)
                    {
                        byte r = rawPixelData[rawIndex + 2];
                        byte g = rawPixelData[rawIndex + 1];
                        byte b = rawPixelData[rawIndex];
                        byte a = rawPixelData[rawIndex + 3];

                        expandedPixelData[rawIndex] = r;
                        expandedPixelData[rawIndex + 1] = g;
                        expandedPixelData[rawIndex + 2] = b;
                        expandedPixelData[rawIndex + 3] = a;
                    }
                    break;
            }

            return expandedPixelData;
        }

        public void Dispose()
        {
            if (m_npkStream != null)
            {
                m_npkStream.Dispose();
            }
        }
    }
}
