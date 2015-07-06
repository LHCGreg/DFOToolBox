using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Win32;

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
            return ViewModel.QuickSaveAsPngCommandCanExecute;
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
            return ViewModel.OpenCommandCanExecute;
        }

        private void RefreshCanOpen()
        {
            OpenCommandCanExecute = CanOpen();
        }

        private async void OnOpen()
        {
            if(!CanOpen())
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
            await ViewModel.OpenCommand.Execute(npkPath);
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

        public MainWindowViewCommands(MainWindow window, MainWindowViewModel viewModel)
        {
            Window = window;
            ViewModel = viewModel;
            ViewModel.PropertyChanged += OnViewModelPropertyChanged;
            OpenCommand = new DelegateCommand(OnOpen, CanOpen);
            QuickSaveAsPngCommand = new DelegateCommand(async () => await ViewModel.QuickSaveAsPngCommand.Execute(), CanQuickSaveAsPng);
            ExitCommand = new DelegateCommand(OnExit, CanExit);

            RefreshCanOpen();
            RefreshCanQuickSaveAsPng();
            RefreshCanExit();
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == MainWindowViewModel.PropertyNameSaveAsPngCommandCanExecute)
            {
                RefreshCanQuickSaveAsPng();
            }
            else if (e.PropertyName == MainWindowViewModel.PropertyNameOpenCommandCanExecute)
            {
                RefreshCanOpen();
            }
        }
    }
}
