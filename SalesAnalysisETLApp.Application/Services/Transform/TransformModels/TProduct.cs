namespace SalesAnalysisETLApp.Application.Services.Transform.TransformModels
{
    public class TProduct
    {
        public string NombreProducto { get; set; } = "";
        public string Categoria { get; set; } = "";
        public decimal PrecioUnit { get; set; }
    }
}