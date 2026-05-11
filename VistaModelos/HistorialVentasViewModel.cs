using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Project_v1.Helpers;
using Project_v1.Modelos;
using Project_v1.Servicios;
using Project_v1.Vista;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Project_v1.VistaModelos
{
    public partial class HistorialVentasViewModel : ObservableObject
    {
        private List<Venta> _todasLasVentas = new List<Venta>();

        [ObservableProperty]
        private ObservableCollection<Venta> ventas = new ObservableCollection<Venta>();

        [ObservableProperty]
        private Venta ventaSeleccionada;

        [ObservableProperty]
        private ObservableCollection<DetalleVenta> detalles = new ObservableCollection<DetalleVenta>();

        [ObservableProperty]
        private ObservableCollection<DetalleServicioVenta> detallesServicio = new ObservableCollection<DetalleServicioVenta>();

        [ObservableProperty]
        private DateTime filtroFechaInicio = DateTime.Today.AddMonths(-1);

        [ObservableProperty]
        private DateTime filtroFechaFin = DateTime.Today;

        [ObservableProperty]
        private string filtroFactura = string.Empty;

        [ObservableProperty]
        private string filtroUsuario = string.Empty;

        [ObservableProperty]
        private decimal totalFiltrado = 0;

        public HistorialVentasViewModel()
        {
            CargarVentas();
        }

        // ── Carga y filtrado ──────────────────────────────────────────────────
        private void CargarVentas()
        {
            using (var db = new AppDbContext())
            {
                _todasLasVentas = db.Ventas
                    .Include(v => v.Usuario)
                    .Include(v => v.Detalles).ThenInclude(d => d.Producto)
                    .Include(v => v.DetallesServicio).ThenInclude(d => d.Servicio)
                    .OrderByDescending(v => v.Fecha)
                    .ToList();
            }
            AplicarFiltros();
        }

        private void AplicarFiltros()
        {
            var res = _todasLasVentas
                .Where(v => v.Fecha.Date >= FiltroFechaInicio.Date
                         && v.Fecha.Date <= FiltroFechaFin.Date);

            if (!string.IsNullOrWhiteSpace(FiltroFactura))
                res = res.Where(v => v.NumFactura != null &&
                    v.NumFactura.Contains(FiltroFactura, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(FiltroUsuario))
                res = res.Where(v => v.Usuario != null &&
                    v.Usuario.Nombre.Contains(FiltroUsuario, StringComparison.OrdinalIgnoreCase));

            var lista = res.ToList();
            Ventas = new ObservableCollection<Venta>(lista);
            TotalFiltrado = lista.Sum(v => v.Total);
        }

        partial void OnFiltroFechaInicioChanged(DateTime value) => AplicarFiltros();
        partial void OnFiltroFechaFinChanged(DateTime value) => AplicarFiltros();
        partial void OnFiltroFacturaChanged(string value) => AplicarFiltros();
        partial void OnFiltroUsuarioChanged(string value) => AplicarFiltros();

        partial void OnVentaSeleccionadaChanged(Venta value)
        {
            if (value == null) { Detalles.Clear(); DetallesServicio.Clear(); return; }

            using (var db = new AppDbContext())
            {
                var prods = db.DetallesVenta
                    .Include(d => d.Producto)
                    .Where(d => d.VentaId == value.Id).ToList();
                Detalles.Clear();
                foreach (var d in prods) Detalles.Add(d);

                var servs = db.DetallesServicioVenta
                    .Include(d => d.Servicio)
                    .Where(d => d.VentaId == value.Id).ToList();
                DetallesServicio.Clear();
                foreach (var s in servs) DetallesServicio.Add(s);
            }
        }

        // ── Comandos existentes ───────────────────────────────────────────────
        [RelayCommand]
        private void LimpiarFiltros()
        {
            FiltroFechaInicio = DateTime.Today.AddMonths(-1);
            FiltroFechaFin = DateTime.Today;
            FiltroFactura = string.Empty;
            FiltroUsuario = string.Empty;
        }

        [RelayCommand]
        private async Task ExportarPdf()
        {
            if (VentaSeleccionada == null)
            {
                MessageBox.Show("Selecciona una venta para exportar.",
                    "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            FacturaPdfService.GenerarYGuardar(VentaSeleccionada);
        }

        [RelayCommand]
        private async Task ExportarHistorialZip()
        {
            FacturaPdfService.ExportarHistorialZip(Ventas.ToList());
        }

        // ── Nuevos comandos de reporte ────────────────────────────────────────
        // Cada uno abre el diálogo con el tipo correspondiente y pasa TODAS
        // las ventas cargadas (el helper filtra por el rango elegido).

        [RelayCommand]
        private void ReporteDia()
        {
            AbrirDialogoYGenerar(ReporteDialogWindow.TipoReporte.Dia);
        }

        [RelayCommand]
        private void ReporteMes()
        {
            AbrirDialogoYGenerar(ReporteDialogWindow.TipoReporte.Mes);
        }

        [RelayCommand]
        private void ReporteAnio()
        {
            AbrirDialogoYGenerar(ReporteDialogWindow.TipoReporte.Anio);
        }

        private void AbrirDialogoYGenerar(ReporteDialogWindow.TipoReporte tipo)
        {
            var dlg = new ReporteDialogWindow(tipo);

            // Asignación segura del Owner
            var mainWindow = Application.Current?.MainWindow;
            if (mainWindow != null && mainWindow.IsVisible)
            {
                dlg.Owner = mainWindow;
                dlg.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }
            else
            {
                dlg.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }

            if (dlg.ShowDialog() == true && dlg.Seleccion != null)
            {
                // Pasamos _todasLasVentas para que el helper filtre por el rango elegido
                ReporteHelper.Generar(dlg.Seleccion, _todasLasVentas);
            }
        }
    }
}