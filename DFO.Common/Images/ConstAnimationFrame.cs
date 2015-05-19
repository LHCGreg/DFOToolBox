using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DFO.Utilities;

namespace DFO.Common.Images
{
    public class ConstAnimationFrame
    {
        private AnimationFrame m_mutable;

        public ImageIdentifier Image { get { return m_mutable.Image; } }
        public int PositionX { get { return m_mutable.PositionX; } }
        public int PositionY { get { return m_mutable.PositionY; } }
        public decimal ImageRateX { get { return m_mutable.ImageRateX; } }
        public decimal ImageRateY { get { return m_mutable.ImageRateY; } }
        public decimal ImageRotate { get { return m_mutable.ImageRotate; } }
        public int Red { get { return m_mutable.Red; } }
        public int Green { get { return m_mutable.Green; } }
        public int Blue { get { return m_mutable.Blue; } }
        public int Alpha { get { return m_mutable.Alpha; } }
        public bool Interpolation { get { return m_mutable.Interpolation; } }
        public string GraphicEffect { get { return m_mutable.GraphicEffect; } }
        public int DelayInMs { get { return m_mutable.DelayInMs; } }
        // TODO: DamageType - don't know possible values at this time
        // TODO: DamageBox - don't know structure of it at this time
        // TODO: Find out what other settings are possible

        public ConstAnimationFrame(AnimationFrame mutable)
        {
            m_mutable = mutable;
        }
    }
}
