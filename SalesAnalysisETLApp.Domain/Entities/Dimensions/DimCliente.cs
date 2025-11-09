namespace SalesAnalysisETLApp.Domain.Entities.Dimensions
{
    public class DimCliente
    {
        public int IdCliente { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Pais { get; set; }
        public string? Ciudad { get; set; }
    }
}
