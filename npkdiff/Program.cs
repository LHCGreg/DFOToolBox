using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DFO.Common;
using DFO.Npk;

namespace DFO.npkdiff
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                CommandLineArgs cmdline = new CommandLineArgs(args);

                // read *.NPK in NpkDir1
                // read *.NPK in NpkDir2
                // Compare

                NpkDirContents dir1Contents = GetNpkDirContents(cmdline.NpkDir1);
                NpkDirContents dir2Contents = GetNpkDirContents(cmdline.NpkDir2);
                NpkDirDifferences differences = dir1Contents.GetDifferences(dir2Contents);
                DisplayDifferences(differences);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Unexpected error: {0}", ex.Message);
            }
        }

        static NpkDirContents GetNpkDirContents(string npkDir)
        {
            NpkDirContents allContents = new NpkDirContents();
            foreach (string npkFilePath in Directory.GetFiles(npkDir, "*.NPK", SearchOption.TopDirectoryOnly))
            {
                NpkDirContents npkContents = GetNpkContents(npkFilePath);
                allContents.Add(npkContents);
            }
            return allContents;
        }

        static NpkDirContents GetNpkContents(string npkFilePath)
        {
            using (NpkReader npk = new NpkReader(npkFilePath))
            {
                return new NpkDirContents(Path.GetFileName(npkFilePath), npk.Frames);
            }
        }

        static void DisplayDifferences(NpkDirDifferences differences)
        {
            foreach (ImgInfo img in differences.ImgsInSecondButNotFirst.OrderBy(x => x.Path.Path))
            {
                Console.WriteLine("+{0} ({1}): {2}", img.Path, img.FrameCount, img.NpkFileName);
            }
            foreach (NpkPath imgPath in differences.ImgsWithMoreFramesInSecond.Keys.OrderBy(x => x.Path))
            {
                string npkFileNameDisplay;
                string npkFileName1 = differences.ImgsWithMoreFramesInSecond[imgPath].NpkFileName1;
                string npkFileName2 = differences.ImgsWithMoreFramesInSecond[imgPath].NpkFileName2;
                if (npkFileName1.Equals(npkFileName2, StringComparison.OrdinalIgnoreCase))
                {
                    npkFileNameDisplay = npkFileName1;
                }
                else
                {
                    npkFileNameDisplay = string.Format("{0}->{1}", npkFileName1, npkFileName2);
                }
                Console.WriteLine(">{0} {1}->{2} {3}", imgPath, differences.ImgsWithMoreFramesInSecond[imgPath].FrameCount1, differences.ImgsWithMoreFramesInSecond[imgPath].FrameCount2, npkFileNameDisplay);
            }
            foreach (NpkPath imgPath in differences.ImgsWithFewerFramesInSecond.Keys.OrderBy(x => x.Path))
            {
                string npkFileNameDisplay;
                string npkFileName1 = differences.ImgsWithFewerFramesInSecond[imgPath].NpkFileName1;
                string npkFileName2 = differences.ImgsWithFewerFramesInSecond[imgPath].NpkFileName2;
                if (npkFileName1.Equals(npkFileName2, StringComparison.OrdinalIgnoreCase))
                {
                    npkFileNameDisplay = npkFileName1;
                }
                else
                {
                    npkFileNameDisplay = string.Format("{0}->{1}", npkFileName1, npkFileName2);
                }
                Console.WriteLine("<{0} {1}->{2}", imgPath, differences.ImgsWithFewerFramesInSecond[imgPath].FrameCount1, differences.ImgsWithFewerFramesInSecond[imgPath].FrameCount2, npkFileNameDisplay);
            }
            foreach (ImgInfo img in differences.ImgsInFirstButNotSecond.OrderBy(x => x.Path.Path))
            {
                Console.WriteLine("-{0} ({1}): {2}", img, img.FrameCount, img.NpkFileName);
            }
        }
    }
}
