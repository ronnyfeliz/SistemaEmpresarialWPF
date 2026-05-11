using Project_v1.Servicios;
using Project_v1.VistaModelos;
using System.ComponentModel;
using System.Windows;

namespace Project_v1.Vista
{
    // Code-behind de la ventana de login
    // Su única responsabilidad es conectar la vista con su ViewModel
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            DataContext = new LoginViewModel(this);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            // Si la sesión no está activa, el usuario cerró el login sin entrar
            if (!Sesion.EstaActivo)
                Application.Current.Shutdown();
        }
    }
}