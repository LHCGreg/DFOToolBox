using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DFO.Common;
using DFO.Common.Images;
using DFO.Images;
using DFO.Npk;
using DFO.Utilities;
using DFOToolbox.Models;

namespace DFOToolbox
{
    public class MainWindowViewModel : NotifyPropertyChangedBase, IMainWindowViewModel, IDisposable
    {
        private NpkReader _npk;

        private const string _quicksaveFolderName = "DFOToolbox";
        private string _quicksaveFolderPath;

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

        private string _status;
        public string Status
        {
            get { return _status; }
            set { _status = value; OnPropertyChanged(); }
        }

        // Wiring up command objects' CanExecute() seems complicated and/or inefficient, so just expose a property for that.
        // This way changes can be notified.

        private bool _canOpen;
        public bool CanOpen
        {
            get { return _canOpen; }
            set { if (value != _canOpen) { _canOpen = value; OnPropertyChanged(); } }
        }

        public const string PropertyNameCanOpen = "CanOpen";

        private bool GetCanOpen()
        {
            return true;
        }

        private void RefreshCanOpen()
        {
            CanOpen = GetCanOpen();
        }

        private bool _canQuickSaveAsPng;
        public bool CanQuickSaveAsPng
        {
            get { return _canQuickSaveAsPng; }
            set { if (value != _canQuickSaveAsPng) { _canQuickSaveAsPng = value; OnPropertyChanged(); } }
        }

        public const string PropertyNameCanQuickSaveAsPng = "CanQuickSaveAsPng";

        private bool GetCanQuickSaveAsPng()
        {
            return FrameList.SelectedItems.Count > 0;
        }

        private void RefreshCanQuickSaveAsPng()
        {
            CanQuickSaveAsPng = GetCanQuickSaveAsPng();
        }

        public MainWindowViewModel()
        {
            // TODO: inject dependencies
            _quicksaveFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), _quicksaveFolderName);

            InnerFileList = new InnerNpkFileList();
            InnerFileList.CurrentChanged += SelectedInnerFileChanged;

            FrameList = new FrameList();
            // Note that SelectedItems may not be up to date yet in the CurrentChanged event handler
            FrameList.CurrentChanged += SelectedFrameChanged;
            FrameList.SelectedItems.CollectionChanged += (sender, e) => RefreshCanQuickSaveAsPng();

            CanOpen = GetCanOpen();
            CanQuickSaveAsPng = GetCanQuickSaveAsPng();
        }

        public void Open(string npkPath)
        {
            if (!CanOpen)
            {
                return;
            }
            
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

        /// <summary>
        /// If there is an expected error, the Error property of the results is set to a DFOToolboxException with a message
        /// suitable for display.
        /// </summary>
        /// <param name="imgPath"></param>
        /// <param name="frameIndex"></param>
        /// <returns></returns>
        public QuickSaveResults QuickSaveAsPng(string imgPath, int frameIndex)
        {
            if (imgPath == null) throw new ArgumentNullException("imgPath");

            QuickSaveResults results = new QuickSaveResults();

            string filename = "{0}.{1}.png".F(SanitizeImgPathForFilename(imgPath), frameIndex);
            string path = Path.Combine(_quicksaveFolderPath, filename);
            results.OutputFolder = _quicksaveFolderPath;
            results.OutputPath = path;

            // Save to (folder)/(sanitized img path).(frame number).png
            if (!Directory.Exists(_quicksaveFolderPath))
            {
                try
                {
                    Directory.CreateDirectory(_quicksaveFolderPath);
                }
                catch (Exception ex)
                {
                    results.Error = new DFOToolboxException("Could not create quicksave directory {0}".F(_quicksaveFolderPath), ex);
                    return results;
                }
            }

            try
            {
                using (FileStream pngOutput = File.Open(path, FileMode.Create))
                {
                    Export.ToPng(_npk, imgPath, frameIndex, pngOutput);
                }
            }
            catch (Exception ex)
            {
                results.Error = new DFOToolboxException("Error saving to {0}".F(path), ex);
                return results;
            }

            return results;
        }

        private string SanitizeImgPathForFilename(string imgPath)
        {
            imgPath = imgPath.Replace('/', '.').Replace('\\', '.');
            foreach (char invalidChar in Path.GetInvalidFileNameChars())
            {
                imgPath = imgPath.Replace(invalidChar.ToString(), "");
            }
            return imgPath;
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
