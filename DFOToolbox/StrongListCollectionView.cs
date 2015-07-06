using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace DFOToolbox
{
    public class StrongListCollectionView<T> : ListCollectionView
    {
        public StrongListCollectionView()
            : base(new ObservableCollection<T>())
        {

        }

        public StrongListCollectionView(ObservableCollection<T> values)
            : base(values)
        {

        }

        public void Clear()
        {
            InternalList.Clear();
        }

        public void Add(T item)
        {
            InternalList.Add(item);
        }

        public T Current { get { return (T)CurrentItem; } }
    }
}
