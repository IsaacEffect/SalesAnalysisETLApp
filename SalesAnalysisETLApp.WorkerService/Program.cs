using SalesAnalysisETLApp.Application.Services.Transform;
using SalesAnalysisETLApp.Domain.Interfaces.Repository;
using SalesAnalysisETLApp.Persistence.Destinations.Dwh;

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

            // Registrar Transform Service
            builder.Services.AddTransient<TransformService>();

            // Registrar el repositorio del Data Warehouse (LOAD)
            builder.Services.AddTransient<IDwhRepository, DwhRepository>();

            // Registrar el Worker
            builder.Services.AddHostedService<Worker>();

            var host = builder.Build();
            host.Run();
        }
    }
}
