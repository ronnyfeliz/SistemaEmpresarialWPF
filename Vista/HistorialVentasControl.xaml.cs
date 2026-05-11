using System.Windows;
using System.Windows.Controls;

namespace Project_v1.Vista
{
    public partial class HistorialVentasControl : UserControl
    {
        public HistorialVentasControl()
        {
            InitializeComponent();
        }

        private void BtnGenerarReporte_Click(object sender, RoutedEventArgs e)
        {
            PopupReporte.IsOpen = !PopupReporte.IsOpen;
        }

        // Cierra el popup cuando el usuario elige una opción.
        // El Command del botón se encarga de abrir el diálogo de período.
        private void OpcionReporte_Click(object sender, RoutedEventArgs e)
        {
            PopupReporte.IsOpen = false;
        }
    }
}