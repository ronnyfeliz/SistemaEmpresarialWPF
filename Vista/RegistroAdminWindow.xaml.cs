using Project_v1.VistaModelos;
using System.Windows;
using System.Windows.Controls;

namespace Project_v1.Vista
{
    // Code-behind de la ventana de registro inicial del administrador
    // Su única responsabilidad es conectar la vista con su ViewModel
    public partial class RegistroAdminWindow : Window
    {
        public RegistroAdminWindow()
        {
            InitializeComponent();

            // Se asigna el ViewModel como contexto de datos de la ventana
            // Esto permite que los bindings del XAML funcionen correctamente
            DataContext = new RegistroAdminViewModel(this);
        }

        // Evento del botón Registrar — pasa el PasswordBox al ViewModel
        private void BtnRegistrar_Click(object sender, RoutedEventArgs e)
        {
            var vm = (RegistroAdminViewModel)DataContext;
            vm.RegistrarCommand.Execute(TxtContrasena);
        }
    }
}