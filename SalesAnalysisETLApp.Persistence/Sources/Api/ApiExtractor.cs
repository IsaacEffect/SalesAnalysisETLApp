using System.Net.Http.Json;
using SalesAnalysisETLApp.Application.Contracts;

namespace SalesAnalysisETLApp.Persistence.Sources.Api
{
    public class ApiExtractor<T> : IExtractor<T>
    {
        private readonly HttpClient _client;
        private readonly string _endpoint;

        public ApiExtractor(HttpClient client, string endpoint)
        {
            _client = client;
            _endpoint = endpoint;
        }

        public async Task<IEnumerable<T>> ExtractAsync()
        {
            var data = await _client.GetFromJsonAsync<List<T>>(_endpoint);
            return data ?? new List<T>();
        }
    }
}
