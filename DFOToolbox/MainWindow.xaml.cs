using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using DFO.Common;
using DFO.Npk;
using DFOToolbox.Models;
using DFO.Common.Images;

namespace DFOToolbox
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IDisposable
    {
        private NpkReader _npk;

        // Models
        public InnerNpkFileList InnerFileList { get; set; }
        public CollectionViewSource InnerFileListViewSource { get; set; }
        public FrameList FrameList { get; set; }
        public CollectionViewSource FrameListViewSource { get; set; }

        public MainWindow()
        {
            InnerFileList = new InnerNpkFileList();
            FrameList = new FrameList();

            InitializeComponent();

            InnerFileListViewSource = (CollectionViewSource)this.TryFindResource("InnerFileListView");
            InnerFileListViewSource.View.CurrentChanged += SelectedInnerFileChanged;

            FrameListViewSource = (CollectionViewSource)this.TryFindResource("FrameListView");
            FrameListViewSource.View.CurrentChanged += SelectedFrameChanged;
        }

        // TODO: Make this a command instead of a direct click handler
        private void mnu_exit_click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void OpenCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            // Get file to open
            // TODO: Detect DFO installation directory and default to that
            OpenFileDialog filePicker = new OpenFileDialog()
            {
                CheckFileExists = true,
                CheckPathExists = true,
                DereferenceLinks = true,
                Filter = "NPK files (*.NPK)|*.NPK",
                Multiselect = false,
                Title = "Select NPK file"
            };

            if (filePicker.ShowDialog() != true)
            {
                return;
            }

            string npkPath = filePicker.FileName;

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
                InnerFileListViewSource.View.MoveCurrentToFirst();
            }
        }

        private void SelectedInnerFileChanged(object sender, EventArgs e)
        {
            // Clear frame list
            FrameList.Clear();

            // Get current selected inner file
            InnerNpkFile selectedFile = (InnerNpkFile)InnerFileListViewSource.View.CurrentItem;

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
                // Better way of displaying than modal?
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

            // Select first frame
            if (FrameList.Count > 0)
            {
                FrameListViewSource.View.MoveCurrentToFirst();
            }
        }

        private void SelectedFrameChanged(object sender, EventArgs e)
        {
            FrameMetadata frame = (FrameMetadata)FrameListViewSource.View.CurrentItem;
            if (frame == null)
            {
                CurrentFrameImage.Source = null;
                CurrentFrameImage.Height = double.NaN;
                CurrentFrameImage.Width = double.NaN;
                return;
            }

            InnerNpkFile selectedFile = (InnerNpkFile)InnerFileListViewSource.View.CurrentItem;

            DFO.Common.Images.Image image = null;
            try
            {
                image = _npk.GetImage(selectedFile.Path, frame.Index);
            }
            catch (Exception ex)
            {
                // TODO: Log this and maybe display something
                CurrentFrameImage.Source = null;
                CurrentFrameImage.Height = double.NaN;
                CurrentFrameImage.Width = double.NaN;
                return;
            }

            // RGBA -> BGRA (for little endian platforms), (BGRA for big endian platforms) - seems to not be reversed for little endian???
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

            BitmapSource bitmap = BitmapSource.Create(frame.Width, frame.Height, dpiX: 96, dpiY: 96, pixelFormat: PixelFormats.Bgra32, palette: null, pixels: convertedBytes, stride: 4 * frame.Width);
            this.CurrentFrameImage.Source = bitmap;
            this.CurrentFrameImage.Width = frame.Width;
            this.CurrentFrameImage.Height = frame.Height;
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
