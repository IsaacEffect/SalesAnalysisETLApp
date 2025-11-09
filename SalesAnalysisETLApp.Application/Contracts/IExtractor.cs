namespace SalesAnalysisETLApp.Application.Contracts
{
    public interface IExtractor<T>
    {
        Task<IEnumerable<T>> ExtractAsync();
    }
}
