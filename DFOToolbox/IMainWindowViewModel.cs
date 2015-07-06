using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using DFOToolbox.Models;
using Microsoft.Practices.Prism.Commands;

namespace DFOToolbox
{
    public interface IMainWindowViewModel
    {
        InnerNpkFileList InnerFileList { get; set; }
        FrameList FrameList { get; set; }
        ImageSource CurrentFrameImage { get; set; }

        void Open(string npkPath);
        bool CanOpen { get; set; }

        void QuickSaveAsPng();
        bool CanQuickSaveAsPng { get; set; }
    }
}
