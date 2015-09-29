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
        // Always set, never null
        private NpkEditor _editor = new NpkEditor();

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

        // For all frames together. Set when the selected .img changes.
        private int _smallestX;
        private int _largestX;
        private int _smallestY;
        private int _largestY;
        private int _width;
        private int _height;

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

        private string _openNPKPath;
        /// <summary>
        /// Full path of the currently open NPK file. Null if none opened currently.
        /// </summary>
        public string OpenNPKPath
        {
            get { return _openNPKPath; }
            set { _openNPKPath = value; OnPropertyChanged(); }
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

        private bool _canEditFrame;
        public bool CanEditFrame
        {
            get { return _canEditFrame; }
            set { if (value != _canEditFrame) { _canEditFrame = value; OnPropertyChanged(); } }
        }

        public const string PropertyNameCanEditFrame = "CanEditFrame";

        private bool GetCanEditFrame()
        {
            return FrameList.Current != null;
        }

        private void RefreshCanEditFrame()
        {
            CanEditFrame = GetCanEditFrame();
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
            FrameList.CurrentChanged += (sender, e) => RefreshCanEditFrame();
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
                _editor.Open(npkPath);
            }
            catch (Exception ex)
            {
                // Display error
                // Handle NpkExcption separately?
                // Better way of displaying than modal?
                MessageBox.Show(string.Format("Error opening NPK file: {0}", ex.Message));
                return;
            }

            OpenNPKPath = npkPath;

            InnerFileList.Clear();
            FrameList.Clear();
            foreach (NpkPath imgPath in _editor.Images.Keys)
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
            RefreshFrameList();

            // Select first frame
            if (FrameList.Count > 0)
            {
                FrameList.MoveCurrentToFirst();
            }
        }

        private void RefreshFrameList()
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
                frames = _editor.Frames[selectedFile.Path].ToList();
            }
            catch (Exception ex)
            {
                // TODO: Better way of displaying than modal?
                // At least get the view to show it instead of the view model
                MessageBox.Show(string.Format("Error reading frames from NPK file: {0}", ex.Message));
                return;
            }

            _smallestX = 0;
            _largestX = 0;
            _smallestY = 0;
            _largestY = 0;
            _width = 1;
            _height = 1;

            List<FrameInfo> nonLinkFrames = frames.Where(f => f.LinkFrame == null).ToList();

            if (nonLinkFrames.Count > 0)
            {
                FrameInfo.GetNormalizedCoordinates(nonLinkFrames, out _smallestX, out _largestX, out _smallestY, out _largestY);
                _width = _largestX - _smallestX + 1;
                _height = _largestY - _smallestY + 1;
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
                        // TODO: Log error that link is out of range?
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
                image = _editor.GetImage(selectedFile.Path, frame.Index);
            }
            catch (Exception)
            {
                // TODO: Log this and maybe display something
                CurrentFrameImage = null;
                return;
            }

            // Adjust position in bounding box according to frame coordinates
            // go from (0, 0) based coordinates to (_smallestX, _smallestY) based coordinates
            // (0, 0) -> (45, 50)
            // (60, 55) -> (15, 5)
            // frame x - _smallestX = bounding box X
            // frame y - _smallestY = bounding box y
            // Paint image in bounding box

            // RGBA -> BGRA (for little endian platforms), (BGRA for big endian platforms) - seems to not be reversed for little endian???
            // TODO: Make NpkReader able to output in variable format so it doesn't need to be converted
            byte[] frameBytes = new byte[_width * _height * 4];

            // Get X in frame
            // bounding box X + _smallestX = frame x
            // (5, 0) 5x5
            // (3, 1), 10x6
            // smallest x: 3
            // smallest y: 0
            // (0, 0): 0 + 3
            for (int boundingBoxY = 0; boundingBoxY < _height; boundingBoxY++)
            {
                int frameY = boundingBoxY + _smallestY;
                int rowOffset = boundingBoxY * _width * 4;

                // if this row is above or below the current frame, draw a row of transparent pixels
                if (frameY < image.Attributes.LocationY || frameY > image.Attributes.LocationY + image.Attributes.Height - 1)
                {
                    for (int boundingBoxX = 0; boundingBoxX < _width; boundingBoxX++)
                    {
                        int pixelOffset = rowOffset + (boundingBoxX * 4);
                        frameBytes[pixelOffset] = 0;
                        frameBytes[pixelOffset + 1] = 0;
                        frameBytes[pixelOffset + 2] = 0;
                        frameBytes[pixelOffset + 3] = 0;
                    }
                }
                else
                {
                    for (int boundingBoxX = 0; boundingBoxX < _width; boundingBoxX++)
                    {
                        int frameX = boundingBoxX + _smallestX;
                        int pixelOffset = rowOffset + (boundingBoxX * 4);

                        // if this column is to the left or right of the current frame, draw a transparent pixel
                        if (frameX < image.Attributes.LocationX || frameX > image.Attributes.LocationX + image.Attributes.Width - 1)
                        {
                            frameBytes[pixelOffset] = 0;
                            frameBytes[pixelOffset + 1] = 0;
                            frameBytes[pixelOffset + 2] = 0;
                            frameBytes[pixelOffset + 3] = 0;
                        }
                        else
                        {
                            // RGBA -> BGRA
                            int zeroBasedFrameY = frameY - image.Attributes.LocationY;
                            int zeroBasedFrameX = frameX - image.Attributes.LocationX;
                            int framePixelOffset = zeroBasedFrameY * image.Attributes.Width * 4 + zeroBasedFrameX * 4;
                            frameBytes[pixelOffset] = image.PixelData[framePixelOffset + 2];  // B
                            frameBytes[pixelOffset + 1] = image.PixelData[framePixelOffset + 1]; // G
                            frameBytes[pixelOffset + 2] = image.PixelData[framePixelOffset]; // R
                            frameBytes[pixelOffset + 3] = image.PixelData[framePixelOffset + 3]; // A
                        }
                    }
                }
            }

            CurrentFrameImage = BitmapSource.Create(_width, _height, dpiX: 96, dpiY: 96, pixelFormat: PixelFormats.Bgra32, palette: null, pixels: frameBytes, stride: 4 * _width);
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
                    Export.ToPng(_editor, imgPath, frameIndex, pngOutput);
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="imgPath"></param>
        /// <param name="frameIndex"></param>
        /// <param name="pngFilePath"></param>
        /// <exception cref="DFOToolbox.DFOToolboxException">Something went wrong while editing. Message is suitable for UI display.</exception>
        /// <exception cref="System.Exception">Other errors resulting from incorrect usage of this function, such as passing null arguments or trying to edit a frame while no file is open.</exception>
        public void EditFrame(string imgPath, int frameIndex, string pngFilePath)
        {
            // validate that we have a file open for editing, that it has the img, that it has the frame index, that the file exists
            imgPath.ThrowIfNull("imgPath");
            pngFilePath.ThrowIfNull("pngFileName");
            if (!_editor.IsOpen)
            {
                throw new InvalidOperationException("Cannot edit a frame because no file is currently open.");
            }

            NpkPath npkPath = imgPath;
            if (!_editor.Frames.ContainsKey(npkPath))
            {
                throw new KeyNotFoundException("There is no img with path {0}.".F(imgPath));
            }

            if (frameIndex >= _editor.Frames[npkPath].Count)
            {
                throw new ArgumentOutOfRangeException("{0} does not have a frame with index {1}.".F(imgPath, frameIndex));
            }

            FrameInfo uneditedFrameMetadata = _editor.Frames[npkPath][frameIndex];

            // Use same pixel format as original.
            // If it's a link frame, we're turning it into a non-link frame, so use 8888 as that will preserve the colors of the new image.
            PixelDataFormat pixelFormatToUse = uneditedFrameMetadata.PixelFormat;
            if (pixelFormatToUse == PixelDataFormat.Link)
            {
                pixelFormatToUse = PixelDataFormat.EightEightEightEight;
            }

            if (!File.Exists(pngFilePath))
            {
                throw new DFOToolboxException("{0} does not exist.".F(pngFilePath));
            }

            byte[] newImageBytesInCorrectFormat;
            int newImageWidth;
            int newImageHeight;

            // Load image pixels into memory
            // TODO: catch exceptions
            using (System.Drawing.Bitmap inputImage = new System.Drawing.Bitmap(pngFilePath))
            {
                // need to get pixels into format used by image
                newImageBytesInCorrectFormat = PixelConversion.Convert(inputImage, pixelFormatToUse);
                newImageWidth = inputImage.Width;
                newImageHeight = inputImage.Height;
            }

            using (MemoryStream newImageBytesInCorrectFormatStream = new MemoryStream(newImageBytesInCorrectFormat))
            {
                FrameInfo newFrameMetadata = new FrameInfo(
                    isCompressed: uneditedFrameMetadata.IsCompressed,
                    compressedLength: -1, // not used
                    pixelFormat: pixelFormatToUse,
                    width: newImageWidth,
                    height: newImageHeight,
                    locationX: uneditedFrameMetadata.LocationX,
                    locationY: uneditedFrameMetadata.LocationY,
                    maxWidth: uneditedFrameMetadata.MaxWidth,
                    maxHeight: uneditedFrameMetadata.MaxHeight
                );

                _editor.EditFrame(npkPath, frameIndex, newFrameMetadata, newImageBytesInCorrectFormatStream);
            }

            // XXX: This code assumes the imgPath passed in is currently selected
            RefreshFrameList();
            FrameList.MoveCurrentToPosition(frameIndex);
        }

        public void Dispose()
        {
            if (_editor != null)
            {
                _editor.Dispose();
            }
        }
    }
}
