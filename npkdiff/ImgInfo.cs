using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DFO.Common;

namespace DFO.npkdiff
{
    struct ImgInfo
    {
        public string NpkFileName { get; private set; }
        public NpkPath Path { get; private set; }
        public int FrameCount { get; private set; }

        public ImgInfo(string npkFileName, NpkPath path, int frameCount)
            : this()
        {
            NpkFileName = npkFileName;
            Path = path;
            FrameCount = frameCount;
        }
    }
}
