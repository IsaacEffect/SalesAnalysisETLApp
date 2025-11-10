
using Microsoft.EntityFrameworkCore;
using SalesAnalysisETLApp.Api.Data.Entities;

namespace SalesAnalysisETLApp.Api.Data.Context
{
    public class ExternalContext : DbContext
    {
        public ExternalContext(DbContextOptions<ExternalContext> options) : base(options) { }

        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<Producto> Productos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Cliente>().ToTable("Clientes");
            modelBuilder.Entity<Producto>().ToTable("Productos");
        }
    }
}
