using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DFO.Utilities;

namespace DFO.Common.Images
{
	public class RawAnimation : IConstable<ConstRawAnimation>
	{
		private bool m_loop = false;
		public bool Loop { get { return m_loop; } set { m_loop = value; } }
		
		private bool m_shadow = false;
		public bool Shadow { get { return m_shadow; } set { m_shadow = value; } }

		private IList<ConstAnimationFrame> m_frames = new List<ConstAnimationFrame>();
		public IList<ConstAnimationFrame> Frames { get { return m_frames; } set { m_frames = value; } }

		public RawAnimation()
		{
			;
		}

		public ConstRawAnimation AsConst()
		{
			return new ConstRawAnimation( this );
		}
	}
}
