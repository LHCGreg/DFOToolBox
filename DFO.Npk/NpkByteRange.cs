using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DFO.Npk
{
    /// <summary>
    /// Represents a region of an .npk file.
    /// </summary>
    internal struct NpkByteRange : IEquatable<NpkByteRange>
    {
        public int FileOffset { get; private set; }
        public int Size { get; private set; }

        public NpkByteRange(int fileOffset, int size)
            : this()
        {
            FileOffset = fileOffset;
            Size = size;
        }

        public override bool Equals(object obj)
        {
            if (obj is NpkByteRange)
                return Equals((NpkByteRange)obj);
            else
                return false;
        }

        public bool Equals(NpkByteRange other)
        {
            return this.FileOffset == other.FileOffset && this.Size == other.Size;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 23;
                hash = hash * 31 + FileOffset;
                hash = hash * 31 + Size;
                return hash;
            }
        }
    }
}
