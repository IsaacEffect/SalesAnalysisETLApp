using Microsoft.EntityFrameworkCore;
using SalesAnalysisETLApp.Api.Data.Context;
using SalesAnalysisETLApp.Api.Data.Entities;
using SalesAnalysisETLApp.Api.Data.Interfaces;

namespace SalesAnalysisETLApp.Api.Data.Repositories
{
    public class ProductoRepository : IProductoRepository
    {
        private readonly ExternalContext _context;

        public ProductoRepository(ExternalContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Producto>> GetAllProductosAsync()
        {
            return await _context.Productos.ToListAsync();
        }
    }
}
