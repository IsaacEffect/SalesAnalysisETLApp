using SalesAnalysisETLApp.Application.Services.Transform.TransformModels;
using SalesAnalysisETLApp.Domain.RawModels;
using System.Globalization;

namespace SalesAnalysisETLApp.Application.Services.Transform
{
    public class TransformService
    {
        // 1. Limpiar duplicados de listas simples
        private IEnumerable<T> RemoveDuplicates<T>(IEnumerable<T> list)
        {
            return list.Distinct();
        }

        // 2. Normalizar nombres (mayúsculas iniciales)
        private string NormalizeString(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "";
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(value.Trim().ToLower());
        }

        // 3. Normalizar fechas (asegurar formato correcto)
        private DateTime NormalizeDate(DateTime date)
        {
            return date.Date;
        }

        // =============================
        // TRANSFORM PRODUCTOS
        // =============================
        public IEnumerable<TProduct> TransformProducts(IEnumerable<RawProduct> raw, IEnumerable<RawProduct> apiRaw)
        {
            var merged = raw.Concat(apiRaw);

            var cleaned = merged
                .Where(p => !string.IsNullOrWhiteSpace(p.ProductName))
                .Select(p => new TProduct
                {
                    NombreProducto = NormalizeString(p.ProductName),
                    Categoria = NormalizeString(p.Category),
                    PrecioUnit = p.Price
                })
                .GroupBy(p => p.NombreProducto)
                .Select(g => g.First());

            return cleaned;
        }

        // =============================
        // TRANSFORM CLIENTES
        // =============================
        public IEnumerable<TCustomer> TransformCustomers(IEnumerable<RawCustomer> raw, IEnumerable<RawCustomer> apiRaw)
        {
            var merged = raw.Concat(apiRaw);

            var cleaned = merged
                .Where(c => !string.IsNullOrWhiteSpace(c.FirstName) || !string.IsNullOrWhiteSpace(c.LastName))
                .Select(c =>
                {
                    var nombreCompletoRaw = string.Join(" ", new[] { c.FirstName, c.LastName }.Where(s => !string.IsNullOrWhiteSpace(s)));

                    return new TCustomer
                    {
                        NombreCompleto = NormalizeString(nombreCompletoRaw),
                        Email = c.Email?.ToLower(),
                        Pais = NormalizeString(c.Country),
                        Ciudad = NormalizeString(c.City)
                    };
                })

                .GroupBy(c => c.NombreCompleto)
                .Select(g => g.First());

            return cleaned;
        }

        // =============================
        // TRANSFORM VENTAS
        // =============================
        public IEnumerable<TSale> TransformSales(
            IEnumerable<RawOrder> orders,
            IEnumerable<RawOrderDetail> details,
            IEnumerable<RawHistoricalSale> historical)
        {
            // 1. Unir Orders + Details (venta regular)
            var joined = from o in orders
                         join d in details on o.OrderID equals d.OrderID
                         select new TSale
                         {
                             ProductID = d.ProductID,
                             CustomerID = o.CustomerID,
                             Cantidad = d.Quantity,
                             Fecha = NormalizeDate(o.OrderDate),
                             TotalVenta = d.Quantity * d.TotalPrice,
                             Pais = null,
                             Ciudad = null
                         };

            // 2. Agregar ventas históricas
            var historicalMapped = historical.Select(h => new TSale
            {
                ProductID = h.ProductID,
                CustomerID = h.CustomerID,
                Cantidad = h.Quantity,
                Fecha = NormalizeDate(h.OrderDate),
                TotalVenta = h.TotalPrice,
                Pais = null,
                Ciudad = null
            });

            // 3. Unión + limpieza
            return joined.Concat(historicalMapped)
                         .Where(s => s.Cantidad > 0 && s.TotalVenta >= 0);
        }
    }
}
