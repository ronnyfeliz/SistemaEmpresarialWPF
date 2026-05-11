using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Project_v1.Modelos;
using Project_v1.Servicios;
using System.Collections.ObjectModel;
using System.Windows;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Project_v1.VistaModelos
{
    public partial class ServicioViewModel : ObservableObject
    {
        private List<Servicio> _todosLosServicios = new();

        [ObservableProperty]
        private ObservableCollection<Servicio> servicios = new();

        [ObservableProperty]
        private Servicio? servicioSeleccionado;

        // ===== FILTROS =====
        [ObservableProperty] private string filtroNombre = string.Empty;
        [ObservableProperty] private string filtroCategoria = string.Empty;
        [ObservableProperty] private bool mostrarInactivos = false;

        // ===== FORMULARIO =====
        [ObservableProperty] private string nombre = string.Empty;
        [ObservableProperty] private string descripcion = string.Empty;
        [ObservableProperty] private string categoria = string.Empty;
        [ObservableProperty] private string precio = string.Empty;
        [ObservableProperty] private bool activo = true;

        // Solo el admin puede editar
        public bool EsAdmin => Sesion.RolActivo.ToString() == "Administrador";

        private bool _modoEdicion = false;

        public ServicioViewModel()
        {
            CargarServicios();
        }

        private void CargarServicios()
        {
            using var db = new AppDbContext();
            _todosLosServicios = db.Servicios.ToList();
            AplicarFiltros();
        }

        private void AplicarFiltros()
        {
            var resultado = _todosLosServicios.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(FiltroNombre))
                resultado = resultado.Where(s =>
                    s.Nombre.Contains(FiltroNombre, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(FiltroCategoria))
                resultado = resultado.Where(s =>
                    s.Categoria.Contains(FiltroCategoria, StringComparison.OrdinalIgnoreCase));

            if (!MostrarInactivos)
                resultado = resultado.Where(s => s.Activo);

            Servicios = new ObservableCollection<Servicio>(resultado.ToList());
        }

        partial void OnFiltroNombreChanged(string value) => AplicarFiltros();
        partial void OnFiltroCategoriaChanged(string value) => AplicarFiltros();
        partial void OnMostrarInactivosChanged(bool value) => AplicarFiltros();

        partial void OnServicioSeleccionadoChanged(Servicio? value)
        {
            if (value != null)
            {
                Nombre = value.Nombre;
                Descripcion = value.Descripcion;
                Categoria = value.Categoria;
                Precio = value.Precio.ToString();
                Activo = value.Activo;
                _modoEdicion = true;
            }
        }

        [RelayCommand]
        private void ExportarExcel()
        {
            if (Servicios.Count == 0)
            {
                MessageBox.Show("No hay servicios para exportar.",
                    "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            ExcelExportService.ExportarServicios(Servicios.ToList());
        }

        [RelayCommand]
        private void LimpiarFiltros()
        {
            FiltroNombre = string.Empty;
            FiltroCategoria = string.Empty;
            MostrarInactivos = false;
        }

        [RelayCommand]
        private void Guardar()
        {
            if (!EsAdmin)
            {
                MessageBox.Show("Solo el administrador puede gestionar servicios.",
                    "Acceso denegado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(Nombre))
            {
                MessageBox.Show("El nombre del servicio es obligatorio.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(Precio, out decimal precioDecimal) || precioDecimal < 0)
            {
                MessageBox.Show("El precio debe ser un número válido mayor o igual a 0.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using var db = new AppDbContext();

            if (_modoEdicion && ServicioSeleccionado != null)
            {
                var servicio = db.Servicios.Find(ServicioSeleccionado.Id);
                if (servicio != null)
                {
                    servicio.Nombre = Nombre;
                    servicio.Descripcion = Descripcion;
                    servicio.Categoria = Categoria;
                    servicio.Precio = precioDecimal;
                    servicio.Activo = Activo;
                    db.SaveChanges();
                    MessageBox.Show("Servicio actualizado correctamente.",
                        "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                var nuevo = new Servicio
                {
                    Nombre = Nombre,
                    Descripcion = Descripcion,
                    Categoria = Categoria,
                    Precio = precioDecimal,
                    Activo = true
                };
                db.Servicios.Add(nuevo);
                db.SaveChanges();
                MessageBox.Show("Servicio registrado correctamente.",
                    "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            LimpiarFormulario();
            CargarServicios();
        }

        [RelayCommand]
        private void Eliminar()
        {
            if (!EsAdmin)
            {
                MessageBox.Show("Solo el administrador puede eliminar servicios.",
                    "Acceso denegado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (ServicioSeleccionado == null)
            {
                MessageBox.Show("Selecciona un servicio primero.",
                    "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var confirmar = MessageBox.Show(
                $"¿Eliminar el servicio '{ServicioSeleccionado.Nombre}'?",
                "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirmar == MessageBoxResult.Yes)
            {
                using var db = new AppDbContext();
                var servicio = db.Servicios.Find(ServicioSeleccionado.Id);
                if (servicio != null)
                {
                    db.Servicios.Remove(servicio);
                    db.SaveChanges();
                    MessageBox.Show("Servicio eliminado correctamente.",
                        "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                LimpiarFormulario();
                CargarServicios();
            }
        }

        [RelayCommand]
        private void ToggleActivo()
        {
            if (!EsAdmin)
            {
                MessageBox.Show("Solo el administrador puede cambiar el estado de un servicio.",
                    "Acceso denegado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (ServicioSeleccionado == null)
            {
                MessageBox.Show("Selecciona un servicio primero.",
                    "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using var db = new AppDbContext();
            var servicio = db.Servicios.Find(ServicioSeleccionado.Id);
            if (servicio != null)
            {
                servicio.Activo = !servicio.Activo;
                db.SaveChanges();
                string estado = servicio.Activo ? "activado" : "desactivado";
                MessageBox.Show($"Servicio {estado} correctamente.",
                    "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            LimpiarFormulario();
            CargarServicios();
        }

        [RelayCommand]
        private void Nuevo() => LimpiarFormulario();

        private void LimpiarFormulario()
        {
            Nombre = string.Empty;
            Descripcion = string.Empty;
            Categoria = string.Empty;
            Precio = string.Empty;
            Activo = true;
            ServicioSeleccionado = null;
            _modoEdicion = false;
        }
    }
}