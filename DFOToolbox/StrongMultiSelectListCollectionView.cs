using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace DFOToolbox
{
    public class StrongMultiSelectListCollectionView<T> : MultiSelectCollectionView<T>
        where T : ISelectable
    {
        public StrongMultiSelectListCollectionView()
            : base(new ObservableCollection<T>())
        {

        }

        public StrongMultiSelectListCollectionView(ObservableCollection<T> values)
            : base(values)
        {

        }

        public void Clear()
        {
            SelectedItems.Clear();
            InternalList.Clear();
        }

        public void Add(T item)
        {
            InternalList.Add(item);
        }

        public T Current { get { return (T)CurrentItem; } }
    }
}
