namespace SalesAnalysisETLApp.WorkerService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);

            // Cargar configuración desde appsettings.json
            builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            // Registrar IHttpClientFactory 
            builder.Services.AddHttpClient("SalesApiClient");

            // Registrar el Worker
            builder.Services.AddHostedService<Worker>();

            var host = builder.Build();
            host.Run();
        }
    }
}
