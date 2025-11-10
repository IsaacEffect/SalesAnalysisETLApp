
using Microsoft.EntityFrameworkCore;
using SalesAnalysisETLApp.Api.Data.Context;
using SalesAnalysisETLApp.Api.Data.Interfaces;
using SalesAnalysisETLApp.Api.Data.Repositories;

namespace SalesAnalysisETLApp.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Configuración de DbContext
            builder.Services.AddDbContext<ExternalContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("ExternalDB")));


            // Repositorios
            builder.Services.AddScoped<IClienteRepository, ClienteRepository>();
            builder.Services.AddScoped<IProductoRepository, ProductoRepository>();

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
