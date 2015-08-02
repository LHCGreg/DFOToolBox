using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DFOToolbox
{
    public class AboutWindowViewModel
    {
        public string AppName { get; private set; }
        public string Version { get; private set; }
        public string Url { get; private set; }

        public AboutWindowViewModel()
        {
            AppName = "DFO Toolbox";
            Url = "https://github.com/LHCGreg/DFOToolBox";
            Version = typeof(AboutWindowViewModel).Assembly.GetName().Version.ToString();
        }
    }
}
