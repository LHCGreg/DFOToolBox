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

        DelegateCommand<string> OpenCommand { get; }
        bool OpenCommandCanExecute { get; set; }

        DelegateCommand QuickSaveAsPngCommand { get; }
        bool QuickSaveAsPngCommandCanExecute { get; set; }
    }
}
