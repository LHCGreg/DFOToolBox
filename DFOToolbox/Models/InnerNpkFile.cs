using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DFOToolbox.Models
{
    public class InnerNpkFile : INotifyPropertyChanged
    {
        private string _name;
        public string Name
        {
            get { return _name; }
            set { _name = value; OnPropertyChanged(); }
        }

        private string _path;
        public string Path
        {
            get { return _path; }
            set { _path = value; OnPropertyChanged(); }
        }

        public InnerNpkFile()
        {

        }

        public InnerNpkFile(string name, string path)
        {
            _name = name;
            _path = path;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName]string caller = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(caller));
            }
        }
    }
}
