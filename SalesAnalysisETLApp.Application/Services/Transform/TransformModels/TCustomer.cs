namespace SalesAnalysisETLApp.Application.Services.Transform.TransformModels
{
    public class TCustomer
    {
        public int IdOriginal { get; set; }
        public string NombreCompleto { get; set; } = "";
        public string? Email { get; set; }
        public string? Pais { get; set; }
        public string? Ciudad { get; set; }
    }

}
