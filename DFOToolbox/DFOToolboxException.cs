using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DFOToolbox
{
    [Serializable]
    public class DFOToolboxException : Exception
    {
        public DFOToolboxException() { }
        public DFOToolboxException(string message) : base(message) { }
        public DFOToolboxException(string message, Exception inner) : base(message, inner) { }
        protected DFOToolboxException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}
