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
        }

        private void SelectedFrameChanged(object sender, EventArgs e)
        {
            // TODO
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
