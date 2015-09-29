using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Win32;
using DFO.Utilities;
using DFOToolbox.Models;
using System.Windows;

namespace DFOToolbox
{
    // View uses commands here, which possibly do view-level logic before going to the viewmodel command,
    // or possibly not going to the viewmodel at all if it's a purely view-level command, such as exiting.
    public class MainWindowViewCommands : NotifyPropertyChangedBase
    {
        private MainWindow Window { get; set; }
        private MainWindowViewModel ViewModel { get; set; }

        public DelegateCommand QuickSaveAsPngCommand { get; private set; }

        private bool _quickSaveAsPngCommandCanExecute;
        public bool QuickSaveAsPngCommandCanExecute
        {
            get { return _quickSaveAsPngCommandCanExecute; }
            set { if (value != _quickSaveAsPngCommandCanExecute) { _quickSaveAsPngCommandCanExecute = value; OnPropertyChanged(); } }
        }

        private bool CanQuickSaveAsPng()
        {
            return ViewModel.CanQuickSaveAsPng;
        }

        private void RefreshCanQuickSaveAsPng()
        {
            QuickSaveAsPngCommandCanExecute = CanQuickSaveAsPng();
        }

        public DelegateCommand OpenCommand { get; private set; }

        private bool _openCommandCanExecute;
        public bool OpenCommandCanExecute
        {
            get { return _openCommandCanExecute; }
            set { if (value != _openCommandCanExecute) { _openCommandCanExecute = value; OnPropertyChanged(); } }
        }

        private bool CanOpen()
        {
            return ViewModel.CanOpen;
        }

        private void RefreshCanOpen()
        {
            OpenCommandCanExecute = CanOpen();
        }

        private void OnOpen()
        {
            if (!CanOpen())
            {
                return;
            }

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
            ViewModel.Open(npkPath);
        }

        public DelegateCommand EditFrameCommand { get; private set; }

        private bool _editFrameCommandCanExecute;
        public bool EditFrameCommandCanExecute
        {
            get { return _editFrameCommandCanExecute; }
            set { if (value != _editFrameCommandCanExecute) { _editFrameCommandCanExecute = value; OnPropertyChanged(); } }
        }

        private bool CanEditFrame()
        {
            return ViewModel.CanEditFrame;
        }

        private void RefreshCanEditFrame()
        {
            EditFrameCommandCanExecute = CanEditFrame();
        }

        public DelegateCommand ExitCommand { get; private set; }

        private bool _exitCommandCanExecute;
        public bool ExitCommandCanExecute
        {
            get { return _exitCommandCanExecute; }
            set { if (value != _exitCommandCanExecute) { _exitCommandCanExecute = value; OnPropertyChanged(); } }
        }

        private bool CanExit()
        {
            return true;
        }

        private void RefreshCanExit()
        {
            ExitCommandCanExecute = CanExit();
        }

        private void OnExit()
        {
            Window.Close();
        }

        public DelegateCommand ShowAboutCommand { get; private set; }

        private bool _showAboutCommandCanExecute;
        public bool ShowAboutCommandCanExecute
        {
            get { return _showAboutCommandCanExecute; }
            set { if (value != _showAboutCommandCanExecute) { _showAboutCommandCanExecute = value; OnPropertyChanged(); } }
        }

        private bool CanShowAbout()
        {
            return true;
        }

        private void RefreshCanShowAbout()
        {
            ShowAboutCommandCanExecute = CanShowAbout();
        }

        private void OnShowAbout()
        {
            AboutWindow aboutWindow = new AboutWindow();
            aboutWindow.ShowDialog();
        }

        public MainWindowViewCommands(MainWindow window, MainWindowViewModel viewModel)
        {
            Window = window;
            ViewModel = viewModel;
            ViewModel.PropertyChanged += OnViewModelPropertyChanged;
            OpenCommand = new DelegateCommand(OnOpen); // Don't give a delegate for if the command can execute, the menu item IsEnabled seems buggy when that's done...just manually bind IsEnabled
            QuickSaveAsPngCommand = new DelegateCommand(OnQuickSaveAsPng);
            EditFrameCommand = new DelegateCommand(OnEditFrame);
            ExitCommand = new DelegateCommand(OnExit);
            ShowAboutCommand = new DelegateCommand(OnShowAbout);

            RefreshCanOpen();
            RefreshCanQuickSaveAsPng();
            RefreshCanEditFrame();
            RefreshCanExit();
            RefreshCanShowAbout();
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == MainWindowViewModel.PropertyNameCanQuickSaveAsPng)
            {
                RefreshCanQuickSaveAsPng();
            }
            else if (e.PropertyName == MainWindowViewModel.PropertyNameCanOpen)
            {
                RefreshCanOpen();
            }
            else if (e.PropertyName == MainWindowViewModel.PropertyNameCanEditFrame)
            {
                RefreshCanEditFrame();
            }
        }

        private void OnQuickSaveAsPng()
        {
            if (!CanQuickSaveAsPng())
            {
                return;
            }

            List<QuickSaveResults> results = new List<QuickSaveResults>();
            ICollection<FrameMetadata> selectedFrames = ViewModel.FrameList.SelectedItems;
            try
            {
                foreach (FrameMetadata frame in selectedFrames)
                {
                    results.Add(ViewModel.QuickSaveAsPng(ViewModel.InnerFileList.Current.Path, frame.Index));
                }
            }
            catch (Exception ex)
            {
                // unexpected exception
                // TODO: Log
                ViewModel.Status = "Unexpected error: {0}".F(ex.Message);
                return;
            }

            // TODO: Make new statuses fade in to give indication that it's a new status

            if (results.Any(r => r.Error != null))
            {
                // error
                // TODO: Log, more details
                if(results.Count == 1)
                {
                    ViewModel.Status = results[0].Error.Message;
                }
                else if(results.Count(r => r.Error != null) == 1)
                {
                    ViewModel.Status = results.First(r => r.Error != null).Error.Message;
                }
                else
                {
                    ViewModel.Status = "There were errors saving some frames.";
                }
            }
            else
            {
                // success
                if (results.Count == 1)
                {
                    ViewModel.Status = "Saved to {0}".F(results[0].OutputPath);
                }
                else
                {
                    ViewModel.Status = "Saved {0} images to {1}".F(results.Count, results[0].OutputFolder);
                }
            }
        }

        private void OnEditFrame()
        {
            if (!CanEditFrame())
            {
                return;
            }

            string imgPath = ViewModel.InnerFileList.Current.Path;
            int frameIndex = ViewModel.FrameList.Current.Index;

            OpenFileDialog filePicker = new OpenFileDialog()
            {
                CheckFileExists = true,
                CheckPathExists = true,
                Filter = "PNG files (*.PNG)|*.PNG",
                Multiselect = false,
                Title = "Select image file"
            };

            if (filePicker.ShowDialog() != true)
            {
                return;
            }

            string newFramePngPath = filePicker.FileName;

            try
            {
                ViewModel.EditFrame(imgPath, frameIndex, newFramePngPath);
            }
            catch (DFOToolboxException ex)
            {
                MessageBox.Show(string.Format("Error saving frame: {0}", ex.Message));
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Error saving frame: {0}", ex.Message));
            }
        }
    }
}
