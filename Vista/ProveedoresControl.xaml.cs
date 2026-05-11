using Project_v1.VistaModelos;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Project_v1.Vista
{
    public partial class ProveedoresControl : UserControl
    {
        public ProveedoresControl()
        {
            InitializeComponent();
            DataContext = new ProveedorViewModel();
        }

        private void BtnExportarHistorial_Click(object sender, RoutedEventArgs e)
            => PopupHistorial.IsOpen = !PopupHistorial.IsOpen;

        private void BtnExportarMovimientos_Click(object sender, RoutedEventArgs e)
            => PopupMovimientos.IsOpen = !PopupMovimientos.IsOpen;

        private void ExportarHistorialExcel_Click(object sender, RoutedEventArgs e)
        {
            PopupHistorial.IsOpen = false;
            if (DataContext is ProveedorViewModel vm)
                vm.ExportarHistorialExcelCommand.Execute(null);
        }

        private void ExportarHistorialPdf_Click(object sender, RoutedEventArgs e)
        {
            PopupHistorial.IsOpen = false;
            if (DataContext is ProveedorViewModel vm)
                vm.ExportarHistorialPdfCommand.Execute(null);
        }

        private void ExportarMovimientoExcel_Click(object sender, RoutedEventArgs e)
        {
            PopupMovimientos.IsOpen = false;
            if (DataContext is ProveedorViewModel vm)
                vm.ExportarMovimientoExcelCommand.Execute(null);
        }

        private void ExportarMovimientoPdf_Click(object sender, RoutedEventArgs e)
        {
            PopupMovimientos.IsOpen = false;
            if (DataContext is ProveedorViewModel vm)
                vm.ExportarMovimientoPdfCommand.Execute(null);
        }

        // Al seleccionar transacción, limpia selección de movimiento
        private void GridTransacciones_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is ProveedorViewModel vm &&
                sender is DataGrid dg && dg.SelectedItem != null)
            {
                vm.MovimientoSeleccionado = null;
            }
        }

        // Al seleccionar movimiento, limpia selección de transacción
        private void GridMovimientos_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is ProveedorViewModel vm &&
                sender is DataGrid dg && dg.SelectedItem != null)
            {
                vm.TransaccionSeleccionada = null;
            }
        }

        // Deseleccionar transacción al hacer clic en fila ya seleccionada
        private void GridTransacciones_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGrid grid &&
                e.OriginalSource is System.Windows.FrameworkElement fe &&
                fe.DataContext == grid.SelectedItem)
            {
                grid.SelectedItem = null;
            }
        }

        // Deseleccionar movimiento al hacer clic en fila ya seleccionada
        private void GridMovimientos_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGrid grid &&
                e.OriginalSource is System.Windows.FrameworkElement fe &&
                fe.DataContext == grid.SelectedItem)
            {
                grid.SelectedItem = null;
            }
        }
    }
}