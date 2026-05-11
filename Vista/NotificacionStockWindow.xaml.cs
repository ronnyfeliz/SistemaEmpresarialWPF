using Project_v1.Modelos;
using Project_v1.Servicios;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Project_v1.Vista
{
    public partial class NotificacionStockWindow : Window
    {
        public NotificacionStockWindow()
        {
            InitializeComponent();
            CargarProductosStockBajo();
        }

        private void BtnExportarExcel_Click(object sender, RoutedEventArgs e)
        {
            if (TablaStock.Items.Count == 0)
            {
                MessageBox.Show("No hay productos con stock bajo para exportar.",
                    "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var listaStockBajo = TablaStock.Items.Cast<Producto>().ToList();

            // ←←← ESTA ES LA LÍNEA IMPORTANTE ←←←
            ExcelExportService.ExportarStockBajo(listaStockBajo);
        }

        private void CargarProductosStockBajo()
        {
            using var db = new AppDbContext();

            // Obtiene todos los productos activos con stock <= 10
            var productosStockBajo = db.Productos
                .Where(p => p.Activo == true && p.Stock <= 10)
                .OrderBy(p => p.Stock)   // Los más críticos (menor stock) primero
                .ToList();

            TablaStock.ItemsSource = productosStockBajo;
            TxtTotal.Text = $"Total de productos en alerta: {productosStockBajo.Count}";
        }

        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}