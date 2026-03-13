using ESCenter.Core;

namespace ESCenter.Models
{
    public class CurveNode : ObservableObject
    {
        private System.Windows.Point _position;
        public System.Windows.Point Position
        {
            get => _position;
            set => SetProperty(ref _position, value);
        }

        private System.Windows.Point _handleIn;
        public System.Windows.Point HandleIn
        {
            get => _handleIn;
            set => SetProperty(ref _handleIn, value);
        }

        private System.Windows.Point _handleOut;
        public System.Windows.Point HandleOut
        {
            get => _handleOut;
            set => SetProperty(ref _handleOut, value);
        }

        public CurveNode(System.Windows.Point position)
        {
            _position = position;
            _handleIn = new System.Windows.Point(position.X - 0.06, position.Y);
            _handleOut = new System.Windows.Point(position.X + 0.06, position.Y);
        }
    }
}
