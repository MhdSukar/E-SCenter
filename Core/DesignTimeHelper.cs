using System.ComponentModel;
using System.Windows;

namespace ESCenter.Core
{
    internal static class DesignTimeHelper
    {
        private static readonly DependencyObject Probe = new();

        internal static bool IsInDesignMode =>
            DesignerProperties.GetIsInDesignMode(Probe);
    }
}
