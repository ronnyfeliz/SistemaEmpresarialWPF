using Project_v1.VistaModelos;
using System.Windows;
using System.Windows.Controls;

namespace Project_v1.Vista
{
    // Code-behind del modulo de usuario
    // Conecta la vista con su ViewModel pasando la referencia de la ventana principal
    public partial class UsuarioControl : UserControl
    {
        public UsuarioControl()
        {
            InitializeComponent();

            // Busca la ventana principal para pasarla al ViewModel
            // Esto permite cerrarla correctamente al cerrar sesion
            Loaded += (s, e) =>
            {
                var ventana = Window.GetWindow(this);
                DataContext = new UsuarioViewModel(ventana);
            };
        }
    }
}