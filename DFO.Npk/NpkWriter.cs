//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using DFO.Common;
//using DFO.Common.Images;

//namespace DFO.Npk
//{
//    public class NpkWriter : IDisposable
//    {
//        private Stream _output;
//        private bool _closeStream;
//        private bool _headerWritten = false;
//        private int _numFilesToWrite;
//        private int _numFilesWritten = 0;
//        private InnerFileInfo[] _innerFileInfo;
//        private byte[] _intBuffer = new byte[4];

//        private static readonly string NpkHeaderString = "NeoplePack_Bill\0";
//        private static readonly byte[] NpkHeaderBytes = Encoding.ASCII.GetBytes(NpkHeaderString);

//        private static readonly string ImgHeaderString = "Neople Img File\0";
//        private static readonly byte[] ImgHeaderBytes = Encoding.ASCII.GetBytes(ImgHeaderString);

//        private static readonly byte[] NamePlaceholder = new byte[256];

//        private struct InnerFileInfo
//        {
//            public NpkPath NpkPath;
//            public uint Offset;
//            public uint Length;
//        }

//        /// <summary>
//        /// 
//        /// </summary>
//        /// <param name="outputStream">Must be a seekable and writeable stream that is at position 0.</param>
//        public NpkWriter(Stream outputStream)
//            : this(outputStream, closeStream: true)
//        {

//        }

//        /// <summary>
//        /// 
//        /// </summary>
//        /// <param name="outputStream">Must be a seekable and writeable stream that is at position 0.</param>
//        /// <param name="closeStream">If true, dispose <paramref name="outputStream"/> when this object is Disposed.</param>
//        public NpkWriter(Stream outputStream, bool closeStream)
//        {
//            _output = outputStream;
//            _closeStream = closeStream;
//        }

//        public void WriteHeader(int numFiles)
//        {
//            if (_headerWritten)
//            {
//                throw new InvalidOperationException("Cannot write NPK header more than once.");
//            }
//            if (numFiles < 0)
//            {
//                throw new ArgumentOutOfRangeException("numFiles", numFiles, "Number of files to be written in an NPK must be greater than 0.");
//            }

//            // Header: "NeoplePack_Bill\0"
//            _output.Write(NpkHeaderBytes, 0, NpkHeaderBytes.Length);

//            // uint - number of files
//            WriteUint32Le((uint)numFiles);

//            // for each file
//            for (int i = 0; i < numFiles; i++)
//            {
//                // uint - file location offset of file - to be filled in later since we don't know how big each file is yet
//                WriteUint32Le(0);

//                // uint - file size in bytes - to be filled in later since we don't know how big each file is yet
//                WriteUint32Le(0);

//                // 256 bytes for NPK path (eg sprite/foo/bar.img)
//                // The path in ASCII is padded with 0 bytes to 256 bytes then xor'd with a static "key"
//                // We don't know this until we write each file, so just write 256 bytes for now.
//                _output.Write(NamePlaceholder, 0, 256);
//            }

//            _headerWritten = true;
//            _numFilesToWrite = numFiles;
//            _innerFileInfo = new InnerFileInfo[numFiles];
//        }

//        public void WriteImg(NpkPath path, IList<FrameInfo> frames, IImageSource pixelSource)
//        {
//            // The first field in an img is 36 * (# non-link frames) + 8 * (# non-link frames)
//            // Not sure what that's for, but that's what it is
//            uint imgField1 = 0;
//            foreach (FrameInfo frame in frames)
//            {
//                if (frame.LinkFrame != null)
//                {
//                    imgField1 += 36;
//                }
//                else
//                {
//                    imgField1 += 8;
//                }
//            }
//            uint imgField2 = 0;
//            uint imgField3 = 2;
//            WriteImg(path, imgField1, imgField2, imgField3, frames, pixelSource);
//        }

