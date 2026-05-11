using Project_v1.Modelos;
using Project_v1.Servicios;
using System.Collections.ObjectModel;
using System.Windows;

namespace Project_v1.Vista
{
    public partial class FacturaConfirmadaWindow : Window
    {
        private readonly Venta _venta;
        private readonly ObservableCollection<DetalleServicioVenta> _servicios;

        public FacturaConfirmadaWindow(Venta venta,
            ObservableCollection<DetalleVenta> detalles,
            ObservableCollection<DetalleServicioVenta>? servicios = null)
        {
            InitializeComponent();
            _venta = venta;
            _servicios = servicios ?? new();

            TxtNumFactura.Text = $"Factura N° {venta.NumFactura}";
            TxtFecha.Text = venta.Fecha.ToString("dd/MM/yyyy  HH:mm");
            TxtCajero.Text = Sesion.NombreUsuario;
            TxtTotal.Text = venta.Total.ToString("C");

            ListaProductos.ItemsSource = detalles;
            ListaServicios.ItemsSource = _servicios;

            // Ocultar sección servicios si no hay ninguno
            SeccionServicios.Visibility = _servicios.Any()
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void BtnCerrar_Click(object sender, RoutedEventArgs e) => Close();

        private async void BtnExportarPdf_Click(object sender, RoutedEventArgs e)
        {
            BtnExportarPdf.IsEnabled = false;
            BtnExportarPdf.Content = "Generando...";
            await Task.Run(() => FacturaPdfService.GenerarYGuardar(_venta));
            BtnExportarPdf.IsEnabled = true;
            BtnExportarPdf.Content = "📄 Exportar PDF";
        }
    }
}