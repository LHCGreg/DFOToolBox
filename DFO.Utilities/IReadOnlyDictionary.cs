using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DFO.Utilities
{
    /// <summary>
    /// Represents a generic collection of key/value pairs where new key/value pairs cannot be added, and
    /// existing pairs cannot be modified or removed.
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public interface IReadOnlyDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
    {
        ICollection<TKey> Keys { get; }
        ICollection<TValue> Values { get; }
        int Count { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="key"/> is null.</exception>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException">The key is not found.</exception>
        TValue this[TKey key] { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="key"/> is null.</exception>
        bool ContainsKey(TKey key);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="key"/> is null.</exception>
        bool TryGetValue(TKey key, out TValue value);
    }
}
