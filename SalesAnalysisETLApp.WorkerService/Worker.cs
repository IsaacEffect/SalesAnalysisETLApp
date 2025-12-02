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

    public Worker(ILogger<Worker> logger, IConfiguration config, IHttpClientFactory httpClientFactory, IDwhRepository dwhRepo)
    {
        _logger = logger;
        _config = config;
        _httpClientFactory = httpClientFactory;
        _dwhRepo = dwhRepo;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // ========================= EXTRACT ============================
        _logger.LogInformation("Iniciando proceso de extracción...");

        try
        {
            // EXTRAER DESDE CSV
            var productsPath = GetRequiredConfig("FilePaths:ProductsCsv");
            var customersPath = GetRequiredConfig("FilePaths:CustomersCsv");
            var ordersPath = GetRequiredConfig("FilePaths:OrdersCsv");
            var orderDetailsPath = GetRequiredConfig("FilePaths:OrderDetailsCsv");

            var productsExtractor = new CsvExtractor<RawProduct>(productsPath);
            var customersExtractor = new CsvExtractor<RawCustomer>(customersPath);
            var ordersExtractor = new CsvExtractor<RawOrder>(ordersPath);
            var orderDetailsExtractor = new CsvExtractor<RawOrderDetail>(orderDetailsPath);

            var products = await productsExtractor.ExtractAsync();
            var customers = await customersExtractor.ExtractAsync();
            var orders = await ordersExtractor.ExtractAsync();
            var orderDetails = await orderDetailsExtractor.ExtractAsync();

            _logger.LogInformation($"Productos extraídos (CSV): {products.Count()}");
            _logger.LogInformation($"Clientes extraídos (CSV): {customers.Count()}");
            _logger.LogInformation($"Órdenes extraídas (CSV): {orders.Count()}");
            _logger.LogInformation($"Detalles de órdenes extraídos (CSV): {orderDetails.Count()}");

            // EXTRAER DESDE API
            _logger.LogInformation("Extrayendo datos actualizados desde API...");

            var httpClient = _httpClientFactory.CreateClient("SalesApiClient");

            var baseUrl = GetRequiredConfig("ApiSettings:BaseUrl");
            var clientesEndpoint = GetRequiredConfig("ApiSettings:ClientesEndpoint");
            var productosEndpoint = GetRequiredConfig("ApiSettings:ProductosEndpoint");

            var apiClientesExtractor = new ApiExtractor<RawCustomer>(httpClient, $"{baseUrl}{clientesEndpoint}");
            var apiProductosExtractor = new ApiExtractor<RawProduct>(httpClient, $"{baseUrl}{productosEndpoint}");

            var apiClientes = await apiClientesExtractor.ExtractAsync();
            var apiProductos = await apiProductosExtractor.ExtractAsync();

            _logger.LogInformation($"Clientes extraídos (API): {apiClientes.Count()}");
            _logger.LogInformation($"Productos extraídos (API): {apiProductos.Count()}");

            // EXTRAER DESDE BD EXTERNA
            _logger.LogInformation("Extrayendo datos desde Base de Datos externa...");

            var externalDbConnection = GetRequiredConfig("ConnectionStrings:ExternalDB");
            var externalDbQuery = GetRequiredConfig("Queries:HistoricalSales");

            var historicalSalesExtractor = new DatabaseExtractor<RawHistoricalSale>(
                externalDbConnection,
                externalDbQuery
            );

            var historicalSales = await historicalSalesExtractor.ExtractAsync();

            _logger.LogInformation($"Ventas históricas extraídas (BD externa): {historicalSales.Count()}");

            // Final extracción
            _logger.LogInformation("Proceso de extracción completado correctamente.");

            // ========================= TRANSFORM ============================

            _logger.LogInformation("Iniciando proceso de transformación...");

            var transformer = new TransformService();

            // Productos limpios
            var cleanProducts = transformer.TransformProducts(products, apiProductos);
            _logger.LogInformation($"Productos transformados (limpios): {cleanProducts.Count()}");

            // Clientes limpios
            var cleanCustomers = transformer.TransformCustomers(customers, apiClientes);
            _logger.LogInformation($"Clientes transformados (limpios): {cleanCustomers.Count()}");

            // Ventas limpias (Orders + Details + Historical)
            var cleanSales = transformer.TransformSales(orders, orderDetails, historicalSales);
            _logger.LogInformation($"Ventas transformadas (limpias): {cleanSales.Count()}");

            // final transformación
            _logger.LogInformation("Proceso de transformación completado correctamente.");

            // ========================= LOAD ============================

            _logger.LogInformation("Iniciando carga de dimensiones en DW...");

            // LIMPIAR DW COMPLETO
            _logger.LogInformation("Limpiando tablas del DW...");
            await _dwhRepo.ClearAllAsync();
            _logger.LogInformation("Todas las tablas del DW fueron limpiadas correctamente.");

            // DIM CATEGORIA
            _logger.LogInformation("Cargando DimCategoria...");

            var categorias = cleanProducts
                .Select(p => p.Categoria)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct()
                .ToList();

            var categoriaKeyMap = new Dictionary<string, int>();

            foreach (var cat in categorias)
            {
                var key = await _dwhRepo.UpsertCategoriaAsync(cat);
                categoriaKeyMap[cat] = key;
            }

            _logger.LogInformation($"DimCategoria cargada: {categoriaKeyMap.Count} categorías.");

            // DIM PRODUCTO
            _logger.LogInformation("Cargando DimProducto...");

            var productoKeyMap = new Dictionary<(string Nombre, string Categoria), int>();

            foreach (var p in cleanProducts)
            {
                var categoriaKey = categoriaKeyMap[p.Categoria];

                var key = await _dwhRepo.UpsertProductoAsync(
                    nombreProducto: p.NombreProducto,
                    idCategoria: categoriaKey,
                    precioUnit: p.PrecioUnit
                );

                productoKeyMap[(p.NombreProducto, p.Categoria)] = key;
            }

            _logger.LogInformation($"DimProducto cargada: {productoKeyMap.Count} productos.");

            // DIM CLIENTE
            _logger.LogInformation("Cargando DimCliente...");

            var clienteKeyMap = new Dictionary<(string Nombre, string? Email), int>();

            foreach (var c in cleanCustomers)
            {
                var key = await _dwhRepo.UpsertClienteAsync(
                    nombreCompleto: c.NombreCompleto,
                    email: c.Email,
                    pais: c.Pais,
                    ciudad: c.Ciudad
                );

                clienteKeyMap[(c.NombreCompleto, c.Email)] = key;
            }

            _logger.LogInformation($"DimCliente cargada: {clienteKeyMap.Count} clientes.");

            // DIM UBICACION
            _logger.LogInformation("Cargando DimUbicacion...");

            var ubicacionKeyMap = new Dictionary<(string? Pais, string? Ciudad), int>();

            var ubicacionesUnicas = cleanCustomers
                .Select(c => (c.Pais, c.Ciudad))
                .Distinct()
                .ToList();

            foreach (var u in ubicacionesUnicas)
            {
                var key = await _dwhRepo.UpsertUbicacionAsync(
                    pais: u.Pais,
                    region: "N/A",
                    ciudad: u.Ciudad
                );

                ubicacionKeyMap[(u.Pais, u.Ciudad)] = key;
            }

            _logger.LogInformation($"DimUbicacion cargada: {ubicacionKeyMap.Count} ubicaciones.");

            // DIM TIEMPO
            _logger.LogInformation("Cargando DimTiempo...");

            var tiempoKeyMap = new Dictionary<DateTime, int>();

            var fechasUnicas = cleanSales
                .Select(s => s.Fecha.Date)
                .Distinct()
                .ToList();

            foreach (var f in fechasUnicas)
            {
                var key = await _dwhRepo.UpsertTiempoAsync(f);
                tiempoKeyMap[f] = key;
            }

            _logger.LogInformation($"DimTiempo cargada: {tiempoKeyMap.Count} fechas.");

            // FIN DEL LOAD DIMENSIONES
            _logger.LogInformation("Carga de dimensiones completada correctamente.");

            // FIN
            _logger.LogInformation("Proceso de ETL completado correctamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error durante el proceso de extracción.");
        }
    }

    private string GetRequiredConfig(string key)
    {
        var value = _config[key];
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Configuración requerida no encontrada: '{key}'.");
        }
        return value;
    }
}
