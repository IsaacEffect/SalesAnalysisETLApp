namespace SalesAnalysisETLApp.Application.Services.Transform.TransformModels
{
    public class TProduct
    {
        public int IdOriginal { get; set; }
        public string NombreProducto { get; set; } = "";
        public string Categoria { get; set; } = "";
        public decimal PrecioUnit { get; set; }
    }
}