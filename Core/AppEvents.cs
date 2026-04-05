using System;

namespace ESCenter.Core
{
    /// <summary>
    /// Lightweight static event bus. ViewModels publish here instead of
    /// reaching up to MainViewModel through the window hierarchy.
    /// </summary>
    public static class AppEvents
    {
        /// <summary>
        /// Raised when any ticket data changes and the dashboard should refresh.
        /// Replaces direct calls to MainViewModel.Dashboard.Refresh().
        /// </summary>
        public static event Action DashboardRefreshRequested;

        /// <summary>
        /// Raised when navigation to the Repair Tickets view is requested.
        /// Replaces direct calls to MainViewModel.ShowRepairTicketsCommand.
        /// </summary>
        public static event Action NavigateToTicketsRequested;

        public static void RequestDashboardRefresh()
            => DashboardRefreshRequested?.Invoke();

        public static void RequestNavigateToTickets()
            => NavigateToTicketsRequested?.Invoke();
    }
}
