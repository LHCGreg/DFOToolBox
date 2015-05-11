using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;

namespace DFO.Common.Images
{
	public class ConstRawAnimation
	{
		private RawAnimation m_mutable;

		public bool Loop { get { return m_mutable.Loop; } }
		public bool Shadow { get { return m_mutable.Shadow; } }
		public ReadOnlyCollection<ConstAnimationFrame> Frames { get { return new ReadOnlyCollection<ConstAnimationFrame>( m_mutable.Frames ); } }

		public ConstRawAnimation( RawAnimation mutable )
		{
			m_mutable = mutable;
		}
	}
}
