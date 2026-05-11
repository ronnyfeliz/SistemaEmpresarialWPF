using Microsoft.EntityFrameworkCore;
using Project_v1.Modelos;
using Project_v1.Servicios;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;

namespace Project_v1.Vista
{
    public partial class HistorialTransaccionesProveedorWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public ObservableCollection<TransaccionProveedor> Transacciones { get; } = new();

        private TransaccionProveedor? _transaccionSeleccionada;
        public TransaccionProveedor? TransaccionSeleccionada
        {
            get => _transaccionSeleccionada;
            set
            {
                _transaccionSeleccionada = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HayTransaccionSeleccionada));
            }
        }

        public bool HayTransaccionSeleccionada => TransaccionSeleccionada != null;

        public HistorialTransaccionesProveedorWindow()
        {
            InitializeComponent();
            DataContext = this;
            CargarTransacciones();
        }

        private void CargarTransacciones()
        {
            using var db = new AppDbContext();
            var datos = db.TransaccionesProveedor
                .Include(t => t.Proveedor)
                .OrderByDescending(t => t.FechaProvision)
                .ToList();
            Transacciones.Clear();
            foreach (var item in datos)
                Transacciones.Add(item);
        }

        private void Actualizar_Click(object sender, RoutedEventArgs e)
            => CargarTransacciones();

        private void ExportarExcel_Click(object sender, RoutedEventArgs e)
            => ExcelExportService.ExportarTransaccionesProveedor(Transacciones.ToList());

        private void ExportarPdf_Click(object sender, RoutedEventArgs e)
            => FacturaPdfService.ExportarTransaccionesProveedor(Transacciones.ToList());

        private void VerDetalles_Click(object sender, RoutedEventArgs e)
        {
            if (TransaccionSeleccionada == null) return;

            string titulo = TransaccionSeleccionada.EstadoPago switch
            {
                EstadoPagoProveedor.Pagado => "COMPROBANTE DE CANCELACIÓN",
                EstadoPagoProveedor.DeudaParcial => "COMPROBANTE DE ABONO PARCIAL",
                _ => "COMPROBANTE DE CRÉDITO"
            };

            var ventana = new ComprobanteProveedorWindow(
                TransaccionSeleccionada.Proveedor?.Nombre ?? "—",
                TransaccionSeleccionada,
                TransaccionSeleccionada.MontoPagado,
                null,
                titulo);

            ventana.ShowDialog();
        }
    }
}