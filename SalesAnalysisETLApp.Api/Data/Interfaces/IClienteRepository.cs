using SalesAnalysisETLApp.Api.Data.Entities;

namespace SalesAnalysisETLApp.Api.Data.Interfaces
{
    public interface IClienteRepository
    {
        Task<IEnumerable<Cliente>> GetAllClientesAsync();
    }
}
