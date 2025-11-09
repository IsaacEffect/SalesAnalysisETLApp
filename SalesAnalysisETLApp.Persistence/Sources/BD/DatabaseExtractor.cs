using Microsoft.Data.SqlClient;
using Dapper;
using SalesAnalysisETLApp.Application.Contracts;

namespace SalesAnalysisETLApp.Persistence.Sources.BD
{
    public class DatabaseExtractor<T> : IExtractor<T>
    {
        private readonly string _connectionString;
        private readonly string _query;

        public DatabaseExtractor(string connectionString, string query)
        {
            _connectionString = connectionString;
            _query = query;
        }

        public async Task<IEnumerable<T>> ExtractAsync()
        {
            using var connection = new SqlConnection(_connectionString);
            return await connection.QueryAsync<T>(_query);
        }
    }
}
