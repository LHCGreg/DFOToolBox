using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DFO.Common;
using DFO.Common.Images;
using DFO.Gif;
using DFO.Npk;
using NDesk.Options;

namespace DFO.npk2gif
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                CommandLineArgs cmdLine = new CommandLineArgs(args);

                using (NpkReader npk = LoadNpk(cmdLine.NpkPath))
                {
                    NpkPath imgPath = GetImgPath(cmdLine, npk);

                    RawAnimation animationData = new RawAnimation();
                    animationData.Loop = true;

                    List<ConstAnimationFrame> frameInfo = GetFrameInfo(cmdLine, npk, imgPath);
                    animationData.Frames = frameInfo;

                    CreateOutputDir(cmdLine.OutputPath);

                    using (FileStream gifOutputStream = OpenOutput(cmdLine.OutputPath))
                    using (GifMaker giffer = new GifMaker(npk, disposeImageSource: false))
                    {
                        try
                        {
                            giffer.Create(animationData.AsConst(), gifOutputStream);
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine("Error creating GIF: {0}", ex.Message);
                            Console.Error.WriteLine(ex.StackTrace);
                            giffer.Dispose();
                            gifOutputStream.Dispose();
                            npk.Dispose();
                            Environment.Exit(1);
                        }
                    }
                }

                Console.WriteLine("GIF saved to {0}", cmdLine.OutputPath);
            }
            catch (OptionException ex)
            {
                Console.Error.WriteLine(ex.Message);
                Console.Error.WriteLine("Run with -h for help");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Unexpected error: {0}", ex.Message);
                Console.Error.WriteLine(ex.StackTrace);
            }
        }

        private static NpkReader LoadNpk(string path)
        {
            NpkReader npk = null;
            try
            {
                npk = new NpkReader(path);
            }
            catch (FileNotFoundException)
            {
                Console.Error.WriteLine("NPK file {0} not found.", path);
                Environment.Exit(1);
            }
            catch (UnauthorizedAccessException)
            {
                Console.Error.WriteLine("You do not have permission to read NPK file {0}", path);
                Environment.Exit(1);
            }
            catch (NpkException ex)
            {
                Console.Error.WriteLine("There was an error while loading the NPK file. The file format may have changed. Here is some information that may help debug the issue: {0}\n\n{1}", ex.Message, ex.StackTrace);
                Environment.Exit(1);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("There was an error while loading the NPK file: {0}", ex.Message);
                Environment.Exit(1);
            }

            return npk;
        }

        private static NpkPath GetImgPath(CommandLineArgs cmdLine, NpkReader npk)
        {
            if (cmdLine.ImgPath != null)
            {
                NpkPath imgPath = new NpkPath(cmdLine.ImgPath);
                IList<NpkPath> imgPathComponents = imgPath.GetPathComponents();
                if (imgPathComponents.Count >= 1 && imgPathComponents[0].Path.Equals("sprite", StringComparison.OrdinalIgnoreCase))
                {
                    // strip sprite/ prefix if present
                    imgPath = imgPath.StripPrefix();
                }

                if (!npk.Images.ContainsKey(imgPath))
                {
                    Console.Error.WriteLine("There is no img file with path {0} in NPK file {1}", cmdLine.ImgPath, cmdLine.NpkPath);
                    Environment.Exit(1);
                }

                return imgPath;
            }
            else
            {
                List<NpkPath> matchingPaths = new List<NpkPath>();
                
                // Only the .img name was given. Look for it.
                foreach (NpkPath path in npk.Images.Keys)
                {
                    if (path.GetFileName().Path.Equals(cmdLine.ImgName, StringComparison.OrdinalIgnoreCase))
                    {
                        matchingPaths.Add(path);
                    }
                }

                if (matchingPaths.Count == 1)
                {
                    return matchingPaths[0];
                }
                else if (matchingPaths.Count == 0)
                {
                    Console.Error.WriteLine("There is no img file called {0} in NPK file {1}", cmdLine.ImgName, cmdLine.NpkPath);
                    Environment.Exit(1);
                    return null; // not reached
                }
                else
                {
                    Console.Error.WriteLine("There are multiple img files matching the name {0} in NPK file {1}: {2}", cmdLine.ImgName, cmdLine.NpkPath, string.Join(", ", matchingPaths));
                    Environment.Exit(1);
                    return null; // not reached
                }
            }
        }

        private static List<ConstAnimationFrame> GetFrameInfo(CommandLineArgs cmdLine, NpkReader npk, NpkPath imgPath)
        {
            List<ConstAnimationFrame> frameInfo = new List<ConstAnimationFrame>();
            List<FrameInfo> frames = npk.Images[imgPath].ToList();

            if (cmdLine.UseAllFrames)
            {
                for (int frameIndex = 0; frameIndex < frames.Count; frameIndex++)
                {
                    frameInfo.Add(new AnimationFrame() { DelayInMs = (uint)cmdLine.FrameDelayInMs, Image = new ImageIdentifier(imgPath, (uint)frameIndex) }.AsConst());
                }
            }
            else
            {
                foreach (int frameIndex in cmdLine.FrameIndexes)
                {
                    if (frameIndex >= frames.Count)
                    {
                        Console.Error.WriteLine("{0} in {1} has {2} frames in it, so frame index {3} is not valid.", imgPath, cmdLine.NpkPath, frames.Count, frameIndex);
                        Environment.Exit(1);
                    }
                    frameInfo.Add(new AnimationFrame() { DelayInMs = (uint)cmdLine.FrameDelayInMs, Image = new ImageIdentifier(imgPath, (uint)frameIndex) }.AsConst());
                }
            }

            return frameInfo;
        }

        private static void CreateOutputDir(string outputPath)
        {
            try
            {
                string directory = Path.GetDirectoryName(outputPath);
                if (string.IsNullOrWhiteSpace(directory))
                {
                    // relative path referring to a file in the current directory, like "output.gif"
                    return;
                }
                Directory.CreateDirectory(directory);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error creating output directory for {0}: {1}", outputPath, ex.Message);
                Environment.Exit(1);
            }
        }

        private static FileStream OpenOutput(string outputPath)
        {
            try
            {
                return new FileStream(outputPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error opening {0} for output: {1}", outputPath, ex.Message);
                Environment.Exit(1);
                return null; // not reached
            }
        }
    }
}
