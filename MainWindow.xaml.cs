using System.ComponentModel;
using System.Windows;

namespace Project_v1
{
    public partial class MainWindow : Window
    {
        // Indica si el cierre fue iniciado por cerrar sesión
        public static bool CerrandoPorSesion { get; set; } = false;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = new Project_v1.VistaModelos.MainViewModel(this);
        }

        // Se ejecuta cuando el usuario intenta cerrar la ventana (X o Alt+F4)
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            // Si viene de cerrar sesión o cambiar cuenta, no preguntar
            if (MainWindow.CerrandoPorSesion)
            {
                MainWindow.CerrandoPorSesion = false;
                return;
            }

            var r = MessageBox.Show("¿Deseas cerrar el sistema?",
                "Cerrar", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (r == MessageBoxResult.No)
                e.Cancel = true;
        }

    }
}