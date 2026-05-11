using Project_v1.Modelos;
using Project_v1.Servicios;
using System.Windows;

namespace Project_v1.Vista
{
    public partial class PagoConfirmadoWindow : Window
    {
        private readonly ClienteDeudor _cliente;
        private readonly bool _moraAplicada;
        private readonly decimal _totalPagado;

        public PagoConfirmadoWindow(ClienteDeudor cliente)
        {
            InitializeComponent();

            _cliente = cliente;
            _moraAplicada = cliente.TieneMora && DateTime.Today > cliente.FechaPago;
            _totalPagado = cliente.MontoDeuda + (_moraAplicada ? cliente.MontoMora : 0);

            var ahora = DateTime.Now;
            TxtFecha.Text = ahora.ToString("dd/MM/yyyy");
            TxtHora.Text = ahora.ToString("HH:mm");

            TxtNombre.Text = cliente.Nombre;
            TxtCedula.Text = cliente.Cedula;
            TxtTelefono.Text = cliente.Telefono;
            TxtFechaDeuda.Text = cliente.FechaInicio.ToString("dd/MM/yyyy");
            TxtFechaLimite.Text = cliente.FechaPago.ToString("dd/MM/yyyy");

            TxtMontoDeuda.Text = cliente.MontoDeuda.ToString("C");

            if (_moraAplicada)
            {
                FilaMora.Visibility = Visibility.Visible;
                TxtMora.Text = cliente.MontoMora.ToString("C");
            }

            TxtTotal.Text = _totalPagado.ToString("C");
        }

        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void BtnExportarPdf_Click(object sender, RoutedEventArgs e)
        {
            BtnExportarPdf.IsEnabled = false;
            BtnExportarPdf.Content = "Generando...";
            await Task.Run(() => FacturaPdfService.GenerarReciboPago(_cliente));
            BtnExportarPdf.IsEnabled = true;
            BtnExportarPdf.Content = "📄 Exportar PDF";
        }
    }
}