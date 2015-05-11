using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DFO.Utilities
{
    public class ReadOnlyDictionary<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>
    {
        private IDictionary<TKey, TValue> m_wrapped;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dictToWrap"></param>
        /// <exception cref="System.ArgumentNullException"><paramref name="dictToWrap"/> is null.</exception>
        public ReadOnlyDictionary(IDictionary<TKey, TValue> dictToWrap)
        {
            dictToWrap.ThrowIfNull("dictToWrap");
            m_wrapped = dictToWrap;
        }

        public bool ContainsKey(TKey key)
        {
            return m_wrapped.ContainsKey(key);
        }

        public ICollection<TKey> Keys
        {
            get { return m_wrapped.Keys; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="key"/> is null.</exception>
        public bool TryGetValue(TKey key, out TValue value)
        {
            return m_wrapped.TryGetValue(key, out value);
        }

        public ICollection<TValue> Values
        {
            get { return m_wrapped.Values; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="key"/> is null.</exception>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException">When getting a value,
        /// the given key does not exist.</exception>
        public TValue this[TKey key]
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return m_wrapped.Contains(item);
        }

        public int Count
        {
            get { return m_wrapped.Count; }
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return m_wrapped.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return ((System.Collections.IEnumerable)m_wrapped).GetEnumerator();
        }
    }
}
