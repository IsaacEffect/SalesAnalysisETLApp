namespace SalesAnalysisETLApp.WorkerService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);
            builder.Services.AddHostedService<Worker>();

            // Cargar configuración desde appsettings
            builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            // Registrar el Worker
            builder.Services.AddHostedService<Worker>();

            var host = builder.Build();
            host.Run();
        }
    }
}