using Microsoft.EntityFrameworkCore;
using Project_v1.Modelos;
using Project_v1.Servicios;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;

namespace Project_v1.Vista
{
    public partial class VentasPorFechaWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        // ── Propiedades ──
        private DateTime _fechaSeleccionada = DateTime.Today.AddDays(-1);
        public DateTime FechaSeleccionada
        {
            get => _fechaSeleccionada;
            set { _fechaSeleccionada = value; OnPropertyChanged(); }
        }

        private decimal _totalDia;
        public decimal TotalDia
        {
            get => _totalDia;
            set { _totalDia = value; OnPropertyChanged(); }
        }

        private int _cantidadVentas;
        public int CantidadVentas
        {
            get => _cantidadVentas;
            set { _cantidadVentas = value; OnPropertyChanged(); }
        }

        private int _productosVendidos;
        public int ProductosVendidos
        {
            get => _productosVendidos;
            set { _productosVendidos = value; OnPropertyChanged(); }
        }

        private int _serviciosVendidos;
        public int ServiciosVendidos
        {
            get => _serviciosVendidos;
            set { _serviciosVendidos = value; OnPropertyChanged(); }
        }

        private bool _hayDatos;
        public bool HayDatos
        {
            get => _hayDatos;
            set { _hayDatos = value; OnPropertyChanged(); }
        }

        public ObservableCollection<Venta> Ventas { get; } = new();

        public VentasPorFechaWindow()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void Consultar()
        {
            using var db = new AppDbContext();

            var ventas = db.Ventas
                .Where(v => v.Fecha.Date == FechaSeleccionada.Date)
                .OrderBy(v => v.Fecha)
                .ToList();

            Ventas.Clear();
            foreach (var v in ventas)
                Ventas.Add(v);

            var ids = ventas.Select(v => v.Id).ToList();

            TotalDia = ventas.Sum(v => v.Total);
            CantidadVentas = ventas.Count;
            ProductosVendidos = db.DetallesVenta
                .Where(d => ids.Contains(d.VentaId))
                .Sum(d => (int?)d.Cantidad) ?? 0;
            ServiciosVendidos = db.DetallesServicioVenta
                .Where(d => ids.Contains(d.VentaId))
                .Sum(d => (int?)d.Cantidad) ?? 0;

            HayDatos = ventas.Count > 0;

            if (!HayDatos)
                MessageBox.Show($"No hay ventas registradas para el {FechaSeleccionada:dd/MM/yyyy}.",
                    "Sin datos", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ConsultarCommand_Executed(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
            => Consultar();

        // ── Event handlers para los botones ──
        private void Consultar_Click(object sender, RoutedEventArgs e)
            => Consultar();

        private void ExportarExcel_Click(object sender, RoutedEventArgs e)
        {
            ExcelExportService.ExportarResumenPorFecha(
                FechaSeleccionada,
                TotalDia, CantidadVentas, ProductosVendidos,
                ServiciosVendidos, Ventas.ToList());
        }

        private void ExportarPdf_Click(object sender, RoutedEventArgs e)
        {
            FacturaPdfService.ExportarResumenPorFecha(
                FechaSeleccionada,
                TotalDia, CantidadVentas, ProductosVendidos,
                ServiciosVendidos, Ventas.ToList());
        }
    }
}