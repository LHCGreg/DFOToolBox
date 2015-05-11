using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DFO.Utilities
{
    /// <summary>
    /// Static class containing utility methods.
    /// </summary>
    public static class Utils
    {
        /// <summary>
        /// Checks to make sure <paramref name="path"/> does not contain any characters in
        /// System.IO.Path.GetInvalidPathChars(). A null path is considered invalid.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static bool PathIsValid(string path)
        {
            if (path == null)
            {
                return false;
            }
            char[] invalidChars = Path.GetInvalidPathChars();
            if (path.IndexOfAny(invalidChars) != -1)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Converts a little-endian 32-bit signed integer to a native 32-bit signed integer.
        /// </summary>
        /// <param name="buffer">A buffer containing the 4 bytes to be converted.</param>
        /// <param name="offset">The index in the buffer that the 4 bytes starts at.</param>
        /// <returns>The native signed integer representation of the little-endian signed integer.</returns>
        /// <exception cref="System.NullReferenceException"><paramref name="buffer"/> is null.</exception>
        /// <exception cref="System.ArgumentNullException"><paramref name="buffer"/> is null.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="offset"/> is less than zero
        /// or greater than the length of <paramref name="buffer"/> minus 4.</exception>
        /// <exception cref="System.IndexOutOfRangeException"><paramref name="offset"/> is less than zero
        /// or greater than the length of <paramref name="buffer"/> minus 4.</exception>
        public static int LeToNativeInt32(byte[] buffer, int offset)
        {
            if (BitConverter.IsLittleEndian)
            {
                return BitConverter.ToInt32(buffer, offset);
            }
            else
            {
                int ret = 0;
                for (int i = 3; i >= 0; i--)
                {
                    ret = unchecked((ret << 8) | buffer[offset + i]);
                }
                return ret;
            }
        }

        /// <summary>
        /// Converts a little-endian 32-bit unsigned integer to a native 32-bit unsigned integer.
        /// </summary>
        /// <param name="buffer">A buffer containing the 4 bytes to be converted.</param>
        /// <param name="offset">The index in the buffer that the 4 bytes starts at.</param>
        /// <returns>The native unsigned integer representation of the little-endian unsigned integer.</returns>
        /// <exception cref="System.NullReferenceException"><paramref name="buffer"/> is null.</exception>
        /// <exception cref="System.ArgumentNullException"><paramref name="buffer"/> is null.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="offset"/> is less than zero
        /// or greater than the length of <paramref name="buffer"/> minus 4.</exception>
        /// <exception cref="System.IndexOutOfRangeException"><paramref name="offset"/> is less than zero
        /// or greater than the length of <paramref name="buffer"/> minus 4.</exception>
        public static uint LeToNativeUInt32(byte[] buffer, int offset)
        {
            if (BitConverter.IsLittleEndian)
            {
                return BitConverter.ToUInt32(buffer, offset);
            }
            else
            {
                uint ret = 0;
                for (int i = 3; i >= 0; i--)
                {
                    ret = unchecked((ret << 8) | buffer[offset + i]);
                }
                return ret;
            }
        }

        /// <summary>
        /// Converts a little-endian 16-bit signed integer to a native 16-bit signed integer.
        /// </summary>
        /// <param name="buffer">A buffer containing the 2 bytes to be converted.</param>
        /// <param name="offset">The index in the buffer that the 2 bytes starts at.</param>
        /// <returns>The native signed integer representation of the little-endian signed integer.</returns>
        /// <exception cref="System.NullReferenceException"><paramref name="buffer"/> is null.</exception>
        /// <exception cref="System.ArgumentNullException"><paramref name="buffer"/> is null.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="offset"/> is less than zero
        /// or greater than the length of <paramref name="buffer"/> minus 2.</exception>
        /// <exception cref="System.IndexOutOfRangeException"><paramref name="offset"/> is less than zero
        /// or greater than the length of <paramref name="buffer"/> minus 2.</exception>
        public static short LeToNativeInt16(byte[] buffer, int offset)
        {
            if (BitConverter.IsLittleEndian)
            {
                return BitConverter.ToInt16(buffer, offset);
            }
            else
            {
                short ret = 0;
                for (int i = 1; i >= 0; i--)
                {
                    ret = unchecked((short)((ret << 8) | buffer[offset + i]));
                }
                return ret;
            }
        }

        /// <summary>
        /// Converts a little-endian 16-bit unsigned integer to a native 16-bit unsigned integer.
        /// </summary>
        /// <param name="buffer">A buffer containing the 2 bytes to be converted.</param>
        /// <param name="offset">The index in the buffer that the 2 bytes starts at.</param>
        /// <returns>The native unsigned integer representation of the little-endian unsigned integer.</returns>
        /// <exception cref="System.NullReferenceException"><paramref name="buffer"/> is null.</exception>
        /// <exception cref="System.ArgumentNullException"><paramref name="buffer"/> is null.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="offset"/> is less than zero
        /// or greater than the length of <paramref name="buffer"/> minus 2.</exception>
        /// <exception cref="System.IndexOutOfRangeException"><paramref name="offset"/> is less than zero
        /// or greater than the length of <paramref name="buffer"/> minus 2.</exception>
        public static ushort LeToNativeUInt16(byte[] buffer, int offset)
        {
            if (BitConverter.IsLittleEndian)
            {
                return BitConverter.ToUInt16(buffer, offset);
            }
            else
            {
                ushort ret = 0;
                for (int i = 1; i >= 0; i--)
                {
                    ret = unchecked((ushort)((ret << 8) | buffer[offset + i]));
                }
                return ret;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <typeparam name="TConstValue"></typeparam>
        /// <param name="dictToWrap"></param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="dictToWrap"/> is null.</exception>
        public static DeepReadOnlyDictionary<TKey, TValue, TConstValue>
            CreateConstDictionary<TKey, TValue, TConstValue>(IDictionary<TKey, TValue> dictToWrap)
            where TValue : IConstable<TConstValue>
        {
            return new DeepReadOnlyDictionary<TKey, TValue, TConstValue>(dictToWrap,
                (TValue value) => value.AsConst());
        }
    }
}
