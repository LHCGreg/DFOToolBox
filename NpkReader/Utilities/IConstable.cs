using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DFO.Utilities
{
    public interface IConstable<TConst>
    {
        TConst AsConst();
    }
}
