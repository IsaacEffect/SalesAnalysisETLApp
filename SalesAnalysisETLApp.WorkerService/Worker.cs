using SalesAnalysisETLApp.Domain.Entities.Facts;
using SalesAnalysisETLApp.Domain.RawModels;
using SalesAnalysisETLApp.Persistence.Sources.Api;
using SalesAnalysisETLApp.Persistence.Sources.BD;
using SalesAnalysisETLApp.Persistence.Sources.Csv;


public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;

    public Worker(ILogger<Worker> logger, IConfiguration config, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _config = config;
        _httpClientFactory = httpClientFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Iniciando proceso de extracción...");

        try
        {
            // EXTRAER DESDE CSV ===
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

            // EXTRAER DESDE API ===
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
            

            // FIN
            _logger.LogInformation("Proceso de extracción completado correctamente.");
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
