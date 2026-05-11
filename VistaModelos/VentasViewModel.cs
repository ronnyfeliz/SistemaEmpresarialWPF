using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Project_v1.Modelos;
using Project_v1.Servicios;
using System.Collections.ObjectModel;
using System.Windows;

namespace Project_v1.VistaModelos
{
    public partial class VentasViewModel : ObservableObject
    {
        // ===== PRODUCTOS =====
        [ObservableProperty] private ObservableCollection<Producto> resultadosBusqueda = new();
        [ObservableProperty] private Producto? productoSeleccionado;
        [ObservableProperty] private string textoBusqueda = string.Empty;
        [ObservableProperty] private string cantidad = "1";
        [ObservableProperty] private ObservableCollection<DetalleVenta> carrito = new();
        [ObservableProperty] private decimal total = 0;

        // ===== SERVICIOS =====
        [ObservableProperty] private ObservableCollection<Servicio> resultadosBusquedaServicio = new();
        [ObservableProperty] private Servicio? servicioSeleccionado;
        [ObservableProperty] private string textoBusquedaServicio = string.Empty;
        [ObservableProperty] private string cantidadServicio = "1";
        [ObservableProperty] private ObservableCollection<DetalleServicioVenta> carritoServicios = new();

        // ===== RESUMEN DEL DÍA =====
        [ObservableProperty] private decimal ventasDelDiaTotal = 0;
        [ObservableProperty] private int ventasDelDiaCantidad = 0;
        [ObservableProperty] private int productosVendidosHoy = 0;
        [ObservableProperty] private int serviciosVendidosHoy = 0;

        public VentasViewModel()
        {
            CargarResumenDelDia();
        }

        private void CargarResumenDelDia()
        {
            using var db = new AppDbContext();
            var ventasHoy = db.Ventas
                .Where(v => v.Fecha.Date == DateTime.Today)
                .ToList();

            VentasDelDiaCantidad = ventasHoy.Count;
            VentasDelDiaTotal = ventasHoy.Sum(v => v.Total);

            var ids = ventasHoy.Select(v => v.Id).ToList();

            ProductosVendidosHoy = db.DetallesVenta
                .Where(d => ids.Contains(d.VentaId))
                .Sum(d => (int?)d.Cantidad) ?? 0;

            ServiciosVendidosHoy = db.DetallesServicioVenta
                .Where(d => ids.Contains(d.VentaId))
                .Sum(d => (int?)d.Cantidad) ?? 0;
        }

        // ===== BUSCAR PRODUCTO =====
        [RelayCommand]
        private void Buscar()
        {
            if (string.IsNullOrWhiteSpace(TextoBusqueda))
            {
                ResultadosBusqueda.Clear();
                return;
            }

            using var db = new AppDbContext();
            var resultados = db.Productos
                .Where(p => p.Activo)
                .ToList()
                .Where(p =>
                    p.Nombre.Contains(TextoBusqueda, StringComparison.OrdinalIgnoreCase) ||
                    p.Marca.Contains(TextoBusqueda, StringComparison.OrdinalIgnoreCase))
                .ToList();

            ResultadosBusqueda = new ObservableCollection<Producto>(resultados);
        }

