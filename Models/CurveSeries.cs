using System.Collections.ObjectModel;
using System.Windows.Media;
using ESCenter.Core;

namespace ESCenter.Models
{
    public class CurveSeries : ObservableObject
    {
        private bool _isVisible = true;
        public string Key { get; set; } = string.Empty;
        public Brush Stroke { get; set; } = Brushes.White;
        public ObservableCollection<CurveNode> Nodes { get; set; } = new();

        public bool IsVisible
        {
            get => _isVisible;
            set => SetProperty(ref _isVisible, value);
        }
    }
}
