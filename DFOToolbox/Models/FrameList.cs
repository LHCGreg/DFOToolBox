using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace DFOToolbox.Models
{
    public class FrameList : StrongMultiSelectListCollectionView<FrameMetadata>
    {
        public FrameList()
        {

        }

        public FrameList(ObservableCollection<FrameMetadata> frames)
            : base(frames)
        {

        }
    }
}
