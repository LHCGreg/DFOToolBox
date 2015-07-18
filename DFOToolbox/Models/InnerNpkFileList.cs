using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace DFOToolbox.Models
{
    public class InnerNpkFileList : StrongListCollectionView<InnerNpkFile>
    {
        public InnerNpkFileList()
        {

        }
        
        public InnerNpkFileList(ObservableCollection<InnerNpkFile> files)
            : base(files)
        {

        }
    }
}
