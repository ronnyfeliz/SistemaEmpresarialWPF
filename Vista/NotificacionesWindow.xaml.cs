using Project_v1.Modelos;
using Project_v1.Servicios;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Project_v1.Vista
{
    public partial class NotificacionesWindow : Window
    {
        private List<NotificacionResumen> _notificacionesActuales;

        public NotificacionesWindow(List<NotificacionResumen> notificaciones)
        {
            InitializeComponent();

            _notificacionesActuales = notificaciones ?? new List<NotificacionResumen>();

            GridNotificaciones.ItemsSource = _notificacionesActuales;

            int cantidad = _notificacionesActuales.Count;
            decimal montoTotal = _notificacionesActuales.Sum(n => n.Monto);

            TxtCantidadDeudas.Text = $"Total de deudas: {cantidad}";
            TxtMontoTotal.Text = montoTotal.ToString("C");
        }

        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void BtnExportarExcel_Click(object sender, RoutedEventArgs e)
        {
            if (_notificacionesActuales == null || !_notificacionesActuales.Any())
            {
                MessageBox.Show("No hay datos para exportar.",
                              "Información",
                              MessageBoxButton.OK,
                              MessageBoxImage.Information);
                return;
            }

            var clientesParaExportar = _notificacionesActuales.Select(n => new ClienteDeudor
            {
                Nombre = n.NombreCliente,
                Cedula = n.Cedula,
                Telefono = n.Telefono,
                MontoDeuda = n.Monto,
                FechaPago = n.FechaPago,
                TieneMora = false,
                MontoMora = 0,
                FechaInicio = n.FechaPago.AddMonths(-1),
                FechaPagado = null
            }).ToList();

            ExcelExportService.ExportarDeudoresNotificados(clientesParaExportar);
        }
    }

    // Clase auxiliar...
    public class NotificacionResumen
    {
        public string NombreCliente { get; set; } = string.Empty;
        public string Cedula { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public decimal Monto { get; set; }
        public DateTime FechaPago { get; set; }
        public string Estado { get; set; } = string.Empty;
    }
}