//        /// <summary>
//        /// The first three fields of an img file are somewhat unknown. We're pretty sure what the first one is, and the second
//        /// and third fields are always the same in current NPK files. If you are modifying an existing NPK however, you may
//        /// consider it safest to use the values from the original NPK.
//        /// </summary>
//        /// <param name="path"></param>
//        /// <param name="imgField1"></param>
//        /// <param name="imgField2"></param>
//        /// <param name="imgField3"></param>
//        /// <param name="frames"></param>
//        /// <param name="pixelSource"></param>
//        public void WriteImg(NpkPath path, uint imgField1, uint imgField2, uint imgField3, IList<FrameInfo> frames, IImageSource pixelSource)
//        {
//            if (!_headerWritten)
//            {
//                throw new InvalidOperationException("Must write NPK header before writing img files.");
//            }

//            if (_numFilesWritten >= _numFilesToWrite)
//            {
//                throw new InvalidOperationException("Have already written the number of inner files to be written.");
//            }

//            // .img files begin with "Neople Img File\0" in ASCII
//            _output.Write(ImgHeaderBytes, 0, ImgHeaderBytes.Length);

//            // field1, field2, field3, # frames, each frame's metadata, each frame's pixels
//            WriteUint32Le(imgField1);
//            WriteUint32Le(imgField2);
//            WriteUint32Le(imgField3);
//            WriteUint32Le((uint)frames.Count);

//            foreach (FrameInfo frame in frames)
//            {
//                if (frame.LinkFrame != null)
//                {
//                    // If link frame
//                    // 32-bit unsigned int - pixel format
//                    // 32-bit unsigned int - target frame index
//                    WriteUint32Le((uint)PixelDataFormat.Link);
//                    WriteUint32Le((uint)frame.LinkFrame.Value);
//                    continue;
//                }

//                // otherwise
//                // 32-bit unsigned int - pixel format
//                // 32-bit unsigned int - 6 if compressed, 5 if not
//                // 32-bit unsigned int - width
//                // 32-bit unsigned int - height
//                // 32-bit unsigned int - compressed length
//                // X
//                // Y
//                // max width
//                // max height

//                WriteUint32Le((uint)frame.PixelFormat);

//                uint compressedFieldValue;
//                if (frame.IsCompressed)
//                {
//                    compressedFieldValue = 6;
//                }
//                else
//                {
//                    compressedFieldValue = 5;
//                }
//                WriteUint32Le(compressedFieldValue);

//                if (frame.Width <= 0)
//                {
//                    throw new ArgumentException(string.Format("Width of a frame cannot be {0}.", frame.Width));
//                }
//                WriteUint32Le((uint)frame.Width);

//                if (frame.Height <= 0)
//                {
//                    throw new ArgumentException(string.Format("Height of a frame cannot be {0}.", frame.Height));
//                }
//                WriteUint32Le((uint)frame.Height);

//                // compressed length - if it's not compressed, this doesn't matter(?), if it is, we won't know it until we compress it later
//                // so write 0 for now
//                if (!frame.IsCompressed)
//                {
//                    int actualLength = frame.Width * frame.Height * NpkReader.s_formatToBytesPerPixel[frame.PixelFormat];
//                    WriteUint32Le((uint)actualLength);
//                }
//                else
//                {
//                    WriteUint32Le(0);
//                }
//            }

//            // Go back and fill in compressed length for frames with compressed pixel data

//            //_innerFileInfo[_numFilesWritten] = new InnerFileInfo() { NpkPath = path, Length = , Offset = };
//            _numFilesWritten++;
//        }

//        public void Finalize()
//        {
//            // Fill in location of each file in NPK header
//        }

//        private void WriteUint32Le(uint num)
//        {

//        }

//        // header
//        // num files
//        // location and path of each file
//        // files, with their metadata. Header, unknown 12 bytes, number frames, each frame's metadata (pixel format, link index/isCompressed, width, height, byte length, x, y, max width, max height), then pixel data for each frame

//        // Take list of files with path, number frames, unknown values, each frame's metadata, and callback to get pixel data

//        public void Dispose()
//        {
//            if (_closeStream)
//            {
//                _output.Dispose();
//            }
//        }
//    }
//}
