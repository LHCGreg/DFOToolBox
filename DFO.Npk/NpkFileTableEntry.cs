using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DFO.Common;

namespace DFO.Npk
{
    internal struct NpkFileTableEntry
    {
        private NpkPath _name;
        private NpkByteRange _location;

        public NpkPath Name { get { return _name; } }
        public NpkByteRange Location { get { return _location; } }

        public NpkFileTableEntry(NpkPath name, NpkByteRange location)
        {
            _name = name;
            _location = location;
        }
    }
}
