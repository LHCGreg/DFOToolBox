using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DFO.Common;
using DFO.Common.Images;
using DFO.Utilities;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;

namespace DFO.Npk
{
    public class NpkEditor : IImageSource, IDisposable
    {
        // Used for read operations. Null if no file open yet.
        private NpkReader _reader;

        // This class owns this stream. It is separate from _reader's stream of the file. Null if no file open yet.
        private FileStream _npkStream;

        // Path of the currently open file. Null if no file open yet.
        private string _openFilePath;

        private bool _disposed = false;

        // Used for writing 32-bit ints to the stream without allocating each time
        private byte[] _intWriteBuffer = new byte[4];

        public bool IsOpen { get { return _openFilePath != null; } }

        private Dictionary<NpkPath, IReadOnlyList<FrameInfo>> _emptyFramesList = new Dictionary<NpkPath, IReadOnlyList<FrameInfo>>(0);
        public IReadOnlyDictionary<NpkPath, IReadOnlyList<FrameInfo>> Frames
        {
            get
            {
                if (_reader == null)
                {
                    return _emptyFramesList;
                }
                else
                {
                    return _reader.Frames;
                }
            }
        }

        private Dictionary<NpkPath, bool> _emptyImagesDict = new Dictionary<NpkPath, bool>(0);

        /// <summary>
        /// Accessing this property will not automatically load frame metadata for any .img's.
        /// </summary>
        public IReadOnlyDictionary<NpkPath, bool> Images
        {
            get
            {
                if (_reader == null)
                {
                    return _emptyImagesDict;
                }
                else
                {
                    return _reader.Images;
                }
            }
        }

        public NpkEditor()
        {

        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("NpkEditor");
            }
        }

        private void ThrowIfNoFileOpen()
        {
            if (!IsOpen)
            {
                throw new InvalidOperationException("There is no file open.");
            }
        }

