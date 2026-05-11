using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Project_v1.Modelos;
using Project_v1.Servicios;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace Project_v1.VistaModelos
{
    public partial class InventarioViewModel : ObservableObject
    {
        private List<Producto> _todosLosProductos = new();

        [ObservableProperty]
        private ObservableCollection<Producto> productos = new();

        [ObservableProperty]
        private Producto? productoSeleccionado;

        // ===== CAMPOS DEL FORMULARIO =====
        [ObservableProperty] private string nombre = string.Empty;
        [ObservableProperty] private string marca = string.Empty;
        [ObservableProperty] private string categoria = string.Empty;
        [ObservableProperty] private string descripcion = string.Empty;
        [ObservableProperty] private string precio = string.Empty;
        [ObservableProperty] private string stock = string.Empty;

        // ===== CAMPOS DE FILTRO =====
        [ObservableProperty] private string filtroNombre = string.Empty;
        [ObservableProperty] private string filtroMarca = string.Empty;
        [ObservableProperty] private string filtroCategoria = string.Empty;
        [ObservableProperty] private string filtroPrecioMin = string.Empty;
        [ObservableProperty] private string filtroPrecioMax = string.Empty;
        [ObservableProperty] private string filtroStockMin = string.Empty;

        // ===== NOTIFICACIONES DE STOCK BAJO =====
        [ObservableProperty] private int cantidadStockBajo = 0;
        [ObservableProperty] private bool hayStockBajo = false;

        public bool EsAdministrador => Sesion.RolActivo == Rol.Administrador;

        private bool _modoEdicion = false;

        public InventarioViewModel()
        {
            CargarProductos();
        }

        private void CargarProductos()
        {
            using var db = new AppDbContext();
            _todosLosProductos = db.Productos
                                   .Where(p => p.Activo == true)
                                   .ToList();

            CantidadStockBajo = _todosLosProductos.Count(p => p.Stock <= 10);
            HayStockBajo = CantidadStockBajo > 0;

            AplicarFiltros();
        }

        private void AplicarFiltros()
        {
            var resultado = _todosLosProductos.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(FiltroNombre))
                resultado = resultado.Where(p => p.Nombre.Contains(FiltroNombre, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(FiltroMarca))
                resultado = resultado.Where(p => p.Marca.Contains(FiltroMarca, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(FiltroCategoria))
                resultado = resultado.Where(p => p.Categoria.Contains(FiltroCategoria, StringComparison.OrdinalIgnoreCase));

            if (decimal.TryParse(FiltroPrecioMin, out decimal precioMin))
                resultado = resultado.Where(p => p.Precio >= precioMin);

            if (decimal.TryParse(FiltroPrecioMax, out decimal precioMax))
                resultado = resultado.Where(p => p.Precio <= precioMax);

            if (int.TryParse(FiltroStockMin, out int stockMin))
                resultado = resultado.Where(p => p.Stock >= stockMin);

            Productos = new ObservableCollection<Producto>(resultado.ToList());
        }

        partial void OnFiltroNombreChanged(string value) => AplicarFiltros();
        partial void OnFiltroMarcaChanged(string value) => AplicarFiltros();
        partial void OnFiltroCategoriaChanged(string value) => AplicarFiltros();
        partial void OnFiltroPrecioMinChanged(string value) => AplicarFiltros();
        partial void OnFiltroPrecioMaxChanged(string value) => AplicarFiltros();
        partial void OnFiltroStockMinChanged(string value) => AplicarFiltros();

        [RelayCommand]
        private void LimpiarFiltros()
        {
            FiltroNombre = string.Empty;
            FiltroMarca = string.Empty;
            FiltroCategoria = string.Empty;
            FiltroPrecioMin = string.Empty;
            FiltroPrecioMax = string.Empty;
            FiltroStockMin = string.Empty;
        }

        [RelayCommand]
        private void VerStockBajo()
        {
            new Vista.NotificacionStockWindow().ShowDialog();
        }

        partial void OnProductoSeleccionadoChanged(Producto? value)
        {
            if (value != null)
            {
                Nombre = value.Nombre;
                Marca = value.Marca;
                Categoria = value.Categoria;
                Descripcion = value.Descripcion;
                Precio = value.Precio.ToString();
                Stock = value.Stock.ToString();
                _modoEdicion = true;
            }
        }

        [RelayCommand]
        private void Guardar()
        {
            if (Sesion.RolActivo != Rol.Administrador)
            {
                MessageBox.Show("Solo el administrador puede agregar o modificar productos.",
                    "Acceso denegado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(Nombre))
            {
                MessageBox.Show("El nombre del producto es obligatorio.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!decimal.TryParse(Precio, out decimal precioDecimal) || precioDecimal < 0)
            {
                MessageBox.Show("El precio debe ser un número válido.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!int.TryParse(Stock, out int stockInt) || stockInt < 0)
            {
                MessageBox.Show("El stock debe ser un número entero válido.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using var db = new AppDbContext();
            if (_modoEdicion && ProductoSeleccionado != null)
            {
                var producto = db.Productos.Find(ProductoSeleccionado.Id);
                if (producto != null)
                {
                    producto.Nombre = Nombre;
                    producto.Marca = Marca;
                    producto.Categoria = Categoria;
                    producto.Descripcion = Descripcion;
                    producto.Precio = precioDecimal;
                    producto.Stock = stockInt;
                    db.SaveChanges();
                    MessageBox.Show("Producto actualizado correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                var nuevo = new Producto
                {
                    Nombre = Nombre,
                    Marca = Marca,
                    Categoria = Categoria,
                    Descripcion = Descripcion,
                    Precio = precioDecimal,
                    Stock = stockInt,
                    Activo = true
                };
                db.Productos.Add(nuevo);
                db.SaveChanges();
                MessageBox.Show("Producto agregado correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            LimpiarFormulario();
            CargarProductos();
        }

        [RelayCommand]
        private void ExportarExcel()
        {
            if (Productos.Count == 0)
            {
                MessageBox.Show("No hay productos para exportar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            ExcelExportService.ExportarInventario(Productos.ToList());
        }

        // EXPORTAR SOLO PRODUCTOS CON STOCK BAJO
        [RelayCommand]
        private void ExportarStockBajoExcel()
        {
            var productosBajoStock = Productos.Where(p => p.Stock <= 10).ToList();

            if (productosBajoStock.Count == 0)
            {
                MessageBox.Show("No hay productos con stock bajo para exportar.",
                    "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            ExcelExportService.ExportarStockBajo(productosBajoStock);   // ← Cambiado aquí
        }

        [RelayCommand]
        private void Eliminar()
        {
            if (Sesion.RolActivo != Rol.Administrador)
            {
                MessageBox.Show("Solo el administrador puede eliminar productos.",
                    "Acceso denegado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (ProductoSeleccionado == null)
            {
                MessageBox.Show("Selecciona un producto para eliminar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var confirmar = MessageBox.Show($"¿Estás seguro de eliminar '{ProductoSeleccionado.Nombre}'?",
                "Confirmar eliminación", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirmar == MessageBoxResult.Yes)
            {
                using var db = new AppDbContext();
                var producto = db.Productos.Find(ProductoSeleccionado.Id);
                if (producto != null)
                {
                    producto.Activo = false;
                    db.SaveChanges();
                    MessageBox.Show("Producto eliminado correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                LimpiarFormulario();
                CargarProductos();
            }
        }

        [RelayCommand]
        private void Nuevo() => LimpiarFormulario();

        private void LimpiarFormulario()
        {
            Nombre = string.Empty;
            Marca = string.Empty;
            Categoria = string.Empty;
            Descripcion = string.Empty;
            Precio = string.Empty;
            Stock = string.Empty;
            ProductoSeleccionado = null;
            _modoEdicion = false;
        }
    }
}