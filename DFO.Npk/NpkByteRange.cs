using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DFO.Npk
{
    /// <summary>
    /// Represents a region of an .npk file.
    /// </summary>
    internal class NpkByteRange
    {
        public long FileOffset { get; private set; }
        public uint Size { get; private set; }

        public NpkByteRange(long fileOffset, uint size)
        {
            FileOffset = fileOffset;
            Size = size;
        }
    }
}
