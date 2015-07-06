using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DFO.Common;
using DFO.Common.Images;
using DFO.Npk;
using DFOToolbox.Models;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Win32;

namespace DFOToolbox
{
    public class MainWindowViewModel : NotifyPropertyChangedBase, IMainWindowViewModel, IDisposable
    {
        private NpkReader _npk;

        private InnerNpkFileList _innerFileList;
        public InnerNpkFileList InnerFileList
        {
            get { return _innerFileList; }
            set { _innerFileList = value; OnPropertyChanged(); }
        }

        private FrameList _frameList;
        public FrameList FrameList
        {
            get { return _frameList; }
            set { _frameList = value; OnPropertyChanged(); }
        }

        private ImageSource _currentFrameImage;
        public ImageSource CurrentFrameImage
        {
            get { return _currentFrameImage; }
            set { _currentFrameImage = value; OnPropertyChanged(); }
        }

        public DelegateCommand<string> OpenCommand { get; private set; }

        // Wiring up command objects' CanExecute() seems complicated and/or inefficient, so just expose a property for that.
        // This way changes can be notified
        private bool _openCommandCanExecute;
        public bool OpenCommandCanExecute
        {
            get { return _openCommandCanExecute; }
            set { if (value != _openCommandCanExecute) { _openCommandCanExecute = value; OnPropertyChanged(); } }
        }

        public const string PropertyNameOpenCommandCanExecute = "OpenCommandCanExecute";

        private bool CanOpen()
        {
            return true;
        }

        private void RefreshCanOpen()
        {
            OpenCommandCanExecute = CanOpen();
        }

        public DelegateCommand QuickSaveAsPngCommand { get; private set; }

        private bool _quickSaveAsPngCommandCanExecute;
        public bool QuickSaveAsPngCommandCanExecute
        {
            get { return _quickSaveAsPngCommandCanExecute; }
            set { if (value != _quickSaveAsPngCommandCanExecute) { _quickSaveAsPngCommandCanExecute = value; OnPropertyChanged(); } }
        }

        public const string PropertyNameSaveAsPngCommandCanExecute = "QuickSaveAsPngCommandCanExecute";

        private bool CanQuickSaveAsPng()
        {
            return FrameList.Current != null;
        }

        private void RefreshCanQuickSaveAsPng()
        {
            QuickSaveAsPngCommandCanExecute = CanQuickSaveAsPng();
        }

        public MainWindowViewModel()
        {
            // TODO: inject dependencies
            InnerFileList = new InnerNpkFileList();
            InnerFileList.CurrentChanged += SelectedInnerFileChanged;

            FrameList = new FrameList();
            FrameList.CurrentChanged += SelectedFrameChanged;
            FrameList.CurrentChanged += (sender, e) => RefreshCanQuickSaveAsPng();

            OpenCommand = new DelegateCommand<string>(OnOpen, npkPath => CanOpen());
            OpenCommandCanExecute = CanOpen();

            QuickSaveAsPngCommand = new DelegateCommand(OnQuickSaveAsPng, () => CanQuickSaveAsPng());
            QuickSaveAsPngCommandCanExecute = CanQuickSaveAsPng();
        }

        private void OnOpen(string npkPath)
        {
            // TODO: async?
            try
            {
                NpkReader oldNpk = _npk;
                _npk = new NpkReader(npkPath);
                if (oldNpk != null)
                {
                    oldNpk.Dispose();
                }
            }
            catch (Exception ex)
            {
                // Display error
                // Handle NpkExcption separately?
                // Better way of displaying than modal?
                MessageBox.Show(string.Format("Error opening NPK file: {0}", ex.Message));
                return;
            }

            InnerFileList.Clear();
            FrameList.Clear();
            foreach (NpkPath imgPath in _npk.Images.Keys)
            {
                string imgName = imgPath.GetPathComponents().LastOrDefault();
                if (imgName == null) continue; // TODO: Log this, something would have to be strange
                InnerFileList.Add(new InnerNpkFile(name: imgName, path: imgPath.Path));
            }

            // Select first .img
            if (InnerFileList.Count > 0)
            {
                InnerFileList.MoveCurrentToFirst();
            }
        }

