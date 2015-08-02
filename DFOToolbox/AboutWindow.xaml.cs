using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using System.Windows.Shapes;

namespace DFOToolbox
{
    /// <summary>
    /// Interaction logic for About.xaml
    /// </summary>
    public partial class AboutWindow : Window
    {
        public AboutWindowViewModel ViewModel { get; set; }
        
        public AboutWindow()
        {
            ViewModel = new AboutWindowViewModel();
            InitializeComponent();
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            try
            {
                using (Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri)))
                {
                    ;
                }
            }
            catch (Exception)
            {
                // TODO: Log
            }
        }
    }
}
