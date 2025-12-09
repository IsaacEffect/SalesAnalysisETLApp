using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using SalesAnalysisETLApp.Domain.Interfaces.Repository;

namespace SalesAnalysisETLApp.Persistence.Destinations.Dwh
{
    public class DwhRepository : IDwhRepository
    {
        private readonly string _dwhConnection;

        public DwhRepository(IConfiguration configuration)
        {
            _dwhConnection = configuration.GetConnectionString("DwhConnection")
                ?? throw new InvalidOperationException("Connection string 'DwhConnection' no encontrada en configuración.");
        }

        private SqlConnection GetConnection() => new SqlConnection(_dwhConnection);

        public async Task<int> UpsertCategoriaAsync(string nombreCategoria)
        {
            const string sql = @"
                DECLARE @Id INT;

                SELECT @Id = IdCategoria FROM Dimension.DimCategoria WHERE NombreCategoria = @NombreCategoria;

                IF @Id IS NOT NULL
                BEGIN
                    UPDATE Dimension.DimCategoria
                    SET NombreCategoria = @NombreCategoria
                    WHERE IdCategoria = @Id;

                    SELECT @Id;
                END
                ELSE
                BEGIN
                    INSERT INTO Dimension.DimCategoria (NombreCategoria)
                    OUTPUT INSERTED.IdCategoria
                    VALUES (@NombreCategoria);
                END
                ";
            using var conn = GetConnection();
            await conn.OpenAsync();
            var id = await conn.ExecuteScalarAsync<int>(sql, new { NombreCategoria = nombreCategoria });
            return id;
        }

        public async Task<int> UpsertClienteAsync(string nombreCompleto, string? email, string? pais, string? ciudad)
        {
            const string sql = @"
                DECLARE @Id INT;

                SELECT @Id = IdCliente FROM Dimension.DimCliente 
                WHERE NombreCompleto = @NombreCompleto 
                  AND (Email = @Email OR (Email IS NULL AND @Email IS NULL));

                IF @Id IS NOT NULL
                BEGIN
                    UPDATE Dimension.DimCliente
                    SET Email = @Email,
                        Pais = @Pais,
                        Ciudad = @Ciudad
                    WHERE IdCliente = @Id;

                    SELECT @Id;
                END
                ELSE
                BEGIN
                    INSERT INTO Dimension.DimCliente (NombreCompleto, Email, Pais, Ciudad)
                    OUTPUT INSERTED.IdCliente
                    VALUES (@NombreCompleto, @Email, @Pais, @Ciudad);
                END
                ";
            using var conn = GetConnection();
            await conn.OpenAsync();
            var id = await conn.ExecuteScalarAsync<int>(sql, new { NombreCompleto = nombreCompleto, Email = email, Pais = pais, Ciudad = ciudad });
            return id;
        }

        public async Task<int> UpsertProductoAsync(string nombreProducto, int idCategoria, decimal? precioUnit)
        {
            const string sql = @"
                DECLARE @Id INT;

                SELECT @Id = IdProducto FROM Dimension.DimProducto 
                WHERE NombreProducto = @NombreProducto;

                IF @Id IS NOT NULL
                BEGIN
                    UPDATE Dimension.DimProducto
                    SET IdCategoria = @IdCategoria,
                        PrecioUnit = @PrecioUnit
                    WHERE IdProducto = @Id;

                    SELECT @Id;
                END
                ELSE
                BEGIN
                    INSERT INTO Dimension.DimProducto (NombreProducto, IdCategoria, PrecioUnit)
                    OUTPUT INSERTED.IdProducto
                    VALUES (@NombreProducto, @IdCategoria, @PrecioUnit);
                END
                ";
            using var conn = GetConnection();
            await conn.OpenAsync();
            var id = await conn.ExecuteScalarAsync<int>(sql, new { NombreProducto = nombreProducto, IdCategoria = idCategoria, PrecioUnit = precioUnit });
            return id;
        }

        public async Task<int> UpsertTiempoAsync(DateTime fecha)
        {
            const string sql = @"
                DECLARE @Id INT;

                SELECT @Id = IdTiempo FROM Dimension.DimTiempo WHERE Fecha = @Fecha;

                IF @Id IS NOT NULL
                BEGIN
                    SELECT @Id;
                END
                ELSE
                BEGIN
                    INSERT INTO Dimension.DimTiempo (Fecha, Anio, Mes, NombreMes, Trimestre)
                    OUTPUT INSERTED.IdTiempo
                    VALUES (@Fecha, DATEPART(year,@Fecha), DATEPART(month,@Fecha), DATENAME(month,@Fecha), ((DATEPART(month,@Fecha)-1)/3)+1);
                END
                ";
            using var conn = GetConnection();
            await conn.OpenAsync();
            var id = await conn.ExecuteScalarAsync<int>(sql, new { Fecha = fecha.Date });
            return id;
        }

