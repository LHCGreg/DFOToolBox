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
    public class DesignerMainWindowViewModel : IMainWindowViewModel
    {
        public InnerNpkFileList InnerFileList { get; set; }
        public FrameList FrameList { get; set; }
        public ImageSource CurrentFrameImage { get; set; }
        public DelegateCommand<string> OpenCommand { get; set; }
        public bool OpenCommandCanExecute { get; set; }
        public DelegateCommand QuickSaveAsPngCommand { get; set; }
        public bool QuickSaveAsPngCommandCanExecute { get; set; }

        public DesignerMainWindowViewModel()
        {
            InnerFileList = new InnerNpkFileList()
            {
                new InnerNpkFile("foo.img", "images/foo.img"),
                new InnerNpkFile("bar.img", "images/bar.img"),
                new InnerNpkFile("baz.img", "Images/baz.img")
            };

            FrameList = new FrameList()
            {
                new FrameMetadata(0, 85, 196, 200, 7, null),
                new FrameMetadata(1, 100, 185, 205, 15, null),
                new FrameMetadata(2, 100, 185, 205, 7, 0)
            };

            CurrentFrameImage = null;
            OpenCommand = new DelegateCommand<string>((path) => {});
            OpenCommandCanExecute = true;
            QuickSaveAsPngCommand = new DelegateCommand(() => { });
            QuickSaveAsPngCommandCanExecute = true;
        }
    }
}
