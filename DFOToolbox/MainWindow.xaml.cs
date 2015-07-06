using System;
using System.Windows;

namespace DFOToolbox
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IDisposable
    {
        public MainWindowViewModel ViewModel { get; set; }
        public MainWindowViewCommands ViewCommands { get; set; }

        public MainWindow()
        {
            ViewModel = new MainWindowViewModel();
            ViewCommands = new MainWindowViewCommands(this, ViewModel);
            InitializeComponent();
        }

        public void Dispose()
        {
            if (ViewModel != null)
            {
                ViewModel.Dispose();
            }
        }
    }
}
