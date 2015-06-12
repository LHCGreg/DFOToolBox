using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DFO.Common;
using DFO.Common.Images;
using DFO.Utilities;

namespace DFO.Npk
{
    public partial class NpkReader
    {
        /// <summary>
        /// Lazily loads frame metadata as needed, presenting a convenient interface.
        /// </summary>
        internal class LazyFramesReadOnlyDictionary : IReadOnlyDictionary<NpkPath, IReadOnlyList<FrameInfo>>
        {
            private NpkReader m_npk;

            public LazyFramesReadOnlyDictionary(NpkReader npk)
            {
                npk.ThrowIfNull("npk");
                m_npk = npk;
            }

            public bool ContainsKey(NpkPath key)
            {
                return m_npk.Images.ContainsKey(key);
            }

            public IEnumerable<NpkPath> Keys
            {
                get { return m_npk.Images.Keys; }
            }

            public bool TryGetValue(NpkPath key, out IReadOnlyList<FrameInfo> value)
            {
                if (m_npk.Images.ContainsKey(key))
                {
                    m_npk.PreLoadSpriteMetadata(key);
                    value = m_npk.m_frames[key];
                    return true;
                }
                else
                {
                    value = null;
                    return false;
                }
            }

            // The debugger does not seem to honor these...so frame metadata will get preloaded if you are debugging
            // and the debugger decides to evaluate this property
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            [DebuggerHidden]
            public IEnumerable<IReadOnlyList<FrameInfo>> Values
            {
                get
                {
                    m_npk.PreLoadAllSpriteFrameMetadata();
                    return m_npk.m_frames.Values;
                }
            }

            public IReadOnlyList<FrameInfo> this[NpkPath key]
            {
                get
                {
                    try
                    {
                        m_npk.PreLoadSpriteMetadata(key);
                    }
                    catch (FileNotFoundException ex)
                    {
                        throw new KeyNotFoundException(ex.Message, ex);
                    }

                    return m_npk.m_frames[key];
                }
            }

            public int Count { get { return m_npk.Images.Count; } }

            public IEnumerator<KeyValuePair<NpkPath, IReadOnlyList<FrameInfo>>> GetEnumerator()
            {
                foreach (NpkPath imgPath in m_npk.Images.Keys)
                {
                    m_npk.PreLoadSpriteMetadata(imgPath);
                    yield return new KeyValuePair<NpkPath, IReadOnlyList<FrameInfo>>(imgPath, m_npk.m_frames[imgPath]);
                }
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }
}
