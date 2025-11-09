namespace SalesAnalysisETLApp.Domain.Entities.Dimensions
{
    public class DimProducto
    {
        public int IdProducto { get; set; }
        public string NombreProducto { get; set; } = string.Empty;
        public int IdCategoria { get; set; }
        public decimal PrecioUnit { get; set; }
    }
}
