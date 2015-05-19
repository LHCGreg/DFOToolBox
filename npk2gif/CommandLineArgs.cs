using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NDesk.Options;

namespace DFO.npk2gif
{
    class CommandLineArgs
    {
        private bool m_showHelp = false;
        public bool ShowHelp { get { return m_showHelp; } private set { m_showHelp = value; } }

        private bool m_showVersion = false;
        public bool ShowVersion { get { return m_showVersion; } private set { m_showVersion = value; } }

        public string NpkPath { get; private set; }

        private string m_imgName;
        private string m_imgPath;

        public string ImgName { get { return m_imgName; } set { m_imgName = value; m_imgPath = null; } }
        public string ImgPath { get { return m_imgPath; } set { m_imgPath = value; m_imgName = null; } }

        private string ImgNameOrPath
        {
            get
            {
                return ImgName ?? ImgPath;
            }
            set
            {
                if (value == null)
                {
                    ImgName = null;
                    ImgPath = null;
                    return;
                }

                if (!value.Contains("/"))
                {
                    ImgName = value;
                }
                else
                {
                    ImgPath = value;
                }
            }
        }

        public bool UseAllFrames { get { return m_frameIndexes == null; } private set { m_frameIndexes = null; } }


        private List<int> m_frameIndexes;
        /// <summary>
        /// Null means all frames
        /// </summary>
        public List<int> FrameIndexes { get { return m_frameIndexes; } private set { m_frameIndexes = value; } }

        private int m_frameDelayInMs = 100;
        public int FrameDelayInMs
        {
            get { return m_frameDelayInMs; }
            private set
            {
                if (m_frameDelayInMs <= 10)
                {
                    throw new OptionException("Delay between frames must be at least 10 ms.", "delay");
                }
                m_frameDelayInMs = value;
            }
        }

        public string OutputPath { get; private set; }

        public OptionSet GetOptionSet()
        {
            OptionSet optionSet = new OptionSet()
            {
                { "?|h|help", "Show this message and exit.", argExistence => ShowHelp = (argExistence != null) },
                { "v|version", "Show version number and exit.", argExistence => ShowVersion = (argExistence != null) },
                { "npk=", "Path to the .NPK file to get the frames from.", arg => NpkPath = arg },
                { "img=", ".img file inside the .NPK file to get the frames from. Can be just the .img file name (foo.img), or the .img path with or without the leading sprite/ (sprite/character/atfighter.img)", arg => ImgNameOrPath = arg },
                { "frames=", "Frame numbers from the .img to use. If not specified, uses all frames from the .img. Frame numbers start at 0. May be either a range (ex: 0-5) or a comma-separate list of frame numbers (ex: 0, 4, 5, 6).", ParseFrameList },
                { "d|delay|frameDelay=", "Delay in milliseconds between frames. Note that the GIF format only supports multiples of 10 milliseconds.", ParseFrameDelay },
                { "o|output=", "File path to write the GIF to.", arg => OutputPath = arg }
            };

            return optionSet;
        }

        public CommandLineArgs(string[] args)
        {
            OptionSet optionSet = GetOptionSet();
            optionSet.Parse(args);

            if (ShowHelp)
            {
                DisplayHelp(Console.Out);
                Environment.Exit(0);
            }

            if (ShowVersion)
            {
                Console.WriteLine("{0} {1}", GetProgramNameWithoutExtension(), GetVersion());
                Environment.Exit(0);
            }

            // validate
            // npk, img, output must be set
            if (NpkPath == null)
            {
                throw new OptionException("-npk must be set to the path to the .NPK file.", "npk");
            }

            if (ImgNameOrPath == null)
            {
                throw new OptionException("-img must be set to the .img file inside the .NPK file to get the frames from.", "img");
            }

            if (OutputPath == null)
            {
                throw new OptionException("Output path must be set with -o or -output.", "o");
            }
        }

        public void DisplayHelp(TextWriter writer)
        {
            writer.WriteLine("Usage: {0} [OPTIONS]", GetProgramNameWithoutExtension());
            writer.WriteLine();
            writer.WriteLine("Parameters:");
            GetOptionSet().WriteOptionDescriptions(writer);
        }

        public void ParseFrameDelay(string arg)
        {
            if (arg == null)
            {
                return;
            }
            int delay;
            if (!int.TryParse(arg, out delay))
            {
                throw new OptionException(string.Format("Frame delay must be an integer number. Was {0}", arg), "delay");
            }

            FrameDelayInMs = delay;
        }

        public void ParseFrameList(string arg)
        {
            if (arg == null)
            {
                UseAllFrames = true;
            }
            arg = arg.Replace(" ", ""); // remove spaces
            // Is it a range?
            if (arg.Contains("-"))
            {
                string[] splitter = new string[] { "-" };
                string[] rangeNumbers = arg.Split(splitter, StringSplitOptions.None);
                if (rangeNumbers.Length != 2)
                {
                    throw new OptionException("-frames parameter badly formatted. If it is a range, it must be of the form X-Y.", "frames");
                }

                int firstNumber;
                int secondNumber;

                if (!int.TryParse(rangeNumbers[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out firstNumber))
                {
                    throw new OptionException(string.Format("First part of -frames range is not an integer: {0}", rangeNumbers[0]), "frames");
                }

                if (!int.TryParse(rangeNumbers[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out secondNumber))
                {
                    throw new OptionException(string.Format("Second part of -frames range is not an integer: {0}", rangeNumbers[1]), "frames");
                }

                if (firstNumber < 0)
                {
                    throw new OptionException(string.Format("First part of -frames range cannot be negative."), "frames");
                }

                if (secondNumber < 0)
                {
                    throw new OptionException(string.Format("Second part of -frames range cannot be negative."), "frames");
                }

                FrameIndexes = new List<int>();
                if (firstNumber < secondNumber)
                {
                    for (int frameIndex = firstNumber; frameIndex <= secondNumber; frameIndex++)
                    {
                        FrameIndexes.Add(frameIndex);
                    }
                }
                else
                {
                    for (int frameIndex = firstNumber; frameIndex >= secondNumber; frameIndex++)
                    {
                        FrameIndexes.Add(frameIndex);
                    }
                }
            }
            else
            {
                // comma-separated list of indexes
                string[] splitter = new string[] { "," };
                string[] numbers = arg.Split(splitter, StringSplitOptions.None);

                if (numbers.Length == 0)
                {
                    throw new OptionException("-frames must be a range (5-8) or a comma-separated list.", "frames");
                }

                List<int> frameIndexes = new List<int>();

                foreach (string numberString in numbers)
                {
                    int number;
                    if (!int.TryParse(numberString, out number))
                    {
                        throw new OptionException(string.Format("frame {0} is not an integer number.", numberString), "frames");
                    }
                    frameIndexes.Add(number);
                }

                FrameIndexes = frameIndexes;
            }
        }

        public static string GetProgramNameWithoutExtension()
        {
            string[] argsWithProgramName = System.Environment.GetCommandLineArgs();
            string programName;
            if (argsWithProgramName[0].Equals(string.Empty))
            {
                // "If the file name is not available, the first element is equal to String.Empty."
                // Doesn't say why that would happen, but ok...
                programName = (new AssemblyName(Assembly.GetExecutingAssembly().FullName).Name);
            }
            else
            {
                programName = Path.GetFileNameWithoutExtension(argsWithProgramName[0]);
            }

            return programName;
        }

        private static string GetVersion()
        {
            return Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }
    }
}
