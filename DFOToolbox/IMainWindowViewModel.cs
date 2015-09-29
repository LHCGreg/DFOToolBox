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
        string Status { get; set; }
        string OpenNPKPath { get; set; }

        void Open(string npkPath);
        bool CanOpen { get; set; }

        QuickSaveResults QuickSaveAsPng(string imgPath, int frameIndex);
        bool CanQuickSaveAsPng { get; set; }

        /// <exception cref="DFOToolbox.DFOToolboxException">Something went wrong while editing. Message is suitable for UI display.</exception>
        /// <exception cref="System.Exception">Other errors resulting from incorrect usage of this function, such as passing null arguments or trying to edit a frame while no file is open.</exception>
        void EditFrame(string imgPath, int frameIndex, string pngFilePath);
        bool CanEditFrame { get; set; }
    }
}
