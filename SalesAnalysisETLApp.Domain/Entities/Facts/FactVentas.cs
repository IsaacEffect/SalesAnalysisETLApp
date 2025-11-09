namespace SalesAnalysisETLApp.Domain.Entities.Facts
{
    public class FactVentas
    {
        public int IdVenta { get; set; }

        // Claves Foraneas
        public int IdProducto { get; set; }
        public int IdCliente { get; set; }
        public int IdTiempo { get; set; }
        public int IdUbicacion { get; set; } 

        // Medidas
        public int Cantidad { get; set; }
        public decimal TotalVenta { get; set; }
    }
}
