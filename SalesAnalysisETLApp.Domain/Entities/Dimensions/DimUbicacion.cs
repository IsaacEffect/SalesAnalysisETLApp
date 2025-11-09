namespace SalesAnalysisETLApp.Domain.Entities.Dimensions
{
    public class DimUbicacion
    {
        public int IdUbicacion { get; set; }
        public string? Pais { get; set; }
        public string? Region { get; set; }
        public string? Ciudad { get; set; }
    }
}
