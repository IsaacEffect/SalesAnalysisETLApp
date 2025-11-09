namespace SalesAnalysisETLApp.Domain.RawModels
{
    public class RawOrderDetail
    {
        public int OrderID { get; set; }
        public int ProductID { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice { get; set; }
    }
}