        /// <summary>
        /// If opening fails, the state is the same as it was before the open.
        /// </summary>
        /// <param name="path"></param>
        public void Open(string path)
        {
            ThrowIfDisposed();

            // Open new file first to get any errors out of the way, then switch to it
            NpkReader newReader = null;
            FileStream newStream = null;
            try
            {
                newReader = new NpkReader(path);
                newStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            }
            catch (Exception)
            {
                if (newStream != null)
                {
                    newStream.Dispose();
                    newReader.Dispose();
                }

                throw;
            }

            NpkReader oldReader = _reader;
            FileStream oldStream = _npkStream;

            _reader = newReader;
            _npkStream = newStream;
            _openFilePath = path;

            if (oldStream != null)
            {
                oldStream.Dispose();
            }
            if (oldReader != null)
            {
                oldReader.Dispose();
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
            ThrowIfDisposed();
            ThrowIfNoFileOpen();
            _reader.PreLoadAllSpriteFrameMetadata();
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
            ThrowIfDisposed();
            ThrowIfNoFileOpen();
            _reader.PreLoadSpriteMetadata(spriteFilePath);
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
            ThrowIfDisposed();
            ThrowIfNoFileOpen();
            return _reader.GetImage(imgPath, frameIndex);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="imgPath"></param>
        /// <param name="frameIndex"></param>
        /// <param name="newFrameMetadata">If this indicates a link frame, <paramref name="newFramePixels"/> is ignored. The IsCompressed flag is honored, compressing the image if it is set. The CompressedLength field is not used.</param>
        /// <param name="newFramePixels">Readable stream consisting solely of the pixel data, in the format indicated by the metadata.</param>
        public void EditFrame(NpkPath imgPath, int frameIndex, FrameInfo newFrameMetadata, Stream newFramePixels)
        {
            ThrowIfDisposed();
            ThrowIfNoFileOpen();
            
            if (!_reader.Frames.ContainsKey(imgPath))
            {
                throw new ArgumentException("{0} is not in the NPK.".F(imgPath));
            }

            if (frameIndex >= _reader.Frames[imgPath].Count)
            {
                throw new ArgumentException("{0} does not have a frame {1}.".F(imgPath, frameIndex));
            }

            NpkFileTableEntry entryOfImgEditing = _reader.Files.Where(f => f.Name.Equals(imgPath)).First();
            IReadOnlyList<FrameInfo> frameListOfImgEditing = _reader.Frames[imgPath];
            FrameInfo frameMetadataOfFrameEditing = frameListOfImgEditing[frameIndex];

            // Render the new frame in memory

            // pixelData is null if it's a link frame
            // pixelData length may be bigger than the actual pixel data. Use newPixelDataLength instead of newPixelData.Length
            int newPixelDataLength;
            byte[] newPixelData = GetPixelData(newFrameMetadata, newFramePixels, out newPixelDataLength);
            byte[] frameMetadataBytes = GetFrameMetadataBytes(newFrameMetadata, newPixelDataLength);

            string tempNpkPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".NPK");
            using (FileStream tempFileStream = File.OpenWrite(tempNpkPath))
            {
                WriteEditedNPK(tempFileStream, imgPath, entryOfImgEditing, frameIndex, frameListOfImgEditing, frameMetadataOfFrameEditing, newFrameMetadata, newPixelData, newPixelDataLength, frameMetadataBytes);
            }

            // temp file now has the new NPK!
            // close _reader
            // close _npkStream
            // delete original file
            // move temp file

            // TODO: "refresh" it
            _reader.Dispose();
            _npkStream.Dispose();

            // TODO: Error handling
            File.Delete(_openFilePath);

            File.Move(tempNpkPath, _openFilePath);

            // reopen
            try
            {
                _reader = new NpkReader(_openFilePath);
            }
            catch (Exception)
            {
                _openFilePath = null;
                _reader = null;
                _npkStream = null;
                throw;
            }

            try
            {
                _npkStream = new FileStream(_openFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            }
            catch (Exception)
            {
                _openFilePath = null;
                _reader.Dispose();
                _reader = null;
                _npkStream = null;
                throw;
            }
        }

        private int GetImgByteCountChange(FrameInfo frameMetadataOfFrameEditing, FrameInfo newFrameMetadata, int newPixelDataLength)
        {
            // old byte count + imgByteCountChange = new byte count
            if (frameMetadataOfFrameEditing.PixelFormat == PixelDataFormat.Link && newFrameMetadata.PixelFormat == PixelDataFormat.Link)
            {
                // If frame going from link to link, .img file is same length
                return 0;
            }
            else if (frameMetadataOfFrameEditing.PixelFormat != PixelDataFormat.Link && newFrameMetadata.PixelFormat == PixelDataFormat.Link)
            {
                // Going from non-link to link
                // old length is pixel data length + metadata of 36 bytes
                // new length is 0 + metadata of 8 bytes
                return 8 - (frameMetadataOfFrameEditing.GetPixelDataLength() + 36);
            }
            else if (frameMetadataOfFrameEditing.PixelFormat == PixelDataFormat.Link && newFrameMetadata.PixelFormat != PixelDataFormat.Link)
            {
                // Going from link to non-link
                // old length is 0 + metadata of 8 bytes
                // new length is newPixelDataLength + metadata of 36 bytes
                return (newPixelDataLength + 36) - 8;
            }
            else
            {
                // Going from non-link to non-link
                // Metadata is still 36 bytes
                // difference is difference in pixel data
                return newPixelDataLength - frameMetadataOfFrameEditing.GetPixelDataLength();
            }
        }

        private uint GetImgField1Value(IReadOnlyList<FrameInfo> frameListOfImgEditing, FrameInfo frameMetadataOfFrameEditing, FrameInfo frameMetadataOfNewFrame)
        {
            int numberOfLinkFramesInImgBeforeEdit = 0;
            int numberOfNonLinkFramesInImgBeforeEdit = 0;
            foreach (FrameInfo frame in frameListOfImgEditing)
            {
                if (frame.PixelFormat == PixelDataFormat.Link)
                {
                    numberOfLinkFramesInImgBeforeEdit++;
                }
                else
                {
                    numberOfNonLinkFramesInImgBeforeEdit++;
                }
            }

            int numberOfLinkFramesInImgAfterEdit = numberOfLinkFramesInImgBeforeEdit;
            int numberOfNonLinkFramesInImgAfterEdit = numberOfNonLinkFramesInImgBeforeEdit;

            if (frameMetadataOfFrameEditing.PixelFormat == PixelDataFormat.Link)
            {
                numberOfLinkFramesInImgAfterEdit--;
            }
            else
            {
                numberOfNonLinkFramesInImgAfterEdit--;
            }

            if (frameMetadataOfNewFrame.PixelFormat == PixelDataFormat.Link)
            {
                numberOfLinkFramesInImgAfterEdit++;
            }
            else
            {
                numberOfNonLinkFramesInImgAfterEdit++;
            }

            uint field1Value = (uint)(36 * numberOfNonLinkFramesInImgAfterEdit + 8 * numberOfLinkFramesInImgAfterEdit);
            return field1Value;
        }

        private byte[] GetPixelData(FrameInfo frameMetadata, Stream pixels, out int length)
        {
            if (frameMetadata.PixelFormat == PixelDataFormat.Link)
            {
                length = 0;
                return null;
            }

            using (MemoryStream framePixelData = new MemoryStream())
            {
                if (!frameMetadata.IsCompressed)
                {
                    pixels.CopyTo(framePixelData);
                }
                else
                {
                    using (DeflaterOutputStream compresser = new DeflaterOutputStream(framePixelData))
                    {
                        compresser.IsStreamOwner = false;
                        pixels.CopyTo(compresser);
                    }
                }

                length = (int)framePixelData.Length;
                return framePixelData.GetBuffer();
            }
        }

        private byte[] GetFrameMetadataBytes(FrameInfo frameMetadata, int pixelDataByteLength)
        {
            // pixel format.
            // If link frame, linked frame index. Stop and go to next frame.
            // Otherwise...
            // 6 if compressed, 5 if not
            // width, height, length, x, y, max width, max height

            byte[] metadataBytes;
            if (frameMetadata.PixelFormat == PixelDataFormat.Link)
            {
                metadataBytes = new byte[8];
                using (MemoryStream metadataStream = new MemoryStream(metadataBytes))
                {
                    if (frameMetadata.LinkFrame == null)
                    {
                        throw new ArgumentException("Pixel data format was link but linked frame index was not set.");
                    }

                    metadataStream.WriteUnsigned32Le((uint)frameMetadata.PixelFormat, _intWriteBuffer);
                    metadataStream.WriteUnsigned32Le((uint)frameMetadata.LinkFrame.Value, _intWriteBuffer);
                }
            }
            else
            {
                metadataBytes = new byte[36];
                using (MemoryStream metadataStream = new MemoryStream(metadataBytes))
                {
                    metadataStream.WriteUnsigned32Le((uint)frameMetadata.PixelFormat, _intWriteBuffer);

                    uint compressedFieldValue;
                    if (frameMetadata.IsCompressed)
                    {
                        compressedFieldValue = 6;
                    }
                    else
                    {
                        compressedFieldValue = 5;
                    }
                    metadataStream.WriteUnsigned32Le(compressedFieldValue, _intWriteBuffer);

                    metadataStream.WriteUnsigned32Le((uint)frameMetadata.Width, _intWriteBuffer);
                    metadataStream.WriteUnsigned32Le((uint)frameMetadata.Height, _intWriteBuffer);
                    metadataStream.WriteUnsigned32Le((uint)pixelDataByteLength, _intWriteBuffer);
                    metadataStream.WriteUnsigned32Le((uint)frameMetadata.LocationX, _intWriteBuffer);
                    metadataStream.WriteUnsigned32Le((uint)frameMetadata.LocationY, _intWriteBuffer);
                    metadataStream.WriteUnsigned32Le((uint)frameMetadata.MaxWidth, _intWriteBuffer);
                    metadataStream.WriteUnsigned32Le((uint)frameMetadata.MaxHeight, _intWriteBuffer);
                }
            }

            return metadataBytes;
        }

        private void WriteEditedNPK(FileStream tempFileStream, NpkPath imgPath, NpkFileTableEntry entryOfImgEditing, int editedFrameIndex,
            IReadOnlyList<FrameInfo> frameListOfImgEditing, FrameInfo frameMetadataOfFrameEditing,
            FrameInfo newFrameMetadata, byte[] newPixelData, int newPixelDataLength, byte[] frameMetadataBytes)
        {
            int imgByteCountChange = GetImgByteCountChange(frameMetadataOfFrameEditing, newFrameMetadata, newPixelDataLength);
            List<NpkByteRange> frameLocationsOfImgEditing = _reader.FrameLocations[imgPath];

            // Header is same
            tempFileStream.Write(NpkReader.s_headerBytes, 0, NpkReader.s_headerBytes.Length);

            // number of files is same
            tempFileStream.WriteUnsigned32Le((uint)_reader.Files.Count, _intWriteBuffer);

            byte[] imgPathBytes = new byte[256];

            // Write file table
            WritedEditedFileTable(tempFileStream, entryOfImgEditing, imgByteCountChange, imgPathBytes);

            // Write from original stream until offset of file we're changing
            // Write from current until currentEntry.Location.FileOffset
            _npkStream.Seek(tempFileStream.Position, SeekOrigin.Begin);
            int numBytesToCopy = (int)(entryOfImgEditing.Location.FileOffset - tempFileStream.Position);
            _npkStream.CopyToPartially(tempFileStream, numBytesToCopy);

            // Now write the new .img

            WriteEditedImg(tempFileStream, editedFrameIndex, frameListOfImgEditing, frameMetadataOfFrameEditing, newFrameMetadata, newPixelData, newPixelDataLength, frameMetadataBytes, frameLocationsOfImgEditing);

            // now write the rest of the original file that's after this .img
            if (entryOfImgEditing.Location.FileOffset + entryOfImgEditing.Location.Size < _npkStream.Length)
            {
                _npkStream.Seek(entryOfImgEditing.Location.FileOffset + entryOfImgEditing.Location.Size, SeekOrigin.Begin);
                numBytesToCopy = (int)_npkStream.Length - (entryOfImgEditing.Location.FileOffset + entryOfImgEditing.Location.Size);
                _npkStream.CopyToPartially(tempFileStream, numBytesToCopy);
            }
        }

        private void WriteEditedImg(FileStream tempFileStream, int editedFrameIndex, IReadOnlyList<FrameInfo> frameListOfImgEditing, FrameInfo frameMetadataOfFrameEditing, FrameInfo newFrameMetadata, byte[] newPixelData, int newPixelDataLength, byte[] frameMetadataBytes, List<NpkByteRange> frameLocationsOfImgEditing)
        {
            // Need to know exact ordering of files
            // Need to know ALL files, even non-image files
            // Need to update offset of all files with an offset greater or equal to the one we're changing
            // Need to update file size of file that we're changing and any that occupy the same space

            //.img files begin with "Neople Img File\0" in ASCII
            tempFileStream.Write(NpkReader.s_imgHeaderBytes, 0, NpkReader.s_imgHeaderBytes.Length);
            // field 1 - (36 * (# non-link frames) + 8 * (# link frames))
            //uint field1Value = (uint)(36 * numberOfNonLinkFramesInImgBeforeEdit + 8 * numberOfLinkFramesInImgBeforeEdit);
            uint field1Value = GetImgField1Value(frameListOfImgEditing, frameMetadataOfFrameEditing, newFrameMetadata);
            tempFileStream.WriteUnsigned32Le(field1Value, _intWriteBuffer);
            // field 2 - always 0
            tempFileStream.WriteUnsigned32Le(0, _intWriteBuffer);
            // field 3 - always 2
            tempFileStream.WriteUnsigned32Le(2, _intWriteBuffer);
            // frame count
            tempFileStream.WriteUnsigned32Le((uint)frameListOfImgEditing.Count, _intWriteBuffer);

            // frame metadata for each frame in order
            for (int frameIndexToWrite = 0; frameIndexToWrite < frameListOfImgEditing.Count; frameIndexToWrite++)
            {
                if (frameIndexToWrite != editedFrameIndex)
                {
                    FrameInfo frameToCopy = frameListOfImgEditing[frameIndexToWrite];
                    byte[] metadataBytes = GetFrameMetadataBytes(frameToCopy, frameToCopy.CompressedLength);
                    tempFileStream.Write(metadataBytes, 0, metadataBytes.Length);
                }
                else
                {
                    tempFileStream.Write(frameMetadataBytes, 0, frameMetadataBytes.Length);
                }
            }

            // frame pixel data for each non-link frame in order
            for (int frameIndexToWrite = 0; frameIndexToWrite < frameListOfImgEditing.Count; frameIndexToWrite++)
            {
                if (frameIndexToWrite != editedFrameIndex)
                {
                    FrameInfo frameToCopy = frameListOfImgEditing[frameIndexToWrite];
                    if (frameToCopy.PixelFormat != PixelDataFormat.Link)
                    {
                        NpkByteRange frameToWriteOriginalLocation = frameLocationsOfImgEditing[frameIndexToWrite];
                        _npkStream.Seek(frameToWriteOriginalLocation.FileOffset, SeekOrigin.Begin);
                        _npkStream.CopyToPartially(tempFileStream, frameToWriteOriginalLocation.Size);
                    }
                }
                else
                {
                    // Write the pixel data for the frame we're changing, if it's not a link frame
                    if (newFrameMetadata.PixelFormat != PixelDataFormat.Link)
                    {
                        tempFileStream.Write(newPixelData, 0, newPixelDataLength);
                    }
                }
            }
        }

        private void WritedEditedFileTable(FileStream tempFileStream, NpkFileTableEntry entryOfImgEditing, int imgByteCountChange, byte[] imgPathBytes)
        {
            foreach (NpkFileTableEntry file in _reader.Files)
            {
                // uint - file location offset of file
                // uint - file size in bytes
                // 256 bytes for NPK path (eg sprite/foo/bar.img) - The path in ASCII is padded with 0 bytes to 256 bytes then xor'd with a static "key"

                if (file.Location.FileOffset <= entryOfImgEditing.Location.FileOffset)
                {
                    // If this file's bytes comes before the file whose bytes we're changing, its offset does not change.
                    // Same if it's the same file whose bytes we're changing or one that uses the same bytes.
                    tempFileStream.WriteUnsigned32Le((uint)file.Location.FileOffset, _intWriteBuffer);
                }
                else
                {
                    // otherwise, add the difference (possibly negative) between sizes of new file and old file
                    int newOffset = file.Location.FileOffset + imgByteCountChange;
                    tempFileStream.WriteUnsigned32Le((uint)newOffset, _intWriteBuffer);
                }

                if (file.Location.FileOffset != entryOfImgEditing.Location.FileOffset)
                {
                    // If this file is not the file we're changing or one that uses the same bytes, the size does not change
                    tempFileStream.WriteUnsigned32Le((uint)file.Location.Size, _intWriteBuffer);
                }
                else
                {
                    int newImgSize = file.Location.Size + imgByteCountChange;
                    tempFileStream.WriteUnsigned32Le((uint)newImgSize, _intWriteBuffer);
                }

                for (int i = 0; i < 256; i++)
                {
                    imgPathBytes[i] = 0;
                }

                Encoding.ASCII.GetBytes(file.Name.Path, 0, file.Name.Path.Length, imgPathBytes, 0);
                for (int i = 0; i < 256; i++)
                {
                    imgPathBytes[i] ^= NpkReader.s_key[i];
                }

                tempFileStream.Write(imgPathBytes, 0, 256);
            }
        }

        public void Dispose()
        {
            if (_reader != null)
            {
                _reader.Dispose();
                _reader = null;
            }
            if (_npkStream != null)
            {
                _npkStream.Dispose();
                _npkStream = null;
            }

            _openFilePath = null;
            _disposed = true;
        }
    }
}
