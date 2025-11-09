using SalesAnalysisETLApp.Domain.RawModels;
using SalesAnalysisETLApp.Persistence.Sources.Csv;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _config;

    public Worker(ILogger<Worker> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Iniciando proceso de extracción...");

        try
        {
            // Extraer Products (CSV)
            var productsPath = GetRequiredConfig("FilePaths:ProductsCsv");
            var productsExtractor = new CsvExtractor<RawProduct>(productsPath);
            var products = await productsExtractor.ExtractAsync();
            _logger.LogInformation($"Productos extraídos: {products.Count()}");

            // Extraer Customers (CSV)
            var customersPath = GetRequiredConfig("FilePaths:CustomersCsv");
            var customersExtractor = new CsvExtractor<RawCustomer>(customersPath);
            var customers = await customersExtractor.ExtractAsync();
            _logger.LogInformation($"Clientes extraídos: {customers.Count()}");

            // Extraer Orders (CSV)
            var ordersPath = GetRequiredConfig("FilePaths:OrdersCsv");
            var ordersExtractor = new CsvExtractor<RawOrder>(ordersPath);
            var orders = await ordersExtractor.ExtractAsync();
            _logger.LogInformation($"Órdenes extraídas: {orders.Count()}");

            // Extraer Order Details (CSV)
            var orderDetailsPath = GetRequiredConfig("FilePaths:OrderDetailsCsv");
            var orderDetailsExtractor = new CsvExtractor<RawOrderDetail>(orderDetailsPath);
            var orderDetails = await orderDetailsExtractor.ExtractAsync();
            _logger.LogInformation($"Detalles de órdenes extraídos: {orderDetails.Count()}");

            // Terminar proceso
            _logger.LogInformation("Proceso de extracción completado correctamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error durante el proceso de extracción");
        }
    }

    private string GetRequiredConfig(string key)
    {
        var value = _config[key];
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Configuration value required but not found: '{key}'.");
        }
        return value;
    }
}