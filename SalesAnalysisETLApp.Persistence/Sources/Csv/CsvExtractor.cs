using CsvHelper;
using SalesAnalysisETLApp.Application.Contracts;
using System.Globalization;

namespace SalesAnalysisETLApp.Persistence.Sources.Csv
{
    public class CsvExtractor<T> : IExtractor<T>
    {
        private readonly string _filePath;

        public CsvExtractor(string filePath)
        {
            _filePath = filePath;
        }

        public async Task<IEnumerable<T>> ExtractAsync()
        {
            using var reader = new StreamReader(_filePath);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            var records = csv.GetRecords<T>().ToList();
            return await Task.FromResult(records);
        }
    }
}
