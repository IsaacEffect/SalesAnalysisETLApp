using Microsoft.AspNetCore.Mvc;
using SalesAnalysisETLApp.Api.Data.Interfaces;

namespace SalesAnalysisETLApp.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClientesController : ControllerBase
    {
        private readonly IClienteRepository _clienteRepo;

        public ClientesController(IClienteRepository clienteRepo)
        {
            _clienteRepo = clienteRepo;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllClientes()
        {
            var clientes = await _clienteRepo.GetAllClientesAsync();
            return Ok(clientes);
        }
    }
}
