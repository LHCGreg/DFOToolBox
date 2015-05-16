using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace DFO.Npk
{
    /// <summary>
    /// Indicates a malformed .npk file.
    /// </summary>
    [Serializable]
    public class NpkException : Exception
    {
        public NpkException() { }
        public NpkException(string message) : base(message) { }
        public NpkException(string message, Exception inner) : base(message, inner) { }
        protected NpkException(
          SerializationInfo info,
          StreamingContext context)
            : base(info, context) { }
    }
}
