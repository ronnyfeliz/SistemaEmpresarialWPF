using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Project_v1.Modelos;
using Project_v1.Servicios;
using Project_v1.Vista;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Project_v1.VistaModelos
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly Window _ventana;

        [ObservableProperty]
        private object vistaActual;

        [ObservableProperty]
        private string nombreNegocio = string.Empty;

        [ObservableProperty]
        private string nombreUsuarioActivo = string.Empty;

        [ObservableProperty]
        private BitmapImage? logoPreview;

        public MainViewModel(Window ventana)
        {
            _ventana = ventana;

            using var db = new AppDbContext();
            var negocio = db.Negocios.FirstOrDefault();

            NombreNegocio = negocio?.NombreNegocio ?? "Mi Negocio";
            NombreUsuarioActivo = Sesion.NombreUsuario;

            // Ventas se mantiene como la pantalla inicial del sistema.
            VistaActual = new VentasViewModel();

            if (!string.IsNullOrWhiteSpace(negocio?.RutaLogo) && File.Exists(negocio.RutaLogo))
            {
                BitmapImage imagen = new();
                imagen.BeginInit();
                imagen.UriSource = new Uri(negocio.RutaLogo, UriKind.Absolute);
                imagen.CacheOption = BitmapCacheOption.OnLoad;
                imagen.EndInit();
                imagen.Freeze();
                LogoPreview = imagen;
            }
        }

        [RelayCommand]
        private void MostrarVentas() => VistaActual = new VentasViewModel();

        [RelayCommand]
        private void MostrarInventario() => VistaActual = new InventarioViewModel();

        [RelayCommand]
        private void MostrarHistorial() => VistaActual = new HistorialVentasViewModel();

        [RelayCommand]
        private void MostrarClientesDeudores() => VistaActual = new ClientesDeudoresViewModel();

        [RelayCommand]
        private void MostrarServicios() => VistaActual = new ServicioViewModel();

        [RelayCommand]
        private void MostrarProveedores() => VistaActual = new ProveedorViewModel();

        [RelayCommand]
        private void MostrarDashboard() => VistaActual = new DashboardViewModel();

        [RelayCommand]
        private void MostrarMiNegocio() => VistaActual = new MiNegocioViewModel();

        [RelayCommand]
        private void MostrarUsuario() => VistaActual = new UsuarioViewModel(_ventana);

        [RelayCommand]
        private void CerrarSesion()
        {
            var confirmar = MessageBox.Show("¿Estás seguro de cerrar sesión?",
                "Cerrar sesión", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirmar != MessageBoxResult.Yes) return;

            Sesion.Cerrar();
            MainWindow.CerrandoPorSesion = true;  // ← nombre correcto

            new LoginWindow().Show();   // primero abrir login
            _ventana.Close();           // luego cerrar main
        }
    }
}
