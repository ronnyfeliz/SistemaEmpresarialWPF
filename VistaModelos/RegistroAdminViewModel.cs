using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Project_v1.Modelos;
using Project_v1.Servicios;
using Project_v1.Vista;
using System.Windows;
using System.Windows.Controls;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Project_v1.VistaModelos
{
    /* ViewModel de la ventana de registro inicial del administrador
       Solo se usa una vez, cuando el sistema no tiene usuarios registrados*/
    public partial class RegistroAdminViewModel : ObservableObject
    {
        // Referencia a la ventana para cerrarla al completar el registro
        private readonly Window _ventana;

        public RegistroAdminViewModel(Window ventana)
        {
            _ventana = ventana;
        }

        // Propiedad enlazada al campo de nombre de usuario
        [ObservableProperty]
        private string nombre = string.Empty;

        // Comando ejecutado al presionar el botón "Registrar Administrador"
        [RelayCommand]
        private void Registrar(PasswordBox passwordBox)
        {
            // Obtiene la contraseña del PasswordBox
            string contrasena = passwordBox.Password;

            // Valida que el nombre no esté vacío
            if (string.IsNullOrWhiteSpace(Nombre))
            {
                MessageBox.Show("El nombre de usuario no puede estar vacío.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Valida que la contraseña no esté vacía
            if (string.IsNullOrWhiteSpace(contrasena))
            {
                MessageBox.Show("La contraseña no puede estar vacía.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using var db = new AppDbContext();

            // Crea el usuario administrador
            var admin = new Usuario
            {
                Nombre = Nombre,
                Contrasena = contrasena,
                Rol = Rol.Administrador,
                Activo = true
            };

            // Guarda el administrador en la base de datos
            db.Usuarios.Add(admin);
            db.SaveChanges();

            MessageBox.Show("Administrador registrado correctamente. Ya puedes iniciar sesión.",
                "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

            // Abre el login y cierra esta ventana
            var login = new LoginWindow();
            login.Show();
            _ventana.Close();
        }
    }
}