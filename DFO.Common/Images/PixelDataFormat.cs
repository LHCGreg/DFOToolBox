using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DFO.Common.Images
{
    // These are in little-endian order, so 1555 ARGB should have the bytes on disk and in memory as BGRA
    public enum PixelDataFormat
    {
        OneFiveFiveFive = 14, // 0x0E
        FourFourFourFour = 15, // 0x0F
        EightEightEightEight = 16, // 0x10
        Link = 17 // 0x11
    }
}
