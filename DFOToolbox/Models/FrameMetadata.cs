using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using DFO.Common.Images;

namespace DFOToolbox.Models
{
    public class FrameMetadata : NotifyPropertyChangedBase, ISelectable
    {
        private int _index;
        public int Index
        {
            get { return _index; }
            set { _index = value; OnPropertyChanged(); }
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get { return _isSelected; }
            set { _isSelected = value; OnPropertyChanged(); }
        }

        public const string PropertyNameIsSelected = "IsSelected";
        
        private int _width;
        public int Width
        {
            get { return _width; }
            set { _width = value; OnPropertyChanged(); }
        }

        private int _height;
        public int Height
        {
            get { return _height; }
            set { _height = value; OnPropertyChanged(); }
        }

        private int _x;
        public int X
        {
            get { return _x; }
            set { _x = value; OnPropertyChanged(); }
        }

        private int _y;
        public int Y
        {
            get { return _y; }
            set { _y = value; OnPropertyChanged(); }
        }

        // If this is set, the frame is a link frame. The other properties will be set to the values of the linked frame.
        private int? _linkFrameIndex;
        public int? LinkFrameIndex
        {
            get { return _linkFrameIndex; }
            set { _linkFrameIndex = value; OnPropertyChanged(); }
        }

        public FrameMetadata()
        {

        }

        public FrameMetadata(int index, int width, int height, int x, int y, int? linkFrameIndex)
        {
            _index = index;
            _width = width;
            _height = height;
            _x = x;
            _y = y;
            _linkFrameIndex = linkFrameIndex;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="frame">If the frame being constructed is a link frame, this parameter should be the linked frame.</param>
        /// <param name="index"></param>
        /// <param name="linkFrameIndex"></param>
        public FrameMetadata(FrameInfo frame, int index, int? linkFrameIndex)
        {
            _index = index;
            _width = frame.Width;
            _height = frame.Height;
            _x = frame.LocationX;
            _y = frame.LocationY;
            _linkFrameIndex = linkFrameIndex;
        }
    }
}
