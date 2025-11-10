using SalesAnalysisETLApp.Api.Data.Entities;

namespace SalesAnalysisETLApp.Api.Data.Interfaces
{
    public interface IProductoRepository
    {
        Task<IEnumerable<Producto>> GetAllProductosAsync();
    }
}
