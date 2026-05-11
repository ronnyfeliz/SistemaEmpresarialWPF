using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Project_v1.Modelos;

namespace Project_v1.Servicios
{
    public class AppDbContext : DbContext
    {
        public DbSet<Producto> Productos { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Venta> Ventas { get; set; }
        public DbSet<DetalleVenta> DetallesVenta { get; set; }
        public DbSet<ClienteDeudor> ClientesDeudores { get; set; }
        public DbSet<Notificacion> Notificaciones { get; set; }
        public DbSet<Negocio> Negocios { get; set; }  // ← NUEVO

        public DbSet<Servicio> Servicios { get; set; }

        public DbSet<DetalleServicioVenta> DetallesServicioVenta { get; set; }
        public DbSet<Proveedor> Proveedores { get; set; }
        public DbSet<TransaccionProveedor> TransaccionesProveedor { get; set; }
        public DbSet<PagoProveedor> PagosProveedor { get; set; }
        public DbSet<HistorialProveedor> HistorialProveedores { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder
                .UseSqlite("Data Source=SistemaVentas.db")
                .ConfigureWarnings(warnings =>
                    warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<TransaccionProveedor>()
                .HasOne(t => t.Proveedor)
                .WithMany(p => p.Transacciones)
                .HasForeignKey(t => t.ProveedorId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PagoProveedor>()
                .HasOne(p => p.Proveedor)
                .WithMany(pr => pr.Pagos)
                .HasForeignKey(p => p.ProveedorId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PagoProveedor>()
                .HasOne(p => p.TransaccionProveedor)
                .WithMany(t => t.Pagos)
                .HasForeignKey(p => p.TransaccionProveedorId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<HistorialProveedor>()
                .HasOne(h => h.Proveedor)
                .WithMany(p => p.Historial)
                .HasForeignKey(h => h.ProveedorId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<HistorialProveedor>()
                .HasOne(h => h.TransaccionProveedor)
                .WithMany(t => t.Historial)
                .HasForeignKey(h => h.TransaccionProveedorId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<HistorialProveedor>()
                .HasOne(h => h.PagoProveedor)
                .WithMany()
                .HasForeignKey(h => h.PagoProveedorId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
