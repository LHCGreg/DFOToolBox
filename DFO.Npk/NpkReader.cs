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

namespace DFO.Npk
{
    public partial class NpkReader : IImageSource, IDisposable
    {
        private Stream m_npkStream;
        private bool m_disposeStream;

        private byte[] m_intBuffer = new byte[4];

        private static string s_keyString = "puchikon@neople dungeon and fighter DNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNFDNF\0";
        private static byte[] s_key = Encoding.ASCII.GetBytes(s_keyString);

        private static IDictionary<PixelDataFormat, int> s_formatToBytesPerPixel = new Dictionary<PixelDataFormat, int>()
        {
            { PixelDataFormat.EightEightEightEight, 4 },
            { PixelDataFormat.FourFourFourFour, 2 },
            { PixelDataFormat.OneFiveFiveFive, 2 },
            { PixelDataFormat.Link, 0 }
        };

        private Dictionary<NpkPath, NpkByteRange> m_imageFileLocations = new Dictionary<NpkPath, NpkByteRange>();
        private Dictionary<NpkPath, bool> m_imagesInFile = new Dictionary<NpkPath, bool>();
        public IReadOnlyDictionary<NpkPath, bool> Images { get { return m_imagesInFile; } }

        private Dictionary<NpkPath, NpkByteRange> m_soundFileLocations = new Dictionary<NpkPath, NpkByteRange>();

        private Dictionary<NpkPath, List<NpkByteRange>> m_frameLocations = new Dictionary<NpkPath, List<NpkByteRange>>();

        // If this is re-set, Images must be updated to point to the new value.
        private Dictionary<NpkPath, List<FrameInfo>> m_frames = new Dictionary<NpkPath, List<FrameInfo>>();

        // Initialized in constructor
        /// <summary>
        /// Dictionary of NpkPath to frame metadata. Frame metadata is automatically loaded for requested paths if they
        /// have not been loaded yet.
        /// </summary>
        public IReadOnlyDictionary<NpkPath, IReadOnlyList<FrameInfo>> Frames { get; private set; }

        private IDictionary<NpkPath, SoundInfo> m_sounds = new Dictionary<NpkPath, SoundInfo>();
        // Initialized in constructor
        /// <summary>
        /// Not currently populated.
        /// </summary>
        public IReadOnlyDictionary<NpkPath, SoundInfo> Sounds { get; private set; }

        /// <summary>
        /// Set to true to do extra error checks that are not normally done because they have a performance hit.
        /// Errors are notified by firing the ErrorDetected event instead of throwing an exception so all errors can be listed.
        /// For use by automated tests
        /// </summary>
        internal bool DoExtraErrorChecks { get; set; }

        /// <summary>
        /// Used to notify of an error found when doing additional error checking for automated tests that are skipped
        /// during normal use.
        /// </summary>
        internal event Action<object, ErrorDetectedEventArgs> ErrorDetected;

        internal class ErrorDetectedEventArgs
        {
            public string Message { get; private set; }

            public ErrorDetectedEventArgs(string message)
            {
                Message = message;
            }
        }

        private void OnErrorDetected(string message)
        {
            ErrorDetectedEventArgs args = new ErrorDetectedEventArgs(message);
            if (ErrorDetected != null)
            {
                ErrorDetected(this, args);
            }
        }

        /// <summary>
        /// Opens the NPK file and reads the metadata for each packed file. Metadata for the frames in the
        /// packed .img files are lazy loaded and can be preloaded with PreLoadAllSpriteFrameMetadata or
        /// PreLoadSpriteMetadata.
        /// </summary>
        /// <param name="pathOfNpkFile"></param>
        /// <exception cref="System.ArgumentNullException"><paramref name="pathOfNpkFile"/> is null.</exception>
        /// <exception cref="System.IO.FileNotFoundException">The .npk file does not exist.</exception>
        /// <exception cref="System.IO.IOException">An I/O error occurred.</exception>
        /// <exception cref="Dfo.Npk.NpkException">The .npk file is corrupt or the format changed.</exception>
        /// <exception cref="System.UnauthorizedAccessException">The caller does not have the required
        /// permission or the file is actually a directory.</exception>
        public NpkReader(string pathOfNpkFile)
            : this(pathOfNpkFile, extraErrorHandler: null)
        {

        }

