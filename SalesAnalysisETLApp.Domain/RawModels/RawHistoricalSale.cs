namespace SalesAnalysisETLApp.Domain.RawModels
{
    public class RawHistoricalSale
    {
        public int OrderID { get; set; }
        public int ProductID { get; set; }
        public int CustomerID { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice { get; set; }
        public DateTime OrderDate { get; set; }
    }
}
