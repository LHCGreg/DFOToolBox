using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DFO.Utilities
{
    public class DeepReadOnlyCollection<TMutable, TConst> : ICollection<TConst>
    {
        private ICollection<TMutable> m_wrapped;
        private Func<TMutable, TConst> m_constFunc;
        private Func<ICollection<TMutable>, TConst, bool> m_containsFunc;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="collectionToWrap"></param>
        /// <param name="convertMutableToConstFunc"></param>
        /// <param name="containsFunc">A function returning true if the given collection contains the given
        /// <typeparamref name="TConst"/>. The Contains() method will use this function if it is not null.
        /// If it is null, Contains() will go through the wrapped collection, converting each element to
        /// a const value, and compare using Equals().</param>
        /// <exception cref="System.ArgumentNullException"><paramref name="collectionToWrap"/> or
        /// <paramref name="convertMutableToConstFunc"/> is null.</exception>
        public DeepReadOnlyCollection(ICollection<TMutable> collectionToWrap,
            Func<TMutable, TConst> convertMutableToConstFunc,
            Func<ICollection<TMutable>, TConst, bool> containsFunc)
        {
            collectionToWrap.ThrowIfNull("collectionToWrap");
            convertMutableToConstFunc.ThrowIfNull("convertMutableToConstFunc");

            m_wrapped = collectionToWrap;
            m_constFunc = convertMutableToConstFunc;
            m_containsFunc = containsFunc;
        }

        private TConst MakeValueConst(TMutable mutable)
        {
            if (mutable != null)
            {
                return m_constFunc(mutable);
            }
            else
            {
                return default(TConst);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <exception cref="System.NotSupportedException">Always thrown.</exception>
        void ICollection<TConst>.Add(TConst item)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <exception cref="System.NotSupportedException">Always thrown.</exception>
        void ICollection<TConst>.Clear()
        {
            throw new NotSupportedException();
        }

        bool ICollection<TConst>.Contains(TConst item)
        {
            if (m_containsFunc != null)
            {
                return m_containsFunc(m_wrapped, item);
            }
            else
            {
                foreach (TMutable mutable in m_wrapped)
                {
                    if (object.Equals(item, MakeValueConst(mutable)))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="array"></param>
        /// <param name="arrayIndex"></param>
        /// <exception cref="System.ArgumentNullException"><paramref name="array"/> is null.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="arrayIndex"/> is less than 0.</exception>
        /// <exception cref="System.ArgumentException">The number of elements in this collection is greater
        /// than the available space from <paramref name="arrayIndex"/> to the end of the destination array.</exception>
        public void CopyTo(TConst[] array, int arrayIndex)
        {
            array.ThrowIfNull("array");

            if (arrayIndex < 0)
            {
                throw new ArgumentOutOfRangeException("arrayIndex");
            }

            if (m_wrapped.Count > array.Length - arrayIndex)
            {
                throw new ArgumentException("The number of elements in the collection is greater than the array can hold.");
            }

            foreach (TMutable mutable in m_wrapped)
            {
                array[arrayIndex] = MakeValueConst(mutable);
                arrayIndex++;
            }
        }

        public int Count
        {
            get { return m_wrapped.Count; }
        }

        bool ICollection<TConst>.IsReadOnly
        {
            get { return true; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        /// <exception cref="System.NotSupportedException">Always thrown.</exception>
        bool ICollection<TConst>.Remove(TConst item)
        {
            throw new NotSupportedException();
        }

        public IEnumerator<TConst> GetEnumerator()
        {
            foreach (TMutable mutable in m_wrapped)
            {
                yield return MakeValueConst(mutable);
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            foreach (TMutable mutable in m_wrapped)
            {
                yield return MakeValueConst(mutable);
            }
        }
    }
}
