namespace SalesAnalysisETLApp.Application.Services.Transform.TransformModels
{
    public class TSale
    {
        public int ProductID { get; set; }
        public int CustomerID { get; set; }
        public DateTime Fecha { get; set; }
        public int Cantidad { get; set; }
        public decimal TotalVenta { get; set; }
        public string? Pais { get; set; }
        public string? Ciudad { get; set; }
    }

}
