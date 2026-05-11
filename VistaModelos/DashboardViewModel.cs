using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.EntityFrameworkCore;
using Project_v1.Servicios;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace Project_v1.VistaModelos
{
    public partial class DashboardViewModel : ObservableObject
    {
        // Estas propiedades alimentan las tarjetas principales del dashboard.
        [ObservableProperty] private decimal ventasHoy = 0;
        [ObservableProperty] private decimal ventasMes = 0;
        [ObservableProperty] private int deudoresActivos = 0;
        [ObservableProperty] private int productosStockBajo = 0;
        [ObservableProperty] private int proveedoresConDeuda = 0;
        [ObservableProperty] private decimal deudaProveedores = 0;
        [ObservableProperty] private string ultimoBackupTexto = "Sin backups aún";

        // Lista visible con las últimas 5 ventas registradas.
        public ObservableCollection<VentaResumenDashboard> UltimasVentas { get; } = new();

        public DashboardViewModel()
        {
            CargarDashboard();
        }

        private void CargarDashboard()
        {
            try
            {
                using var db = new AppDbContext();

                DateTime hoy = DateTime.Today;
                DateTime inicioMes = new DateTime(hoy.Year, hoy.Month, 1);

                // Ventas realizadas en el día actual.
                VentasHoy = db.Ventas
                    .Where(v => v.Fecha.Date == hoy)
                    .Sum(v => (decimal?)v.Total) ?? 0;

                // Ventas acumuladas desde el primer día del mes actual.
                VentasMes = db.Ventas
                    .Where(v => v.Fecha.Date >= inicioMes && v.Fecha.Date <= hoy)
                    .Sum(v => (decimal?)v.Total) ?? 0;

                // Deudores activos = clientes que todavía no han pagado.
                DeudoresActivos = db.ClientesDeudores
                    .Count(c => !c.Pagado);

                /* Se consideran de stock bajo los productos activos con 10 unidades o menos,
                   para mantener el mismo criterio visual que la ventana de alerta de inventario.*/
                ProductosStockBajo = db.Productos
                    .Count(p => p.Activo && p.Stock <= 10);

                var proveedores = db.Proveedores
                    .Include(p => p.Transacciones)
                    .Where(p => p.Activo)
                    .ToList();

                ProveedoresConDeuda = proveedores.Count(p => p.DeudaTotal > 0);
                DeudaProveedores = proveedores.Sum(p => p.DeudaTotal);

                /* Se reutiliza la lógica del servicio de backups para mantener consistencia
                    con la pantalla de Mi Negocio.*/
                DateTime? ultimoBackup = BackupService.FechaUltimoBackup();
                UltimoBackupTexto = ultimoBackup.HasValue
                    ? ultimoBackup.Value.ToString("dd/MM/yyyy HH:mm")
                    : "Sin backups aún";

                // Se cargan las últimas 5 ventas con el nombre del usuario que las registró.
                var ultimasVentas = db.Ventas
                    .Include(v => v.Usuario)
                    .OrderByDescending(v => v.Fecha)
                    .Take(5)
                    .Select(v => new VentaResumenDashboard
                    {
                        NumFactura = v.NumFactura,
                        FechaTexto = v.Fecha.ToString("dd/MM/yyyy HH:mm"),
                        Cajero = v.Usuario != null ? v.Usuario.Nombre : "-",
                        Total = v.Total
                    })
                    .ToList();

                UltimasVentas.Clear();
                foreach (var venta in ultimasVentas)
                    UltimasVentas.Add(venta);
            }
            catch
            {
                /* Si ocurre un error, el dashboard conserva valores por defecto
                   para evitar que la vista falle al cargar.*/
                VentasHoy = 0;
                VentasMes = 0;
                DeudoresActivos = 0;
                ProductosStockBajo = 0;
                ProveedoresConDeuda = 0;
                DeudaProveedores = 0;
                UltimoBackupTexto = "No disponible";
                UltimasVentas.Clear();
            }
        }
    }

    /* Modelo liviano para mostrar solo la información necesaria
       en la tabla/lista de últimas ventas del dashboard.*/
    public class VentaResumenDashboard
    {
        public string NumFactura { get; set; } = string.Empty;
        public string FechaTexto { get; set; } = string.Empty;
        public string Cajero { get; set; } = string.Empty;
        public decimal Total { get; set; }
    }
}
