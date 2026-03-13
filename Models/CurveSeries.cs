using ESCenter.Core;

namespace ESCenter.Models
{
    public class CurveSeries : ObservableObject
    {
        private bool _isVisible = true;

        public string Key { get; set; } = string.Empty;

        public System.Windows.Media.Brush Stroke { get; set; } = System.Windows.Media.Brushes.White;

        public bool IsVisible
        {
            get => _isVisible;
            set => SetProperty(ref _isVisible, value);
        }
    }
}