        internal NpkReader(string pathOfNpkFile, Action<object, ErrorDetectedEventArgs> extraErrorHandler)
        {
            if (extraErrorHandler != null)
            {
                DoExtraErrorChecks = true;
                ErrorDetected += extraErrorHandler;
            }
            pathOfNpkFile.ThrowIfNull("pathOfNpkFile");
            m_npkStream = OpenNpkFile(pathOfNpkFile);
            m_disposeStream = true;
            Initialize();
        }

        /// <summary>
        /// Opens the NPK file and reads the metadata for each packed file. Metadata for the frames in the
        /// packed .img files are lazy loaded and can be preloaded with PreLoadAllSpriteFrameMetadata or
        /// PreLoadSpriteMetadata.
        /// </summary>
        /// <param name="npkStream">A seekable stream of an NPK file, with the read pointer at the beginning of the file.
        /// The stream will be disposed when this object is disposed</param>
        /// <exception cref="System.ArgumentNullException"><paramref name="npkStream"/> is null.</exception>
        /// <exception cref="System.IO.IOException">An I/O error occurred.</exception>
        /// <exception cref="Dfo.Npk.NpkException">The .npk file is corrupt or the format changed.</exception>
        public NpkReader(Stream npkStream)
            : this(npkStream, disposeStream: true, extraErrorHandler: null)
        {

        }

        /// <summary>
        /// Opens the NPK file and reads the metadata for each packed file. Metadata for the frames in the
        /// packed .img files are lazy loaded and can be preloaded with PreLoadAllSpriteFrameMetadata or
        /// PreLoadSpriteMetadata.
        /// </summary>
        /// <param name="npkStream">A seekable stream of an NPK file, with the read pointer at the beginning of the file.
        /// The stream will be disposed when this object is disposed</param>
        /// <param name="disposeStream">If true, dispose the stream when this object is disposed.</param>
        /// <exception cref="System.ArgumentNullException"><paramref name="npkStream"/> is null.</exception>
        /// <exception cref="System.IO.IOException">An I/O error occurred.</exception>
        /// <exception cref="Dfo.Npk.NpkException">The .npk file is corrupt or the format changed.</exception>
        public NpkReader(Stream npkStream, bool disposeStream)
            : this(npkStream, disposeStream, extraErrorHandler: null)
        {

        }

        internal NpkReader(Stream npkStream, bool disposeStream, Action<object, ErrorDetectedEventArgs> extraErrorHandler)
        {
            if (extraErrorHandler != null)
            {
                DoExtraErrorChecks = true;
                ErrorDetected += extraErrorHandler;
            }

            npkStream.ThrowIfNull("npkStream");
            m_npkStream = npkStream;
            m_disposeStream = disposeStream;
            Initialize();
        }

