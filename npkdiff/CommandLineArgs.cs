using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NDesk.Options;

namespace DFO.npkdiff
{
    class CommandLineArgs
    {
        private bool m_showHelp = false;
        public bool ShowHelp { get { return m_showHelp; } private set { m_showHelp = value; } }

        private bool m_showVersion = false;
        public bool ShowVersion { get { return m_showVersion; } private set { m_showVersion = value; } }
        
        public string NpkDir1 { get; private set; }
        public string NpkDir2 { get; private set; }

        public CommandLineArgs(string[] args)
        {
            OptionSet optionSet = GetOptionSet();

            try
            {
                optionSet.Parse(args);
            }
            catch (OptionException ex)
            {
                Console.Error.WriteLine(ex.Message);
                Environment.Exit(1);
            }

            if (ShowHelp)
            {
                DisplayHelp(Console.Error);
                Environment.Exit(0);
            }

            if (ShowVersion)
            {
                Console.WriteLine("{0} {1}", GetProgramNameWithoutExtension(), GetVersion());
                Environment.Exit(0);
            }

            if (NpkDir1 == null || NpkDir2 == null)
            {
                DisplayHelp(Console.Error);
                Environment.Exit(1);
            }
        }

        public OptionSet GetOptionSet()
        {
            OptionSet optionSet = new OptionSet()
            {
                { "?|h|help", "Show this message and exit.", argExistence => ShowHelp = (argExistence != null) },
                { "v|version", "Show version number and exit.", argExistence => ShowVersion = (argExistence != null) },
                { "<>", AddDir }
            };

            return optionSet;
        }

        private void AddDir(string arg)
        {
            if (NpkDir1 == null)
            {
                NpkDir1 = arg;
            }
            else if (NpkDir2 == null)
            {
                NpkDir2 = arg;
            }
            else
            {
                throw new OptionException(string.Format("More than 2 directories specified. Run {0} -h for help.", GetProgramNameWithoutExtension()), "<>");
            }
        }

        public void DisplayHelp(TextWriter writer)
        {
            writer.WriteLine("Usage: {0} IMAGEPACKDIRECTORY1 IMAGEPACKDIRECTORY2", GetProgramNameWithoutExtension());
            writer.WriteLine();
            writer.WriteLine("Parameters:");
            GetOptionSet().WriteOptionDescriptions(writer);
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
            return typeof(CommandLineArgs).Assembly.GetName().Version.ToString();
        }
    }
}