        private void SelectedInnerFileChanged(object sender, EventArgs e)
        {
            // Clear frame list
            FrameList.Clear();

            // Get current selected inner file
            InnerNpkFile selectedFile = InnerFileList.Current;

            // If none selected nothing else to do here
            if (selectedFile == null)
            {
                return;
            }

            // Get the list of frames. Since it's lazy loaded, there could be a read error.
            List<FrameInfo> frames;
            try
            {
                frames = _npk.Frames[selectedFile.Path].ToList();
            }
            catch (Exception ex)
            {
                // TODO: Better way of displaying than modal?
                // At least get the view to show it instead of the view model
                MessageBox.Show(string.Format("Error reading frames from NPK file: {0}", ex.Message));
                return;
            }

            // Populate frame list
            for (int frameIndex = 0; frameIndex < frames.Count; frameIndex++)
            {
                FrameInfo frame = frames[frameIndex];
                // if linked frame, follow link
                if (frame.LinkFrame != null)
                {
                    int linkIndex = frame.LinkFrame.Value;
                    if (linkIndex < 0 || linkIndex >= frames.Count)
                    {
                        // todo: Log error that link is out of range?
                        FrameList.Add(new FrameMetadata(frameIndex, 0, 0, 0, 0, linkIndex));
                    }
                    else
                    {
                        FrameInfo linkedFrame = frames[linkIndex];
                        FrameList.Add(new FrameMetadata(linkedFrame, frameIndex, linkIndex));
                    }
                }
                else
                {
                    FrameList.Add(new FrameMetadata(frame, frameIndex, linkFrameIndex: null));
                }
            }

            // Select first frame
            if (FrameList.Count > 0)
            {
                FrameList.MoveCurrentToFirst();
            }
        }

        private void SelectedFrameChanged(object sender, EventArgs e)
        {
            FrameMetadata frame = FrameList.Current;
            if (frame == null)
            {
                CurrentFrameImage = null;
                return;
            }

            InnerNpkFile selectedFile = InnerFileList.Current;

            DFO.Common.Images.Image image = null;
            try
            {
                image = _npk.GetImage(selectedFile.Path, frame.Index);
            }
            catch (Exception)
            {
                // TODO: Log this and maybe display something
                CurrentFrameImage = null;
                return;
            }

            // RGBA -> BGRA (for little endian platforms), (BGRA for big endian platforms) - seems to not be reversed for little endian???
            // TODO: Make NpkReader able to output in variable format so it doesn't need to be converted
            byte[] convertedBytes = new byte[image.PixelData.Length];
            bool isLittleEndian = BitConverter.IsLittleEndian;

            if (isLittleEndian)
            {
                for (int i = 0; i < image.PixelData.Length; i += 4)
                {
                    convertedBytes[i] = image.PixelData[i + 2]; // B
                    convertedBytes[i + 1] = image.PixelData[i + 1]; // G
                    convertedBytes[i + 2] = image.PixelData[i]; // R
                    convertedBytes[i + 3] = image.PixelData[i + 3]; // A
                }
            }
            else
            {
                for (int i = 0; i < image.PixelData.Length; i += 4)
                {
                    convertedBytes[i] = image.PixelData[i + 2]; // B
                    convertedBytes[i + 1] = image.PixelData[i + 1]; // G
                    convertedBytes[i + 2] = image.PixelData[i]; // R
                    convertedBytes[i + 3] = image.PixelData[i + 3]; // A
                }
            }

            CurrentFrameImage = BitmapSource.Create(frame.Width, frame.Height, dpiX: 96, dpiY: 96, pixelFormat: PixelFormats.Bgra32, palette: null, pixels: convertedBytes, stride: 4 * frame.Width);
        }

        private void OnQuickSaveAsPng()
        {
            if (!CanQuickSaveAsPng())
            {
                return;
            }
            // TODO
            MessageBox.Show("Save as PNG");
        }

        public void Dispose()
        {
            if (_npk != null)
            {
                _npk.Dispose();
            }
        }
    }
}
