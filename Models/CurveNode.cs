using System.Windows;
using ESCenter.Core;

namespace ESCenter.Models
{
    public class CurveNode : ObservableObject
    {
        private Point _position;
        public Point Position
        {
            get => _position;
            set => SetProperty(ref _position, value);
        }

        private Point _handleIn;
        public Point HandleIn
        {
            get => _handleIn;
            set => SetProperty(ref _handleIn, value);
        }

        private Point _handleOut;
        public Point HandleOut
        {
            get => _handleOut;
            set => SetProperty(ref _handleOut, value);
        }

        public CurveNode(Point position)
        {
            _position = position;
            _handleIn = new Point(position.X - 0.06, position.Y);
            _handleOut = new Point(position.X + 0.06, position.Y);
        }
    }
}
