using Project_v1.VistaModelos;
using System.Windows.Controls;

namespace Project_v1.Vista
{
    // Code-behind del módulo de clientes deudores
    // Solo conecta la vista con su ViewModel
    public partial class ClientesDeudoresControl : UserControl
    {
        public ClientesDeudoresControl()
        {
            InitializeComponent();

            // Conecta el UserControl con su ViewModel
            DataContext = new ClientesDeudoresViewModel();
        }
    }
}