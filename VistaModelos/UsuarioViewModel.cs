using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Project_v1.Modelos;
using Project_v1.Servicios;
using Project_v1.Vista;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Project_v1.VistaModelos
{
    public partial class UsuarioViewModel : ObservableObject
    {
        private readonly Window _ventana;

        public string NombreUsuario => Sesion.NombreUsuario;
        public string RolUsuario => Sesion.RolActivo.ToString();
        public string EstadoSesion => Sesion.EstaActivo ? "Activo" : "Inactivo";
        public bool EsAdministrador => Sesion.RolActivo == Rol.Administrador;

        [ObservableProperty]
        private ObservableCollection<Usuario> cajeros = new();

        [ObservableProperty]
        private bool puedeAgregarCajero = false;

        [ObservableProperty]
        private string infoCajeros = string.Empty;

        [ObservableProperty]
        private Usuario? cajeroSeleccionado;

        [ObservableProperty]
        private string nuevoCajeroNombre = string.Empty;

        [ObservableProperty]
        private bool mostrarFormularioCajero = false;

        public UsuarioViewModel(Window ventana)
        {
            _ventana = ventana;
            if (EsAdministrador)
                CargarCajeros();
        }

        private void CargarCajeros()
        {
            using var db = new AppDbContext();
            var lista = db.Usuarios
                          .Where(u => u.Rol == Rol.Cajero && u.Activo == true)
                          .ToList();

            Cajeros = new ObservableCollection<Usuario>(lista);
            PuedeAgregarCajero = lista.Count < 5;
            InfoCajeros = $"Cajeros registrados: {lista.Count} / 5";
        }

        [RelayCommand]
        private void MostrarFormulario()
        {
            MostrarFormularioCajero = true;
        }

        [RelayCommand]
        private void GuardarCajero(PasswordBox passwordBox)
        {
            if (string.IsNullOrWhiteSpace(NuevoCajeroNombre))
            {
                MessageBox.Show("El nombre del cajero es obligatorio.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string contrasena = passwordBox?.Password ?? string.Empty;
            if (string.IsNullOrWhiteSpace(contrasena))
            {
                MessageBox.Show("La contrasena es obligatoria.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using var db = new AppDbContext();

            if (db.Usuarios.Count(u => u.Rol == Rol.Cajero && u.Activo == true) >= 5)
            {
                MessageBox.Show("Se ha alcanzado el limite de 5 cajeros activos.",
                    "Limite alcanzado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (db.Usuarios.Any(u => u.Nombre == NuevoCajeroNombre && u.Activo == true))
            {
                MessageBox.Show("Ya existe un usuario activo con ese nombre.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            db.Usuarios.Add(new Usuario
            {
                Nombre = NuevoCajeroNombre,
                Contrasena = contrasena,
                Rol = Rol.Cajero,
                Activo = true
            });
            db.SaveChanges();

            MessageBox.Show($"Cajero '{NuevoCajeroNombre}' registrado correctamente.",
                "Exito", MessageBoxButton.OK, MessageBoxImage.Information);

            NuevoCajeroNombre = string.Empty;
            MostrarFormularioCajero = false;
            if (passwordBox != null) passwordBox.Clear();
            CargarCajeros();
        }

        [RelayCommand]
        private void CancelarFormulario(PasswordBox passwordBox)
        {
            NuevoCajeroNombre = string.Empty;
            MostrarFormularioCajero = false;
            if (passwordBox != null) passwordBox.Clear();
        }

        [RelayCommand]
        private void EliminarCajero()
        {
            if (CajeroSeleccionado == null)
            {
                MessageBox.Show("Selecciona un cajero para eliminar.",
                    "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var confirmar = MessageBox.Show(
                $"¿Eliminar al cajero '{CajeroSeleccionado.Nombre}'?",
                "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirmar != MessageBoxResult.Yes) return;

            using var db = new AppDbContext();
            var cajero = db.Usuarios.Find(CajeroSeleccionado.Id);
            if (cajero != null)
            {
                cajero.Activo = false;
                db.SaveChanges();
                MessageBox.Show("Cajero eliminado correctamente.",
                    "Exito", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            CajeroSeleccionado = null;
            CargarCajeros();
        }

        // ── Cambiar cuenta ────────────────────────────────────────────────
        [RelayCommand]
        private void CambiarCuenta()
        {
            var confirmar = MessageBox.Show(
                "¿Deseas cerrar esta sesión para iniciar con otra cuenta?",
                "Cambiar cuenta", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirmar != MessageBoxResult.Yes) return;

            IrAlLogin();
        }

        // ── Cerrar sesión ─────────────────────────────────────────────────
        [RelayCommand]
        private void CerrarSesion()
        {
            var confirmar = MessageBox.Show(
                "¿Estás seguro de cerrar sesión?",
                "Cerrar sesión", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirmar != MessageBoxResult.Yes) return;

            IrAlLogin();
        }

        // ── Flujo compartido: limpiar sesión → abrir Login → cerrar Main ──
        private void IrAlLogin()
        {
            // 1. Limpiar sesión
            Sesion.Cerrar();

            // 2. Indicar a MainWindow que este cierre es por logout,
            //    para que NO muestre el diálogo "¿deseas cerrar el sistema?"
            MainWindow.CerrandoPorSesion = true;

            // 3. Abrir Login
            new LoginWindow().Show();

            // 4. Cerrar MainWindow (silencioso gracias a la bandera)
            _ventana.Close();
        }
    }
}