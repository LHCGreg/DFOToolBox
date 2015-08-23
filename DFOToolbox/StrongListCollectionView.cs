using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
            this.CurrentChanged += (sender, e) => OnPropertyChanged(new PropertyChangedEventArgs(PropNameCurrent));
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

        private const string PropNameCurrent = "Current";
        public T Current { get { return (T)CurrentItem; } }
    }
}
