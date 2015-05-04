using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;

namespace DFO.Utilities
{
    /// <summary>
    /// Static class containing some utility extension methods.
    /// </summary>
    internal static class UtilityExtensions
    {
        /// <summary>
        /// Rotates <paramref name="number"/> left by <paramref name="numBitstoRotate"/> bits. If
        /// <paramref name="numBitsToRotate"/> is negative, the results are undefined.
        /// </summary>
        /// <param name="number">The number to rotate.</param>
        /// <param name="numBitsToRotate">The number of bits to rotate left.</param>
        /// <returns><paramref name="number"/> rotated left by <paramref name="numBitsToRotate"/> bits.</returns>
        public static uint Rotl(this uint number, int numBitsToRotate)
        {
            return (number << numBitsToRotate) | (number >> (32 - numBitsToRotate));
        }

        /// <summary>
        /// Rotates <paramref name="number"/> right by <paramref name="numBitstoRotate"/> bits. If
        /// <paramref name="numBitsToRotate"/> is negative, the results are undefined.
        /// </summary>
        /// <param name="number">The number to rotate.</param>
        /// <param name="numBitsToRotate">The number of bits to rotate right.</param>
        /// <returns><paramref name="number"/> rotated right by <paramref name="numBitsToRotate"/> bits.</returns>
        public static uint Rotr(this uint number, int numBitsToRotate)
        {
            return (number >> numBitsToRotate) | (number << (32 - numBitsToRotate));
        }

        /// <summary>
        /// Reads <paramref name="numBytes"/> into <paramref name="buffer"/> from <paramref name="stream"/>. If
        /// <paramref name="numBytes"/> bytes could not be read, a <c>System.IO.EndOfStreamException</c> is thrown.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="buffer">The buffer to store the bytes read in. It must be at least
        /// <paramref name="numBytes"/> bytes in size.</param>
        /// <param name="numBytes">The number of bytes to read.</param>
        /// <exception cref="System.IO.EndOfStreamException">Fewer than <paramref name="numBytes"/> bytes were read.</exception>
        /// <exception cref="System.NullReferenceException"><paramref name="stream"/> or <paramref name="buffer"/> is null.</exception>
        /// <exception cref="System.ArgumentNullException"><paramref name="stream"/> or <paramref name="buffer"/> is null.</exception>
        /// <exception cref="System.ArgumentException">The buffer is not large enough</exception>
        /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="numBytes"/> is negative</exception>
        /// <exception cref="System.IOException">An I/O error occurred.</exception>
        /// <exception cref="System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="System.ObjectDisposedException">The stream is disposed.</exception>
        public static void ReadOrDie(this Stream stream, byte[] buffer, int numBytes)
        {
            int totalBytesRead = 0;
            while (totalBytesRead < numBytes)
            {
                int bytesRead = stream.Read(buffer, totalBytesRead, numBytes - totalBytesRead);
                if (bytesRead == 0)
                {
                    throw new EndOfStreamException("Unexpected end of file");
                }
                totalBytesRead += bytesRead;
            }
        }

        /// <summary>
        /// Reads a 32-bit signed little-endian integer from <paramref name="stream"/>, using
        /// <paramref name="buffer"/> for temporary storage. <paramref name="buffer"/> must be at least 4 bytes.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="buffer">The buffer to use for temporary storage.</param>
        /// <returns>A 32-bit signed integer from the stream.</returns>
        /// <exception cref="System.IO.EndOfStreamException">Fewer than 4 bytes were read.</exception>
        /// <exception cref="System.NullReferenceException"><paramref name="stream"/> or <paramref name="buffer"/> is null.</exception>
        /// <exception cref="System.ArgumentNullException"><paramref name="stream"/> or <paramref name="buffer"/> is null.</exception>
        /// <exception cref="System.ArgumentException">The buffer is not large enough</exception>
        /// <exception cref="System.IOException">An I/O error occurred.</exception>
        /// <exception cref="System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="System.ObjectDisposedException">The stream is disposed.</exception>
        public static int GetInt32Le(this Stream stream, byte[] buffer)
        {
            // Buffer gets passed in so we're not making a ton of unnecessary 4-byte allocations
            stream.ReadOrDie(buffer, 4);
            return Utils.LeToNativeInt32(buffer, 0);
        }

        /// <summary>
        /// Reads a 32-bit unsigned little-endian integer from <paramref name="stream"/>, using
        /// <paramref name="buffer"/> for temporary storage. <paramref name="buffer"/> must be at least 4 bytes.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="buffer">The buffer to use for temporary storage.</param>
        /// <returns>A 32-bit unsigned integer from the stream.</returns>
        /// <exception cref="System.IO.EndOfStreamException">Fewer than 4 bytes were read.</exception>
        /// <exception cref="System.NullReferenceException"><paramref name="stream"/> or <paramref name="buffer"/> is null.</exception>
        /// <exception cref="System.ArgumentNullException"><paramref name="stream"/> or <paramref name="buffer"/> is null.</exception>
        /// <exception cref="System.ArgumentException">The buffer is not large enough</exception>
        /// <exception cref="System.IOException">An I/O error occurred.</exception>
        /// <exception cref="System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="System.ObjectDisposedException">The stream is disposed.</exception>
        public static uint GetUnsigned32Le(this Stream stream, byte[] buffer)
        {
            stream.ReadOrDie(buffer, 4);
            return Utils.LeToNativeUInt32(buffer, 0);
        }

