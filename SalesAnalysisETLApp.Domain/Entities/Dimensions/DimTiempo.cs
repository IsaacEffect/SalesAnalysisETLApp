namespace SalesAnalysisETLApp.Domain.Entities.Dimensions
{
    public class DimTiempo
    {
        public int IdTiempo { get; set; }
        public DateTime Fecha { get; set; }
        public int Anio { get; set; }
        public int Mes { get; set; }
        public string? NombreMes { get; set; }
        public int Trimestre { get; set; }
    }
}
