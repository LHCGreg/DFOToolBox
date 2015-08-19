using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DFO.Common;

namespace DFO.npkdiff
{
    class NpkDirDifferences
    {
        public IReadOnlyList<ImgInfo> ImgsInFirstButNotSecond { get; private set; }
        public IReadOnlyDictionary<NpkPath, FrameCountDifferenceInfo> ImgsWithFewerFramesInSecond { get; private set; }
        public IReadOnlyList<ImgInfo> ImgsInSecondButNotFirst { get; private set; }
        public IReadOnlyDictionary<NpkPath, FrameCountDifferenceInfo> ImgsWithMoreFramesInSecond { get; private set; }

        public NpkDirDifferences(IReadOnlyList<ImgInfo> imgsInFirstButNotSecond, IReadOnlyDictionary<NpkPath, FrameCountDifferenceInfo> imgsWithFewerFramesInSecond, IReadOnlyList<ImgInfo> imgsInSecondButNotFirst, IReadOnlyDictionary<NpkPath, FrameCountDifferenceInfo> imgsWithMoreFramesInSecond)
        {
            ImgsInFirstButNotSecond = imgsInFirstButNotSecond;
            ImgsWithFewerFramesInSecond = imgsWithFewerFramesInSecond;
            ImgsInSecondButNotFirst = imgsInSecondButNotFirst;
            ImgsWithMoreFramesInSecond = imgsWithMoreFramesInSecond;
        }
    }
}
