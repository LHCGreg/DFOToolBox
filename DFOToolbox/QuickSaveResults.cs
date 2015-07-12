using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DFOToolbox
{
    public class QuickSaveResults
    {
        public string OutputPath { get; set; }
        public string OutputFolder { get; set; }
        public DFOToolboxException Error { get; set; }
    }
}
