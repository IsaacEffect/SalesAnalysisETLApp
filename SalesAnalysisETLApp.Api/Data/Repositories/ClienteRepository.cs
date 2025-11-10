using Microsoft.EntityFrameworkCore;
using SalesAnalysisETLApp.Api.Data.Context;
using SalesAnalysisETLApp.Api.Data.Entities;
using SalesAnalysisETLApp.Api.Data.Interfaces;

namespace SalesAnalysisETLApp.Api.Data.Repositories
{
    public class ClienteRepository : IClienteRepository
    {
        private readonly ExternalContext _context;

        public ClienteRepository(ExternalContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Cliente>> GetAllClientesAsync()
        {
            return await _context.Clientes.ToListAsync();
        }
    }
}
