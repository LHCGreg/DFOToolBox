using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DFO.Common;

namespace DFO.npkdiff
{
    struct FrameCountDifferenceInfo
    {
        public int FrameCount1 { get; private set; }
        public string NpkFileName1 { get; private set; }
        public int FrameCount2 { get; private set; }
        public string NpkFileName2 { get; private set; }

        public FrameCountDifferenceInfo(int frameCount1, string npkFileName1, int frameCount2, string npkFileName2)
            : this()
        {
            FrameCount1 = frameCount1;
            FrameCount2 = frameCount2;
            NpkFileName1 = npkFileName1;
            NpkFileName2 = npkFileName2;
        }
    }
}