        // ===== AGREGAR PRODUCTO AL CARRITO =====
        [RelayCommand]
        private void AgregarAlCarrito()
        {
            if (ProductoSeleccionado == null)
            {
                MessageBox.Show("Selecciona un producto primero.",
                    "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(Cantidad, out int cant) || cant <= 0)
            {
                MessageBox.Show("La cantidad debe ser un numero mayor a 0.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var existente = Carrito.FirstOrDefault(d => d.ProductoId == ProductoSeleccionado.Id);
            int cantidadTotal = existente != null ? existente.Cantidad + cant : cant;

            if (cantidadTotal > ProductoSeleccionado.Stock)
            {
                MessageBox.Show($"Stock insuficiente. Disponible: {ProductoSeleccionado.Stock}, " +
                                $"En carrito: {(existente?.Cantidad ?? 0)}, " +
                                $"Maximo adicional: {ProductoSeleccionado.Stock - (existente?.Cantidad ?? 0)}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (existente != null)
            {
                int index = Carrito.IndexOf(existente);
                Carrito.RemoveAt(index);
                Carrito.Insert(index, new DetalleVenta
                {
                    ProductoId = existente.ProductoId,
                    Producto = existente.Producto,
                    VentaId = existente.VentaId,
                    Cantidad = cantidadTotal,
                    PrecioUnitario = existente.PrecioUnitario,
                    Subtotal = cantidadTotal * existente.PrecioUnitario
                });
            }
            else
            {
                Carrito.Add(new DetalleVenta
                {
                    ProductoId = ProductoSeleccionado.Id,
                    Producto = ProductoSeleccionado,
                    Cantidad = cant,
                    PrecioUnitario = ProductoSeleccionado.Precio,
                    Subtotal = cant * ProductoSeleccionado.Precio
                });
            }

            ActualizarTotal();
            TextoBusqueda = string.Empty;
            ResultadosBusqueda.Clear();
            Cantidad = "1";
        }

        // ===== ELIMINAR PRODUCTO DEL CARRITO =====
        [RelayCommand]
        private void EliminarDelCarrito(DetalleVenta detalle)
        {
            Carrito.Remove(detalle);
            ActualizarTotal();
        }

        // ===== BUSCAR SERVICIO =====
        [RelayCommand]
        private void BuscarServicio()
        {
            if (string.IsNullOrWhiteSpace(TextoBusquedaServicio))
            {
                ResultadosBusquedaServicio.Clear();
                return;
            }

            using var db = new AppDbContext();
            var resultados = db.Servicios
                .Where(s => s.Activo)
                .ToList()
                .Where(s =>
                    s.Nombre.Contains(TextoBusquedaServicio, StringComparison.OrdinalIgnoreCase) ||
                    s.Categoria.Contains(TextoBusquedaServicio, StringComparison.OrdinalIgnoreCase))
                .ToList();

            ResultadosBusquedaServicio = new ObservableCollection<Servicio>(resultados);
        }

        // ===== AGREGAR SERVICIO AL CARRITO =====
        [RelayCommand]
        private void AgregarServicioAlCarrito()
        {
            if (ServicioSeleccionado == null)
            {
                MessageBox.Show("Selecciona un servicio primero.",
                    "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(CantidadServicio, out int cant) || cant <= 0)
            {
                MessageBox.Show("La cantidad debe ser un número mayor a 0.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var existente = CarritoServicios.FirstOrDefault(d => d.ServicioId == ServicioSeleccionado.Id);

            if (existente != null)
            {
                int index = CarritoServicios.IndexOf(existente);
                CarritoServicios.RemoveAt(index);
                CarritoServicios.Insert(index, new DetalleServicioVenta
                {
                    ServicioId = existente.ServicioId,
                    Servicio = existente.Servicio,
                    Cantidad = existente.Cantidad + cant,
                    PrecioUnitario = existente.PrecioUnitario,
                    Subtotal = (existente.Cantidad + cant) * existente.PrecioUnitario
                });
            }
            else
            {
                CarritoServicios.Add(new DetalleServicioVenta
                {
                    ServicioId = ServicioSeleccionado.Id,
                    Servicio = ServicioSeleccionado,
                    Cantidad = cant,
                    PrecioUnitario = ServicioSeleccionado.Precio,
                    Subtotal = cant * ServicioSeleccionado.Precio
                });
            }

            ActualizarTotal();
            TextoBusquedaServicio = string.Empty;
            ResultadosBusquedaServicio.Clear();
            CantidadServicio = "1";
        }

        // ===== ELIMINAR SERVICIO DEL CARRITO =====
        [RelayCommand]
        private void EliminarServicioDelCarrito(DetalleServicioVenta detalle)
        {
            CarritoServicios.Remove(detalle);
            ActualizarTotal();
        }

        // ===== TOTAL =====
        private void ActualizarTotal()
        {
            Total = Carrito.Sum(d => d.Subtotal) + CarritoServicios.Sum(d => d.Subtotal);
        }

        // ===== CONFIRMAR VENTA =====
        [RelayCommand]
        private async Task ConfirmarVenta()
        {
            if (!Carrito.Any() && !CarritoServicios.Any())
            {
                MessageBox.Show("Agrega al menos un producto o servicio para confirmar la venta.",
                    "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using var db = new AppDbContext();

            var ahora = DateTime.Now;
            string numFactura = $"F{ahora:yyyyMMddHHmmss}";

            var venta = new Venta
            {
                NumFactura = numFactura,
                Fecha = ahora,
                Total = Total,
                UsuarioId = db.Usuarios.First(u => u.Nombre == Sesion.NombreUsuario).Id
            };

            db.Ventas.Add(venta);
            db.SaveChanges();

            foreach (var item in Carrito)
            {
                db.DetallesVenta.Add(new DetalleVenta
                {
                    VentaId = venta.Id,
                    ProductoId = item.ProductoId,
                    Cantidad = item.Cantidad,
                    PrecioUnitario = item.PrecioUnitario,
                    Subtotal = item.Subtotal
                });

                var producto = db.Productos.Find(item.ProductoId);
                if (producto != null)
                    producto.Stock -= item.Cantidad;
            }

            foreach (var item in CarritoServicios)
            {
                db.DetallesServicioVenta.Add(new DetalleServicioVenta
                {
                    VentaId = venta.Id,
                    ServicioId = item.ServicioId,
                    Cantidad = item.Cantidad,
                    PrecioUnitario = item.PrecioUnitario,
                    Subtotal = item.Subtotal
                });
            }

            db.SaveChanges();

            var ventanaFactura = new Project_v1.Vista.FacturaConfirmadaWindow(
                venta, Carrito, CarritoServicios);
            ventanaFactura.ShowDialog();

            Carrito.Clear();
            CarritoServicios.Clear();
            Total = 0;

            CargarResumenDelDia(); // ← actualiza el panel del día
        }

        // ===== EXPORTAR RESUMEN DEL DÍA =====
        [RelayCommand]
        private void ExportarResumenExcel()
        {
            using var db = new AppDbContext();
            var ventasHoy = db.Ventas
                .Where(v => v.Fecha.Date == DateTime.Today)
                .ToList();

            ExcelExportService.ExportarResumenDiario(
                VentasDelDiaTotal,
                VentasDelDiaCantidad,
                ProductosVendidosHoy,
                ServiciosVendidosHoy,
                ventasHoy);
        }

        [RelayCommand]
        private void ExportarResumenPdf()
        {
            using var db = new AppDbContext();
            var ventasHoy = db.Ventas
                .Where(v => v.Fecha.Date == DateTime.Today)
                .ToList();

            FacturaPdfService.ExportarResumenDiario(
                VentasDelDiaTotal,
                VentasDelDiaCantidad,
                ProductosVendidosHoy,
                ServiciosVendidosHoy,
                ventasHoy);
        }

        // ===== CANCELAR VENTA =====
        [RelayCommand]
        private void CancelarVenta()
        {
            if (!Carrito.Any() && !CarritoServicios.Any()) return;

            var confirmar = MessageBox.Show("¿Cancelar la venta actual?",
                "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirmar == MessageBoxResult.Yes)
            {
                Carrito.Clear();
                CarritoServicios.Clear();
                Total = 0;
            }
        }

        [RelayCommand]
        private void VerDiasAnteriores()
        {
            var ventana = new Vista.VentasPorFechaWindow();
            ventana.ShowDialog();
        }
    }
}