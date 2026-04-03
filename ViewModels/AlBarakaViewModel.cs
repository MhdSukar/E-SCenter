using ESCenter.Services;

namespace ESCenter.ViewModels;

public class AlBarakaViewModel : PnLDashboardViewModel
{
    public AlBarakaViewModel() : base(new FinancialService())
    {
    }

    public AlBarakaViewModel(IFinancialService financialService) : base(financialService)
    {
    }
}