        /// <summary>
        /// Reads a 16-bit signed little-endian integer from <paramref name="stream"/>, using
        /// <paramref name="buffer"/> for temporary storage. <paramref name="buffer"/> must be at least 2 bytes.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="buffer">The buffer to use for temporary storage.</param>
        /// <returns>A 16-bit signed integer from the stream.</returns>
        /// <exception cref="System.IO.EndOfStreamException">Fewer than 2 bytes were read.</exception>
        /// <exception cref="System.NullReferenceException"><paramref name="stream"/> or <paramref name="buffer"/> is null.</exception>
        /// <exception cref="System.ArgumentNullException"><paramref name="stream"/> or <paramref name="buffer"/> is null.</exception>
        /// <exception cref="System.ArgumentException">The buffer is not large enough</exception>
        /// <exception cref="System.IOException">An I/O error occurred.</exception>
        /// <exception cref="System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="System.ObjectDisposedException">The stream is disposed.</exception>
        public static short GetInt16Le(this Stream stream, byte[] buffer)
        {
            stream.ReadOrDie(buffer, 2);
            return Utils.LeToNativeInt16(buffer, 0);
        }

        /// <summary>
        /// Reads a 16-bit unsigned little-endian integer from <paramref name="stream"/>, using
        /// <paramref name="buffer"/> for temporary storage. <paramref name="buffer"/> must be at least 2 bytes.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="buffer">The buffer to use for temporary storage.</param>
        /// <returns>A 16-bit unsigned integer from the stream.</returns>
        /// <exception cref="System.IO.EndOfStreamException">Fewer than 2 bytes were read.</exception>
        /// <exception cref="System.NullReferenceException"><paramref name="stream"/> or <paramref name="buffer"/> is null.</exception>
        /// <exception cref="System.ArgumentNullException"><paramref name="stream"/> or <paramref name="buffer"/> is null.</exception>
        /// <exception cref="System.ArgumentException">The buffer is not large enough</exception>
        /// <exception cref="System.IOException">An I/O error occurred.</exception>
        /// <exception cref="System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="System.ObjectDisposedException">The stream is disposed.</exception>
        public static ushort GetUnsigned16Le(this Stream stream, byte[] buffer)
        {
            stream.ReadOrDie(buffer, 2);
            return Utils.LeToNativeUInt16(buffer, 0);
        }

        /// <summary>
        /// Copies the input stream to the output stream.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="output"></param>
        /// <exception cref="System.IO.IOException"></exception>
        /// <exception cref="System.NotSupportedException"></exception>
        /// <exception cref="System.ObjectDisposedException"></exception>
        /// <exception cref="System.ArgumentNullException"></exception>
        public static void CopyTo(this Stream input, Stream output)
        {
            output.ThrowIfNull("output");

            const int bufferSize = 8192;
            byte[] buffer = new byte[bufferSize];
            int read;
            while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                output.Write(buffer, 0, read);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        /// <exception cref="System.IO.IOException"></exception>
        /// <exception cref="System.NotSupportedException"></exception>
        /// <exception cref="System.ObjectDisposedException"></exception>
        public static byte[] ReadFully(this Stream input)
        {
            using (MemoryStream tempStream = new MemoryStream())
            {
                CopyTo(input, tempStream);
                return tempStream.ToArray();
            }
        }

        /// <summary>
        /// Returns this number as an <c>int</c> if it is an integer.
        /// </summary>
        /// <param name="dec">The <c>decimal</c> to return as an integer.</param>
        /// <returns><paramref name="dec"/> as an <c>int</c> if it is an integer or null if it is not an integer.</returns>
        public static int? AsInt32(this decimal dec)
        {
            try
            {
                int integerValue = decimal.ToInt32(dec);
                if ((decimal)integerValue != dec)
                {
                    return null;
                }
                else
                {
                    return integerValue;
                }
            }
            catch (OverflowException)
            {
                return null;
            }
        }

        /// <summary>
        /// Returns this number as a <c>uint</c> if it is an unsigned integer.
        /// </summary>
        /// <param name="dec">The <c>deciman</c> to return as an unsigned integer.</param>
        /// <returns><paramref name="dec"/> as a <c>uint</c> if it is an unsigned integer or null if it is
        /// not an integer or is negative.</returns>
        public static uint? AsUInt32(this decimal dec)
        {
            try
            {
                uint uintValue = decimal.ToUInt32(dec);
                if ((decimal)uintValue != dec)
                {
                    return null;
                }
                else
                {
                    return uintValue;
                }
            }
            catch (OverflowException)
            {
                return null;
            }
        }

        public static ulong? AsUInt64(this decimal dec)
        {
            try
            {
                ulong ulongValue = decimal.ToUInt64(dec);
                if ((decimal)ulongValue != dec)
                {
                    return null;
                }
                else
                {
                    return ulongValue;
                }
            }
            catch (OverflowException)
            {
                return null;
            }
        }

        public static void ThrowIfNull(this object obj, string argName)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(argName);
            }
        }

        public static string F(this string str, params object[] args)
        {
            return string.Format(str, args);
        }
    }
}