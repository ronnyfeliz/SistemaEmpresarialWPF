using Project_v1.Modelos;
using Project_v1.Servicios;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;

namespace Project_v1.Vista
{
    public partial class ProveedorVencimientoWindow : Window
    {
        public ObservableCollection<VencimientoProveedorItem> Transacciones { get; }

        public ProveedorVencimientoWindow(List<VencimientoProveedorItem> transacciones)
        {
            InitializeComponent();
            Transacciones = new ObservableCollection<VencimientoProveedorItem>(transacciones);
            DataContext = this;
        }

        private void ExportarExcel_Click(object sender, RoutedEventArgs e)
        {
            ExcelExportService.ExportarVencimientosProveedor(Transacciones);
        }

        private void Entendido_Click(object sender, RoutedEventArgs e) => Close();
    }
}