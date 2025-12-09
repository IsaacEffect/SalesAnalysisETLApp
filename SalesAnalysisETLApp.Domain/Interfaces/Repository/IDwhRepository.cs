namespace SalesAnalysisETLApp.Domain.Interfaces.Repository
{
    public interface IDwhRepository
    {
        Task<int> UpsertCategoriaAsync(string nombreCategoria);
        Task<int> UpsertClienteAsync(string nombreCompleto, string? email, string? pais, string? ciudad);
        Task<int> UpsertProductoAsync(string nombreProducto, int idCategoria, decimal? precioUnit);
        Task<int> UpsertTiempoAsync(DateTime fecha);
        Task<int> UpsertUbicacionAsync(string? pais, string? region, string? ciudad);

        Task InsertFactVentaAsync(
            int idProducto,
            int idCliente,
            int idTiempo,
            int idUbicacion,
            int cantidad,
            decimal totalVenta
        );

        Task InsertProductoNoMapeadoAsync(int productId);

        // métodos bulk
        Task<IEnumerable<int>> UpsertCategoriasAsync(IEnumerable<string> nombres);
        Task<IEnumerable<int>> UpsertClientesAsync(IEnumerable<(string Nombre, string? Email, string? Pais, string? Ciudad)> clientes);
        Task<IEnumerable<int>> UpsertProductosAsync(IEnumerable<(string Nombre, int IdCategoria, decimal? Precio)> productos);

        // limpieza de datos
        Task ClearAllAsync();
    }
}
