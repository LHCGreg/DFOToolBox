using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using DFOToolbox.Models;
using Microsoft.Practices.Prism.Commands;

namespace DFOToolbox
{
    /// <summary>
    /// Stub viewmodel for the main window used to provide data to be shown in the designer view in Visual Studio.
    /// </summary>
    public class DesignerMainWindowViewModel : IMainWindowViewModel
    {
        public InnerNpkFileList InnerFileList { get; set; }
        public FrameList FrameList { get; set; }
        public ImageSource CurrentFrameImage { get; set; }
        public string Status { get; set; }
        public string OpenNPKPath { get; set; }
        public bool CanOpen { get; set; }
        public bool CanQuickSaveAsPng { get; set; }

        public DesignerMainWindowViewModel()
        {
            InnerFileList = new InnerNpkFileList()
            {
                new InnerNpkFile("blahblahblah_0.img", "sprite/character/fighter/blahblahblah_0.img"),
                new InnerNpkFile("blahblahblah_1.img", "sprite/character/fighter/blahblahblah_1.img"),
                new InnerNpkFile("blahblahblah_2.img", "sprite/character/fighter/blahblahblah_2.img")
            };

            InnerFileList.MoveCurrentToFirst();

            FrameList = new FrameList()
            {
                new FrameMetadata(0, 85, 196, 200, 7, null),
                new FrameMetadata(1, 100, 185, 205, 15, null),
                new FrameMetadata(2, 100, 185, 205, 7, 0)
            };

            CurrentFrameImage = null;
            Status = "I'm the status";
            OpenNPKPath = @"C:\Neople\DFO\ImagePacks2\blah.NPK";
            CanOpen = true;
            CanQuickSaveAsPng = true;
        }

        public void Open(string npkPath)
        {

        }

        public QuickSaveResults QuickSaveAsPng(string imgPath, int frameIndex)
        {
            return new QuickSaveResults() { Error = new DFOToolboxException("Designer viewmodel doesn't support saving.") };
        }

        public bool CanEditFrame { get; set; }

        public void EditFrame(string imgPath, int frameIndex, string pngFilePath)
        {
            
        }
    }
}
