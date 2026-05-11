using Microsoft.EntityFrameworkCore;
using Project_v1.Modelos;
using Project_v1.Servicios;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Project_v1.Vista
{
    public partial class HistorialPagadosWindow : Window
    {
        private List<ClienteDeudor> _todos = new();
        private ClienteDeudor? _seleccionado;

        public HistorialPagadosWindow()
        {
            InitializeComponent();
            CargarDatos();
        }

        private void CargarDatos()
        {
            using var db = new AppDbContext();
            _todos = db.ClientesDeudores
                       .Where(c => c.Pagado == true)
                       .OrderByDescending(c => c.FechaPagado)
                       .ToList();

            AplicarFiltros();
        }

        private void AplicarFiltros()
        {
            var resultado = _todos.AsEnumerable();

            var texto = TxtBuscarNombre.Text.Trim();
            if (!string.IsNullOrWhiteSpace(texto))
            {
                resultado = resultado.Where(c =>
                    c.Nombre.Contains(texto, StringComparison.OrdinalIgnoreCase) ||
                    c.Cedula.Contains(texto, StringComparison.OrdinalIgnoreCase));
            }

            if (FiltroFecha.SelectedDate.HasValue)
            {
                resultado = resultado.Where(c =>
                    c.FechaPagado.HasValue &&
                    c.FechaPagado.Value.Date == FiltroFecha.SelectedDate.Value.Date);
            }

            var lista = resultado.ToList();
            GridPagados.ItemsSource = lista;

            int count = lista.Count;
            decimal totalRecuperado = lista.Sum(c =>
            {
                bool mora = c.TieneMora &&
                            c.FechaPagado.HasValue &&
                            c.FechaPagado.Value.Date > c.FechaPago.Date;

                return c.MontoDeuda + (mora ? c.MontoMora : 0);
            });

            TxtConteo.Text = $"{count} cliente(s) encontrado(s)";
            TxtTotalPagado.Text = $"Total recuperado: {totalRecuperado:C}";
            TxtPie.Text = $"{count} registros  •  Total recuperado: {totalRecuperado:C}";

            // Si la lista tiene elementos, se selecciona el primero automáticamente
            // para que el panel derecho y los botones tengan contexto válido.
            if (lista.Count > 0)
            {
                GridPagados.SelectedIndex = 0;
            }
            else
            {
                GridPagados.SelectedItem = null;
                _seleccionado = null;
                LimpiarDetalle();
            }

            ActualizarEstadoBotones();
        }

        private void BtnExportarExcel_Click(object sender, RoutedEventArgs e)
        {
            var lista = (GridPagados.ItemsSource as List<ClienteDeudor>) ?? _todos;

            if (lista.Count == 0)
            {
                MessageBox.Show("No hay registros para exportar.",
                    "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            ExcelExportService.ExportarHistorialPagados(lista);
        }

        private void BtnExportarPdfLista_Click(object sender, RoutedEventArgs e)
        {
            // Se exporta la lista filtrada visible actualmente.
            // Si no hay filtro aplicado, se usa toda la colección de pagados.
            var lista = (GridPagados.ItemsSource as List<ClienteDeudor>) ?? _todos;

            if (lista.Count == 0)
            {
                MessageBox.Show("No hay registros para exportar.",
                    "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            FacturaPdfService.ExportarDeudoresPagados(lista);
        }

        private void Filtrar(object sender, object e) => AplicarFiltros();

        private void LimpiarFiltros(object sender, RoutedEventArgs e)
        {
            TxtBuscarNombre.Text = string.Empty;
            FiltroFecha.SelectedDate = null;
        }

        private void GridPagados_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _seleccionado = GridPagados.SelectedItem as ClienteDeudor;

            if (_seleccionado == null)
            {
                LimpiarDetalle();
                ActualizarEstadoBotones();
                return;
            }

            bool moraAplicada = _seleccionado.TieneMora &&
                                _seleccionado.FechaPagado.HasValue &&
                                _seleccionado.FechaPagado.Value.Date > _seleccionado.FechaPago.Date;

            decimal total = _seleccionado.MontoDeuda + (moraAplicada ? _seleccionado.MontoMora : 0);

            DetNombre.Text = _seleccionado.Nombre;
            DetCedula.Text = $"Cédula: {_seleccionado.Cedula}";
            DetTelefono.Text = $"Tel: {_seleccionado.Telefono}";
            DetFechaInicio.Text = _seleccionado.FechaInicio.ToString("dd/MM/yyyy");
            DetFechaLimite.Text = _seleccionado.FechaPago.ToString("dd/MM/yyyy");
            DetFechaPagado.Text = _seleccionado.FechaPagado?.ToString("dd/MM/yyyy") ?? "-";
            DetMonto.Text = _seleccionado.MontoDeuda.ToString("C");
            DetTotal.Text = total.ToString("C");

            if (_seleccionado.FechaPagado.HasValue)
            {
                int dias = (_seleccionado.FechaPagado.Value.Date - _seleccionado.FechaInicio.Date).Days;
                DetDias.Text = $"{dias} día(s)";
            }
            else
            {
                DetDias.Text = "-";
            }

            if (moraAplicada)
            {
                DetFilaMora.Visibility = Visibility.Visible;
                DetMora.Text = _seleccionado.MontoMora.ToString("C");
            }
            else
            {
                DetFilaMora.Visibility = Visibility.Collapsed;
                DetMora.Text = string.Empty;
            }

            ActualizarEstadoBotones();
        }

        private void BtnVerNotificaciones_Click(object sender, RoutedEventArgs e)
        {
            if (_seleccionado == null)
            {
                MessageBox.Show("Selecciona un cliente pagado primero.",
                    "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var ventana = new HistorialClienteWindow(_seleccionado);
            ventana.ShowDialog();
        }

        private void BtnExportarPdf_Click(object sender, RoutedEventArgs e)
        {
            if (_seleccionado == null)
            {
                MessageBox.Show("Selecciona un cliente pagado primero.",
                    "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Este método abre un SaveFileDialog, por lo que debe ejecutarse
            // en el hilo principal de la interfaz y no dentro de Task.Run.
            BtnExportarPdf.IsEnabled = false;
            BtnExportarPdf.Content = "Generando...";

            try
            {
                FacturaPdfService.GenerarReciboPago(_seleccionado);
            }
            finally
            {
                BtnExportarPdf.IsEnabled = true;
                BtnExportarPdf.Content = "📄 Exportar Recibo PDF";
            }
        }

        private async void BtnExportarZip_Click(object sender, RoutedEventArgs e)
        {
            if (_todos.Count == 0)
            {
                MessageBox.Show("No hay recibos para exportar.",
                    "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                FileName = $"Recibos_Pagados_{DateTime.Now:yyyyMMdd}",
                DefaultExt = ".zip",
                Filter = "Zip files (.zip)|*.zip"
            };

            if (dialog.ShowDialog() != true) return;

            BtnExportarZip.IsEnabled = false;
            BtnExportarZip.Content = "Generando...";

            var listaActual = (GridPagados.ItemsSource as List<ClienteDeudor>) ?? _todos;

            await System.Threading.Tasks.Task.Run(() =>
            {
                QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

                using var db = new AppDbContext();
                var negocio = db.Negocios.FirstOrDefault();

                using var zipStream = new FileStream(dialog.FileName, FileMode.Create);
                using var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, true);

                foreach (var cliente in listaActual)
                {
                    // Organiza por año/mes/día de cuando pagó.
                    var fecha = cliente.FechaPagado ?? cliente.FechaInicio;
                    string rutaEntry =
                        $"Recibos_Pagados/{fecha:yyyy}/{fecha:MM}/{fecha:dd}/" +
                        $"Recibo_{cliente.Nombre.Replace(" ", "_")}_{fecha:yyyyMMdd}.pdf";

                    bool moraAplicada = cliente.TieneMora &&
                                        cliente.FechaPagado.HasValue &&
                                        cliente.FechaPagado.Value.Date > cliente.FechaPago.Date;

                    decimal totalPagado = cliente.MontoDeuda + (moraAplicada ? cliente.MontoMora : 0);

                    var entry = archive.CreateEntry(rutaEntry);
                    using var entryStream = entry.Open();
                    FacturaPdfService.ComponerRecibo(cliente, negocio, moraAplicada, totalPagado, entryStream);
                }

                string? folder = Path.GetDirectoryName(dialog.FileName);
                if (folder != null)
                    System.Diagnostics.Process.Start("explorer.exe", folder);
            });

            BtnExportarZip.IsEnabled = true;
            BtnExportarZip.Content = "📦 Exportar ZIP";
        }

        private void LimpiarDetalle()
        {
            DetNombre.Text = string.Empty;
            DetCedula.Text = string.Empty;
            DetTelefono.Text = string.Empty;
            DetFechaInicio.Text = string.Empty;
            DetFechaLimite.Text = string.Empty;
            DetFechaPagado.Text = string.Empty;
            DetDias.Text = string.Empty;
            DetMonto.Text = string.Empty;
            DetMora.Text = string.Empty;
            DetTotal.Text = string.Empty;
            DetFilaMora.Visibility = Visibility.Collapsed;
        }

        private void ActualizarEstadoBotones()
        {
            bool haySeleccion = _seleccionado != null;
            BtnVerNotificaciones.IsEnabled = haySeleccion;
            BtnExportarPdf.IsEnabled = haySeleccion;
        }

        private void BtnCerrar_Click(object sender, RoutedEventArgs e) => Close();
    }
}