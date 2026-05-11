using System.Windows;

namespace Project_v1.Vista
{
    public partial class MoraDialogWindow : Window
    {
        // true si el usuario eligio aplicar mora
        public bool AplicaMora { get; private set; } = false;

        // Monto de mora ingresado por el usuario
        public decimal MontoMora { get; private set; } = 0;

        public MoraDialogWindow()
        {
            InitializeComponent();
        }

        private void BtnSi_Click(object sender, RoutedEventArgs e)
        {
            // Valida que el monto sea un numero valido mayor a 0
            if (!decimal.TryParse(TxtMora.Text, out decimal monto) || monto <= 0)
            {
                MessageBox.Show("Ingresa un monto de mora valido mayor a 0.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            AplicaMora = true;
            MontoMora = monto;
            DialogResult = true;
            Close();
        }

        private void BtnNo_Click(object sender, RoutedEventArgs e)
        {
            AplicaMora = false;
            MontoMora = 0;
            DialogResult = false;
            Close();
        }
    }
}