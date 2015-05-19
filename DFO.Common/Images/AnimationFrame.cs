using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DFO.Utilities;

namespace DFO.Common.Images
{
    public class AnimationFrame : IConstable<ConstAnimationFrame>
    {
        public ImageIdentifier Image { get; set; }
        public int PositionX { get; set; }
        public int PositionY { get; set; }

        private decimal m_imageRateX = 1.000000m;
        public decimal ImageRateX { get { return m_imageRateX; } set { m_imageRateX = value; } }

        private decimal m_imageRateY = 1.000000m;
        public decimal ImageRateY { get { return m_imageRateY; } set { m_imageRateY = value; } }

        private decimal m_imageRotate = 0m;
        public decimal ImageRotate { get { return m_imageRotate; } set { m_imageRotate = value; } }

        private int m_red = 255;
        public int Red { get { return m_red; } set { m_red = value; } }

        private int m_green = 255;
        public int Green { get { return m_green; } set { m_green = value; } }

        private int m_blue = 255;
        public int Blue { get { return m_blue; } set { m_blue = value; } }

        private int m_alpha = 255;
        public int Alpha { get { return m_alpha; } set { m_alpha = value; } }

        private bool m_interpolation = false;
        public bool Interpolation { get { return m_interpolation; } set { m_interpolation = value; } }

        private string m_graphicEffect = "NONE";
        public string GraphicEffect { get { return m_graphicEffect; } set { m_graphicEffect = value; } }

        private int m_delayInMs;
        public int DelayInMs
        {
            get { return m_delayInMs; }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("DelayInMs", string.Format("Delay in ms cannot be {0}, it must not be negative.", value));
                }
                m_delayInMs = value;
            }
        }

        public AnimationFrame()
        {
            ;
        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="other"></param>
        public AnimationFrame(ConstAnimationFrame other)
        {
            this.Alpha = other.Alpha;
            this.Blue = other.Blue;
            this.DelayInMs = other.DelayInMs;
            this.GraphicEffect = other.GraphicEffect;
            this.Green = other.Green;
            this.Image = other.Image;
            this.PositionX = other.PositionX;
            this.PositionY = other.PositionY;
            this.ImageRateX = other.ImageRateX;
            this.ImageRateY = other.ImageRateY;
            this.ImageRotate = other.ImageRotate;
            this.Interpolation = other.Interpolation;
            this.Red = other.Red;
        }

        public ConstAnimationFrame AsConst()
        {
            return new ConstAnimationFrame(this);
        }
    }
}
