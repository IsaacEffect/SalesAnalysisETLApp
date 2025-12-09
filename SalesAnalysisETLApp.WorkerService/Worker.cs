using SalesAnalysisETLApp.Application.Services.Transform;
using SalesAnalysisETLApp.Domain.Interfaces.Repository;
using SalesAnalysisETLApp.Domain.RawModels;
using SalesAnalysisETLApp.Persistence.Sources.Api;
using SalesAnalysisETLApp.Persistence.Sources.BD;
using SalesAnalysisETLApp.Persistence.Sources.Csv;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IDwhRepository _dwhRepo;

    public Worker(
        ILogger<Worker> logger,
        IConfiguration config,
        IHttpClientFactory httpClientFactory,
        IDwhRepository dwhRepo)
    {
        _logger = logger;
        _config = config;
        _httpClientFactory = httpClientFactory;
        _dwhRepo = dwhRepo;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Iniciando proceso de extracción...");

        try
        {
            // ========================= EXTRACT ============================
            var products = await new CsvExtractor<RawProduct>(GetRequiredConfig("FilePaths:ProductsCsv")).ExtractAsync();
            var customers = await new CsvExtractor<RawCustomer>(GetRequiredConfig("FilePaths:CustomersCsv")).ExtractAsync();
            var orders = await new CsvExtractor<RawOrder>(GetRequiredConfig("FilePaths:OrdersCsv")).ExtractAsync();
            var orderDetails = await new CsvExtractor<RawOrderDetail>(GetRequiredConfig("FilePaths:OrderDetailsCsv")).ExtractAsync();

            _logger.LogInformation($"Productos CSV: {products.Count()}");
            _logger.LogInformation($"Clientes CSV: {customers.Count()}");
            _logger.LogInformation($"Órdenes CSV: {orders.Count()}");
            _logger.LogInformation($"Detalles CSV: {orderDetails.Count()}");

            var httpClient = _httpClientFactory.CreateClient("SalesApiClient");
            var baseUrl = GetRequiredConfig("ApiSettings:BaseUrl");

            var apiClientes = await new ApiExtractor<RawCustomer>(httpClient, $"{baseUrl}{GetRequiredConfig("ApiSettings:ClientesEndpoint")}").ExtractAsync();
            var apiProductos = await new ApiExtractor<RawProduct>(httpClient, $"{baseUrl}{GetRequiredConfig("ApiSettings:ProductosEndpoint")}").ExtractAsync();

            _logger.LogInformation($"Clientes API: {apiClientes.Count()}");
            _logger.LogInformation($"Productos API: {apiProductos.Count()}");

            var historicalSales = await new DatabaseExtractor<RawHistoricalSale>(
                GetRequiredConfig("ConnectionStrings:ExternalDB"),
                GetRequiredConfig("Queries:HistoricalSales")
            ).ExtractAsync();

            _logger.LogInformation($"Ventas históricas BD externa: {historicalSales.Count()}");
            _logger.LogInformation("Extracción completada.");

            // ========================= TRANSFORM ============================
            _logger.LogInformation("Iniciando transformación...");

            var transformer = new TransformService();

            var cleanProducts = transformer.TransformProducts(products, apiProductos);
            var cleanCustomers = transformer.TransformCustomers(customers, apiClientes);
            var cleanSales = transformer.TransformSales(orders, orderDetails, historicalSales);

            _logger.LogInformation($"Productos limpios: {cleanProducts.Count()}");
            _logger.LogInformation($"Clientes limpios: {cleanCustomers.Count()}");
            _logger.LogInformation($"Ventas limpias: {cleanSales.Count()}");

            _logger.LogInformation("Transformación completada.");

            // ========================= LOAD ============================
            _logger.LogInformation("Iniciando carga en DW...");

            await _dwhRepo.ClearAllAsync();
            _logger.LogInformation("DW limpiado.");

            // ========== CATEGORIA ==========
            var categoriaKeyMap = cleanProducts
                .Select(p => p.Categoria)
                .Distinct()
                .ToDictionary(
                    c => c,
                    c => _dwhRepo.UpsertCategoriaAsync(c).Result
                );

            // ========== PRODUCTO ==========
            var productoKeyMap = new Dictionary<(string Nombre, string Categoria), int>();
            foreach (var p in cleanProducts)
            {
                var key = await _dwhRepo.UpsertProductoAsync(
                    p.NombreProducto,
                    categoriaKeyMap[p.Categoria],
                    p.PrecioUnit
                );
                productoKeyMap[(p.NombreProducto, p.Categoria)] = key;
            }

            // ========== CLIENTE ==========
            var clienteKeyMap = new Dictionary<(string Nombre, string? Email), int>();
            foreach (var c in cleanCustomers)
            {
                clienteKeyMap[(c.NombreCompleto, c.Email)] =
                    await _dwhRepo.UpsertClienteAsync(c.NombreCompleto, c.Email, c.Pais, c.Ciudad);
            }

            // ========== UBICACION ==========
            var ubicacionKeyMap = cleanCustomers
                .Select(c => (c.Pais, c.Ciudad))
                .Distinct()
                .ToDictionary(
                    x => x,
                    x => _dwhRepo.UpsertUbicacionAsync(x.Pais, "N/A", x.Ciudad).Result
                );

            // ========== TIEMPO ==========
            var tiempoKeyMap = cleanSales
                .Select(s => s.Fecha.Date)
                .Distinct()
                .ToDictionary(
                    f => f,
                    f => _dwhRepo.UpsertTiempoAsync(f).Result
                );

            _logger.LogInformation("Dimensiones cargadas.");

            // ======================= MAPEO ORIGINAL -> DIM =======================

            var productoIdToDim = cleanProducts.ToDictionary(
                p => p.IdOriginal,
                p => productoKeyMap[(p.NombreProducto, p.Categoria)]
            );

            var clienteIdToDim = cleanCustomers.ToDictionary(
                c => c.IdOriginal,
                c => clienteKeyMap[(c.NombreCompleto, c.Email)]
            );

            var clienteIdToLocation = cleanCustomers.ToDictionary(
                c => c.IdOriginal,
                c => (c.Pais, c.Ciudad)
            );

            // ======================= FACT VENTAS =======================

            _logger.LogInformation("Iniciando carga de FactVentas...");

            int registrosFact = 0;

            var productosNoMapeados = new HashSet<int>();
            var clientesNoMapeados = new HashSet<int>();

            foreach (var s in cleanSales)
            {
                // PRODUCTO
                // PRODUCTOS
                if (!productoIdToDim.TryGetValue(s.ProductID, out var idProd))
                {
                    if (productosNoMapeados.Add(s.ProductID))
                    {
                        // Guardar en BD para auditoría
                        await _dwhRepo.InsertProductoNoMapeadoAsync(s.ProductID);
                    }
                    continue;
                }

                // CLIENTE
                if (!clienteIdToDim.TryGetValue(s.CustomerID, out var idCli))
                {
                    if (clientesNoMapeados.Add(s.CustomerID))
                        _logger.LogWarning($"Cliente no mapeado: {s.CustomerID}");
                    continue;
                }

                var (pais, ciudad) = clienteIdToLocation[s.CustomerID];

                if (!ubicacionKeyMap.TryGetValue((pais, ciudad), out var idUbi))
                {
                    idUbi = await _dwhRepo.UpsertUbicacionAsync(pais, "N/A", ciudad);
                    ubicacionKeyMap[(pais, ciudad)] = idUbi;
                }

                var idTiempo = tiempoKeyMap[s.Fecha.Date];

                await _dwhRepo.InsertFactVentaAsync(
                    idProducto: idProd,
                    idCliente: idCli,
                    idTiempo: idTiempo,
                    idUbicacion: idUbi,
                    cantidad: s.Cantidad,
                    totalVenta: s.TotalVenta
                );

                registrosFact++;
            }

            // ======================= RESUMEN =======================

            if (productosNoMapeados.Count > 0)
                _logger.LogWarning($"Productos NO mapeados: {productosNoMapeados.Count} | Ejemplos: {string.Join(", ", productosNoMapeados.Take(20))}");

            if (clientesNoMapeados.Count > 0)
                _logger.LogWarning($"Clientes NO mapeados: {clientesNoMapeados.Count} | Ejemplos: {string.Join(", ", clientesNoMapeados.Take(20))}");

            _logger.LogInformation($"FactVentas cargada: {registrosFact} registros.");
            _logger.LogInformation("ETL COMPLETADO.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error durante el proceso ETL.");
        }
    }

    private string GetRequiredConfig(string key)
    {
        var value = _config[key];
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"Configuración requerida no encontrada: '{key}'.");

        return value;
    }
}
