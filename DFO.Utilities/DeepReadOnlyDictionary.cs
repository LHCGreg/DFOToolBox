using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DFO.Utilities
{
    public class DeepReadOnlyDictionary<TKey, TValue, TConstValue> : DFO.Utilities.IReadOnlyDictionary<TKey, TConstValue>
    {
        private IDictionary<TKey, TValue> m_wrapped;
        private Func<TValue, TConstValue> m_constFunc;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dictToWrap"></param>
        /// <param name="convertValueToConstFunc">This function must convert a <typeparamref name="TValue"/>
        /// to a <typeparamref name="TConstValue"/>. It need not be able to handle null values.</param>
        /// <exception cref="System.ArgumentNullException"><paramref name="dictToWrap"/> or
        /// <paramref name="convertValueToConstFunc"/> is null.</exception>
        public DeepReadOnlyDictionary(IDictionary<TKey, TValue> dictToWrap,
            Func<TValue, TConstValue> convertValueToConstFunc)
        {
            dictToWrap.ThrowIfNull("dictToWrap");
            convertValueToConstFunc.ThrowIfNull("convertValueToConstFunc");

            m_wrapped = dictToWrap;
            m_constFunc = convertValueToConstFunc;
        }

        private TConstValue MakeValueConst(TValue value)
        {
            if (value != null)
            {
                return m_constFunc(value);
            }
            else
            {
                return default(TConstValue);
            }
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
        public bool TryGetValue(TKey key, out TConstValue value)
        {
            TValue mutableValueGotten;
            if (m_wrapped.TryGetValue(key, out mutableValueGotten))
            {
                value = MakeValueConst(mutableValueGotten);
                return true;
            }
            else
            {
                value = default(TConstValue);
                return false;
            }
        }

        public ICollection<TConstValue> Values
        {
            get { return new DeepReadOnlyCollection<TValue, TConstValue>(m_wrapped.Values, m_constFunc, null); }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="key"/> is null.</exception>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException">When getting a value,
        /// the given key does not exist.</exception>
        public TConstValue this[TKey key]
        {
            get
            {
                return MakeValueConst(m_wrapped[key]);
            }
        }

        public bool Contains(KeyValuePair<TKey, TConstValue> item)
        {
            TValue mutableValue;
            if (m_wrapped.TryGetValue(item.Key, out mutableValue))
            {
                if (object.Equals(item.Value, MakeValueConst(mutableValue)))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public int Count
        {
            get { return m_wrapped.Count; }
        }

        public IEnumerator<KeyValuePair<TKey, TConstValue>> GetEnumerator()
        {
            foreach (KeyValuePair<TKey, TValue> mutablePair in m_wrapped)
            {
                yield return new KeyValuePair<TKey, TConstValue>(mutablePair.Key, MakeValueConst(mutablePair.Value));
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            foreach (KeyValuePair<TKey, TValue> mutablePair in m_wrapped)
            {
                yield return new KeyValuePair<TKey, TConstValue>(mutablePair.Key, MakeValueConst(mutablePair.Value));
            }
        }
    }
}
