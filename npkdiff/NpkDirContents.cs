using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DFO.Common;
using DFO.Common.Images;

namespace DFO.npkdiff
{
    class NpkDirContents
    {
        private Dictionary<NpkPath, ImgInfo> _imgFrames;

        public NpkDirContents()
        {
            _imgFrames = new Dictionary<NpkPath, ImgInfo>();
        }

        public NpkDirContents(string npkFilePath, IReadOnlyDictionary<NpkPath, IReadOnlyList<FrameInfo>> frames)
        {
            _imgFrames = new Dictionary<NpkPath, ImgInfo>(frames.Count);
            foreach (NpkPath npkPath in frames.Keys)
            {
                _imgFrames[npkPath] = new ImgInfo(npkFilePath, npkPath, frames[npkPath].Count);
            }
        }

        public void Add(NpkDirContents other)
        {
            foreach (NpkPath npkPath in other._imgFrames.Keys)
            {
                if (!_imgFrames.ContainsKey(npkPath))
                {
                    _imgFrames[npkPath] = other._imgFrames[npkPath];
                }
            }
        }

        public NpkDirDifferences GetDifferences(NpkDirContents other)
        {
            if (other == null) throw new ArgumentNullException("other");

            List<ImgInfo> imgsInFirstButNotSecond = new List<ImgInfo>();
            List<ImgInfo> imgsInSecondButNotFirst = new List<ImgInfo>();
            Dictionary<NpkPath, FrameCountDifferenceInfo> imgsWithFewerFramcesInSecond = new Dictionary<NpkPath, FrameCountDifferenceInfo>();
            Dictionary<NpkPath, FrameCountDifferenceInfo> imgsWithMoreFramesInSecond = new Dictionary<NpkPath, FrameCountDifferenceInfo>();

            foreach (NpkPath npkPath in this._imgFrames.Keys)
            {
                if (!other._imgFrames.ContainsKey(npkPath))
                {
                    imgsInFirstButNotSecond.Add(this._imgFrames[npkPath]);
                }
                else
                {
                    int thisFrameCount = this._imgFrames[npkPath].FrameCount;
                    string thisNpkFilePath = this._imgFrames[npkPath].NpkFileName;
                    int otherFrameCount = other._imgFrames[npkPath].FrameCount;
                    string otherNpkFilePath = other._imgFrames[npkPath].NpkFileName;
                    if (thisFrameCount > otherFrameCount)
                    {
                        imgsWithFewerFramcesInSecond[npkPath] = new FrameCountDifferenceInfo(thisFrameCount, thisNpkFilePath, otherFrameCount, otherNpkFilePath);
                    }
                    else if (thisFrameCount < otherFrameCount)
                    {
                        imgsWithMoreFramesInSecond[npkPath] = new FrameCountDifferenceInfo(thisFrameCount, thisNpkFilePath, otherFrameCount, otherNpkFilePath);
                    }
                }
            }

            foreach (NpkPath npkPath in other._imgFrames.Keys)
            {
                if (!this._imgFrames.ContainsKey(npkPath))
                {
                    imgsInSecondButNotFirst.Add(other._imgFrames[npkPath]);
                }
            }

            return new NpkDirDifferences(imgsInFirstButNotSecond, imgsWithFewerFramcesInSecond, imgsInSecondButNotFirst, imgsWithMoreFramesInSecond);
        }
    }
}
