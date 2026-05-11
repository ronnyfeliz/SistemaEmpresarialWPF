using Project_v1.VistaModelos;
using System.Windows.Controls;

namespace Project_v1.Vista
{
    // Code-behind del módulo de ventas
    // Solo conecta la vista con su ViewModel
    public partial class VentasControl : UserControl
    {
        public VentasControl()
        {
            InitializeComponent();

            // Conecta el UserControl con su ViewModel
            DataContext = new VentasViewModel();
        }
    }
}