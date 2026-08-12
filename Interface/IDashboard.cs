using System.Threading.Tasks;
using static TERMS_LOYALTY_API.DTOs.shelf.DashboardDto;

namespace TERMS_LOYALTY_API.Interface
{
    public interface IDashboard
    {
        Task<DashboardDataDto> GetDashboardDataAsync(int? userId = null);

    }
}
