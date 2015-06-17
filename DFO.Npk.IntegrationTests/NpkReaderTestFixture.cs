using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DFO.Common;
using DFO.Common.Images;
using NUnit.Framework;

namespace DFO.Npk.IntegrationTests
{
    [TestFixture]
    public class NpkReaderTestFixture
    {
        [Test]
        public void ReadAllImages()
        {
            Config config = new Config();
            List<string> extraErrors = new List<string>();
            foreach (string path in Directory.GetFiles(config.ImageNpkDir, "*.NPK"))
            {
                using (NpkReader npk = new NpkReader(path, extraErrorHandler: (sender, args) => { extraErrors.Add(args.Message); Console.WriteLine(args.Message); }))
                {
                    npk.PreLoadAllSpriteFrameMetadata();

                    foreach (NpkPath imgPath in npk.Frames.Keys)
                    {
                        IReadOnlyList<FrameInfo> imgFrames = npk.Frames[imgPath];
                        for (int frameIndex = 0; frameIndex < imgFrames.Count; frameIndex++)
                        {
                            npk.GetImage(imgPath, frameIndex);
                        }
                    }

                    if (extraErrors.Count > 0)
                    {
                        throw new Exception("Errors detected. Check console output for details.");
                    }
                }
            }
        }
    }
}
