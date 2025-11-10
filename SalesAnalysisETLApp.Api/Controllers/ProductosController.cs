using Microsoft.AspNetCore.Mvc;
using SalesAnalysisETLApp.Api.Data.Interfaces;

namespace SalesAnalysisETLApp.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductosController : ControllerBase
    {
        private readonly IProductoRepository _productoRepo;

        public ProductosController(IProductoRepository productoRepo)
        {
            _productoRepo = productoRepo;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllProductos()
        {
            var productos = await _productoRepo.GetAllProductosAsync();
            return Ok(productos);
        }
    }
}