        public async Task<int> UpsertUbicacionAsync(string? pais, string? region, string? ciudad)
        {
            const string sql = @"
                DECLARE @Id INT;

                SELECT @Id = IdUbicacion FROM Dimension.DimUbicacion 
                WHERE ISNULL(Pais,'') = ISNULL(@Pais,'') AND ISNULL(Ciudad,'') = ISNULL(@Ciudad,'');

                IF @Id IS NOT NULL
                BEGIN
                    UPDATE Dimension.DimUbicacion
                    SET Region = @Region
                    WHERE IdUbicacion = @Id;

                    SELECT @Id;
                END
                ELSE
                BEGIN
                    INSERT INTO Dimension.DimUbicacion (Pais, Region, Ciudad)
                    OUTPUT INSERTED.IdUbicacion
                    VALUES (@Pais, @Region, @Ciudad);
                END
                ";
            using var conn = GetConnection();
            await conn.OpenAsync();
            var id = await conn.ExecuteScalarAsync<int>(sql, new { Pais = pais, Region = region, Ciudad = ciudad });
            return id;
        }

        // implementaciones bulk simples (se pueden optimizar con TVP)
        public async Task<IEnumerable<int>> UpsertCategoriasAsync(IEnumerable<string> nombres)
        {
            var list = new List<int>();
            foreach (var n in nombres)
            {
                var id = await UpsertCategoriaAsync(n);
                list.Add(id);
            }
            return list;
        }

        public async Task<IEnumerable<int>> UpsertClientesAsync(IEnumerable<(string Nombre, string? Email, string? Pais, string? Ciudad)> clientes)
        {
            var list = new List<int>();
            foreach (var c in clientes)
            {
                var id = await UpsertClienteAsync(c.Nombre, c.Email, c.Pais, c.Ciudad);
                list.Add(id);
            }
            return list;
        }

        public async Task<IEnumerable<int>> UpsertProductosAsync(IEnumerable<(string Nombre, int IdCategoria, decimal? Precio)> productos)
        {
            var list = new List<int>();
            foreach (var p in productos)
            {
                var id = await UpsertProductoAsync(p.Nombre, p.IdCategoria, p.Precio);
                list.Add(id);
            }
            return list;
        }

        public async Task InsertFactVentaAsync(
            int idProducto,
            int idCliente,
            int idTiempo,
            int idUbicacion,
            int cantidad,
            decimal totalVenta
)
        {
            var sql = @"
        INSERT INTO [Fact].[FactVentas]
        (IdProducto, IdCliente, IdTiempo, IdUbicacion, Cantidad, TotalVenta)
        VALUES (@IdProducto, @IdCliente, @IdTiempo, @IdUbicacion, @Cantidad, @TotalVenta);
    ";

            using var connection = GetConnection();
            await connection.ExecuteAsync(sql, new
            {
                IdProducto = idProducto,
                IdCliente = idCliente,
                IdTiempo = idTiempo,
                IdUbicacion = idUbicacion,
                Cantidad = cantidad,
                TotalVenta = totalVenta
            });
        }

        public async Task InsertProductoNoMapeadoAsync(int productId)
        {
            const string sql = @"
                IF NOT EXISTS (SELECT 1 FROM Log.ProductosNoMapeados WHERE ProductID = @ProductID)
                INSERT INTO Log.ProductosNoMapeados (ProductID)
                VALUES (@ProductID);
            ";

            using var conn = GetConnection();
            await conn.ExecuteAsync(sql, new { ProductID = productId });
        }

        public async Task ClearAllAsync()
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            string sql = @"
                DELETE FROM Fact.FactVentas;
                DELETE FROM Dimension.DimProducto;
                DELETE FROM Dimension.DimCategoria;
                DELETE FROM Dimension.DimCliente;
                DELETE FROM Dimension.DimUbicacion;
                DELETE FROM Dimension.DimTiempo;
                DELETE FROM Log.ProductosNoMapeados;
            ";

            await conn.ExecuteAsync(sql);
        }

    }
}