        private void Initialize()
        {
            try
            {
                Frames = new LazyFramesReadOnlyDictionary(this);
                Sounds = new Dictionary<NpkPath, SoundInfo>(m_sounds);

                LoadNpkFileTable();
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
        /// <param name="pathOfNpkFile"></param>
        /// <exception cref="System.IO.FileNotFoundException">The .npk file does not exist.</exception>
        /// <exception cref="System.IO.IOException">An I/O error occurred.</exception>
        /// <exception cref="System.UnauthorizedAccessException">The caller does not have the required
        /// permission or the file is actually a directory.</exception>
        private FileStream OpenNpkFile(string pathOfNpkFile)
        {
            try
            {
                return File.Open(pathOfNpkFile, FileMode.Open, FileAccess.Read, FileShare.Read);
            }
            catch (Exception ex)
            {
                // Wrap these as a FileNotFound
                if (ex is ArgumentException || ex is PathTooLongException || ex is DirectoryNotFoundException
                || ex is NotSupportedException)
                {
                    throw new FileNotFoundException("{0} does not exist.".F(pathOfNpkFile), pathOfNpkFile, ex);
                }
                else
                {
                    throw;
                }
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
        /// <exception cref="DFO.Npk.NpkException">The file is corrupt or the format has changed.</exception>
        private void LoadNpkFileTable()
        {
            // file starts with "NeoplePack_Bill\0" in ASCII
            try
            {
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
                    IList<NpkPath> pathComponents = pathWithPrefix.GetPathComponents();
                    if (pathComponents.Count >= 1)
                    {
                        NpkByteRange fileLocation = new NpkByteRange(absoluteLocation, (int)size);
                        if (pathComponents[0].Equals("sprite"))
                        {
                            m_imageFileLocations[pathWithPrefix] = fileLocation;
                            m_imagesInFile[pathWithPrefix] = true;
                        }
                        else if (pathComponents[0].Equals("sounds"))
                        {
                            m_soundFileLocations[pathWithPrefix] = fileLocation;
                        }
                        else
                        {
                            // Not an image or a sound. Ignore it I guess, no sense throwing an exception.
                            // Don't break any programs just because a new file type was added or something.
                            OnErrorDetected("Something other than a sprite or sounds file at packed file index {0}: {1}".F(fileIndex, pathComponents[0]));
                        }
                    }
                    else
                    {
                        // empty path? O_o Ignore it I guess.
                        OnErrorDetected("Empty path at packed file index {0}.".F(fileIndex));
                    }
                }
            }
            catch (EndOfStreamException ex)
            {
                throw new NpkException("Unexpected end of file.", ex);
            }
        }

        /// <summary>
        /// Preloads frame metadata for all .img files in the NPK.
        /// If the metadata has already been loaded, does nothing.
        /// Metadata for an .img's frames are loaded on demand otherwise.
        /// </summary>
        /// <exception cref="System.IO.IOException">An I/O error occurred.</exception>
        /// <exception cref="Dfo.Npk.NpkException">The .npk file is corrupt or the format changed.</exception>
        public void PreLoadAllSpriteFrameMetadata()
        {
            foreach (KeyValuePair<NpkPath, NpkByteRange> pathLocationPair in m_imageFileLocations)
            {
                LoadSpriteFileMetaData(pathLocationPair.Key, pathLocationPair.Value);
            }
        }

        /// <summary>
        /// Preloads frame metadata for the .img file with the given path with a leading sprite/ if present.
        /// If the metadata has already been loaded, does nothing.
        /// Metadata for an .img's frames are loaded on demand otherwise.
        /// </summary>
        /// <param name="spriteFilePath">NPK path of the .img file to preload. Must contain a leading sprite/ if present in the actual path.</param>
        /// <exception cref="System.IO.FileNotFoundException">There is no .img file in the NPK with the given path.</exception>
        /// <exception cref="System.IO.IOException">An I/O error occurred.</exception>
        /// <exception cref="Dfo.Npk.NpkException">The .npk file is corrupt or the format changed.</exception>
        public void PreLoadSpriteMetadata(NpkPath spriteFilePath)
        {
            NpkByteRange spriteFileLocation;
            if (!m_imageFileLocations.TryGetValue(spriteFilePath, out spriteFileLocation))
            {
                throw new FileNotFoundException(string.Format("There is no .img file with path {0} in this NPK.", spriteFilePath));
            }

            LoadSpriteFileMetaData(spriteFilePath, spriteFileLocation);
        }

        /// <summary>
        /// Loads a sprite file's metadata, setting its value in m_images and
        /// m_frameLocations. The .npk file is assumed to be open.
        /// </summary>
        /// <param name="lookForErrors">Do some extra checks to look for errors in the NPK reading code. For use by automated tests.</param>
        /// <exception cref="System.IO.IOException">I/O error.</exception>
        /// <exception cref="Dfo.Npk.NpkException">The .npk file appears to be corrupt or the format has changed.</exception>
        private void LoadSpriteFileMetaData(NpkPath spriteFilePath, NpkByteRange spriteFileLocation)
        {
            // If already loaded, return
            if (m_frames.ContainsKey(spriteFilePath))
            {
                return;
            }

            try
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
                    throw new NpkException("Did not find expected image file header when reading {0}.".F(spriteFilePath));
                }

                // Don't know what these 4 bytes, 4 bytes, and 4 bytes are
                uint unknown1 = GetUnsigned32Le();
                uint unknown2 = GetUnsigned32Le();
                uint unknown3 = GetUnsigned32Le();

                // 32-bit unsigned int - number of frames in the .img file
                uint numFrames = GetUnsigned32Le();

                List<FrameInfo> frames = new List<FrameInfo>((int)numFrames);
                List<NpkByteRange> frameLocations = new List<NpkByteRange>((int)numFrames);

                // Next is each frame's metadata, one after the other.
                for (uint frameIndex = 0; frameIndex < numFrames; frameIndex++)
                {
                    FrameInfo frame = ReadFrameMetadata();
                    frames.Add(frame);
                }

                // Next is each non-reference frame's pixel data, one after the other.
                long currentFramePosition = m_npkStream.Position;
                for (uint frameIndex = 0; frameIndex < numFrames; frameIndex++)
                {
                    FrameInfo frame = frames[(int)frameIndex];
                    if (frame.LinkFrame != null)
                    {
                        // Link frames have no pixel data
                        // Could set this to referenced frame's data to simplify code elsewhere?
                        frameLocations.Add(new NpkByteRange(0, 0));
                        continue;
                    }

                    NpkByteRange frameByteRange;
                    if (frame.IsCompressed)
                    {
                        frameByteRange = new NpkByteRange(currentFramePosition, frame.CompressedLength);
                    }
                    else
                    {
                        int length = frame.Width * frame.Height * s_formatToBytesPerPixel[frame.PixelFormat];
                        frameByteRange = new NpkByteRange(currentFramePosition, length);
                    }

                    frameLocations.Add(frameByteRange);

                    currentFramePosition += frameByteRange.Size;

                    // No need to seek through the pixel data normally.
                    // Do it when doing extra error checks to verify that after the pixel data of all the frames
                    // is either another img file or EOF.
                    if (DoExtraErrorChecks)
                    {
                        Seek(frameByteRange.Size, SeekOrigin.Current);
                    }
                }

                if (DoExtraErrorChecks)
                {
                    // Check for invalid link frames
                    for (uint frameIndex = 0; frameIndex < numFrames; frameIndex++)
                    {
                        FrameInfo frame = frames[(int)frameIndex];
                        if (frame.LinkFrame != null && (frame.LinkFrame.Value >= numFrames || frame.LinkFrame.Value < 0))
                        {
                            OnErrorDetected("{0}, invalid link frame index from {1} to {2}.".F(spriteFilePath, frameIndex, frame.LinkFrame.Value));
                        }

                        if (frame.LinkFrame != null)
                        {
                            FrameInfo linkedFrame = frames[frame.LinkFrame.Value];
                            if (linkedFrame.LinkFrame != null)
                            {
                                OnErrorDetected("{0}, link frame to a link frame, {1} to {2}.".F(spriteFilePath, frameIndex, frame.LinkFrame.Value));
                            }
                        }
                    }
                    
                    // Should be "Neople Img File" or EOF
                    byte[] nextImgHeaderBuf = new byte[15];
                    int bytesRead = m_npkStream.Read(nextImgHeaderBuf, 0, 15);
                    if (bytesRead == 0)
                    {
                        // EOF, we're ok
                    }
                    else if (bytesRead != 15)
                    {
                        OnErrorDetected(string.Format("{0}, {1} bytes read instead of 15 or 0.", spriteFilePath.Path, bytesRead));
                    }
                    else
                    {
                        string nextImgHeader = Encoding.ASCII.GetString(nextImgHeaderBuf);
                        if (nextImgHeader != "Neople Img File")
                        {
                            OnErrorDetected(string.Format("{0}, header is not Neople Img File.", spriteFilePath.Path));
                        }
                    }
                }

                m_frames[spriteFilePath] = frames;
                m_frameLocations[spriteFilePath] = frameLocations;
            }
            catch (EndOfStreamException ex)
            {
                throw new NpkException("Unexpected end of file.", ex);
            }
        }

        /// <summary>
        /// Reads an image frame metadata at the current position, advancing the read pointer to the next frame's metadata.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.IO.IOException">I/O error.</exception>
        /// <exception cref="System.IO.EndOfStreamException">Unexpected end of file.</exception>
        /// <exception cref="Dfo.Npk.NpkException">The .npk appears to be corrupt.</exception>
        private FrameInfo ReadFrameMetadata()
        {
            uint mode = GetUnsigned32Le();

            if (DoExtraErrorChecks)
            {
                if ((int)mode != (int)PixelDataFormat.Link && (int)mode != (int)PixelDataFormat.OneFiveFiveFive
                    && (int)mode != (int)PixelDataFormat.FourFourFourFour && (int)mode != (int)PixelDataFormat.EightEightEightEight)
                {
                    OnErrorDetected("Frame with unknown pixel format {0}.".F(mode));
                }
            }

            if ((int)mode == (int)PixelDataFormat.Link)
            {
                // reference
                uint imageLink = GetUnsigned32Le();
                return new FrameInfo((int)imageLink);
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

                return new FrameInfo(isCompressed, (int)compressedLength, (PixelDataFormat)mode, (int)width, (int)height, (int)keyX, (int)keyY, (int)maxWidth, (int)maxHeight);
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
        /// <exception cref="Dfo.Npk.NpkException">Tried to seek before the beginning of the file.</exception>
        private long Seek(long absolutePosition, SeekOrigin origin)
        {
            try
            {
                return m_npkStream.Seek(absolutePosition, origin);
            }
            catch (NotSupportedException ex)
            {
                throw new IOException("The NPK stream does not support seeking.", ex);
            }
            catch (ArgumentException)
            {
                throw new NpkException("A seek tried to seek before the beginning of the file.");
            }
        }

        /// <summary>
        /// Loads the pixels of a frame.
        /// </summary>
        /// <param name="imgPath">The Npk Path of the .img file, with the leading sprite/ if present</param>
        /// <param name="frameIndex"></param>
        /// <returns></returns>
        /// <exception cref="System.IO.FileNotFoundException">The img file does not exist in this .npk file
        /// or no frame with the given index exists in the img file.</exception>
        /// <exception cref="System.IO.IOException">An I/O error occurred.</exception>
        /// <exception cref="Dfo.Npk.NpkException">The .npk file is corrupt or the format changed.</exception>
        public Image GetImage(NpkPath imgPath, int frameIndex)
        {
            imgPath.ThrowIfNull("imgPath");

            try
            {
                PreLoadSpriteMetadata(imgPath);

                IList<FrameInfo> imgFrames = m_frames[imgPath];

                if (frameIndex >= imgFrames.Count || frameIndex < 0)
                {
                    throw new FileNotFoundException("Cannot get frame index {0} of {1}. It only has {2} frames."
                        .F(frameIndex, imgPath, imgFrames.Count));
                }

                FrameInfo frameData = imgFrames[frameIndex];
                int realFrameIndex = frameIndex;

                // Follow frame links
                if (frameData.LinkFrame != null)
                {
                    realFrameIndex = frameData.LinkFrame.Value;
                    if (realFrameIndex >= imgFrames.Count || realFrameIndex < 0)
                    {
                        throw new FileNotFoundException("Cannot get linked frame index {0} of {1}. It only has {2} frames."
                            .F(realFrameIndex, imgPath, imgFrames.Count));
                    }
                    frameData = imgFrames[realFrameIndex];

                    if (frameData.LinkFrame != null)
                    {
                        throw new NpkException(
                            "There is a link frame to another link frame which is not allowed. {0} frame {1} links to frame {2}."
                            .F(imgPath, frameIndex, realFrameIndex));
                    }
                }

                NpkByteRange pixelDataLocation = m_frameLocations[imgPath][realFrameIndex];
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
            switch (frameData.PixelFormat)
            {
                case PixelDataFormat.OneFiveFiveFive:
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
                case PixelDataFormat.FourFourFourFour:
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
                case PixelDataFormat.EightEightEightEight:
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
            if (m_npkStream != null && m_disposeStream)
            {
                m_npkStream.Dispose();
            }
        }
    }
}
