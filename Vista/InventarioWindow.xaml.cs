using Project_v1.VistaModelos;
using System.Windows.Controls;

namespace Project_v1.Vista
{
    // UserControl del módulo de inventario
    // Se carga dentro de la ventana principal como contenido del módulo activo
    public partial class InventarioWindow : UserControl
    {
        public InventarioWindow()
        {
            InitializeComponent();

            // Conecta el UserControl con su ViewModel
            DataContext = new InventarioViewModel();
        }
    }
}