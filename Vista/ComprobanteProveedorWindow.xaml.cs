using Project_v1.Modelos;
using Project_v1.Servicios;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Project_v1.VistaModelos;
using System.Windows.Input;

namespace Project_v1.Vista
{



    public partial class ComprobanteProveedorWindow : Window
    {
        private readonly string _nombreProveedor;
        private readonly TransaccionProveedor _transaccion;
        private readonly decimal _montoMovimiento;
        private readonly decimal? _saldoAnterior;
        private readonly string _titulo;


        public ComprobanteProveedorWindow(
            string nombreProveedor,
            TransaccionProveedor transaccion,
            decimal montoMovimiento,
            decimal? saldoAnterior,
            string titulo)
        {
            InitializeComponent();

            _nombreProveedor = nombreProveedor;
            _transaccion = transaccion;
            _montoMovimiento = montoMovimiento;
            _saldoAnterior = saldoAnterior;
            _titulo = titulo;

            ConstruirVista();
        }

        private void ConstruirVista()
        {
            var ahora = DateTime.Now;
            TxtFecha.Text = ahora.ToString("dd/MM/yyyy");
            TxtHora.Text = ahora.ToString("HH:mm");
            TxtTitulo.Text = _titulo;

            // ── Sello de estado ──────────────────────────────
            bool pagado = _transaccion.SaldoPendiente <= 0;
            string selloTexto = pagado ? "✓  CANCELADO" : "⚡  ABONO PARCIAL";
            string selloColor = pagado ? "#27AE60" : "#D68910";

            SelloEstado.BorderBrush = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString(selloColor));
            TxtSello.Text = selloTexto;
            TxtSello.Foreground = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString(selloColor));

            // ── Datos del proveedor ──────────────────────────
            GridDatosProveedor.RowDefinitions.Clear();
            GridDatosProveedor.Children.Clear();

            void AgregarFila(Grid grid, string etiqueta, string valor, int fila, bool resaltar = false)
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var lblEtiqueta = new TextBlock
                {
                    Text = etiqueta,
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x7F, 0x8C, 0x8D)),
                    Margin = new Thickness(0, 3, 0, 3)
                };
                Grid.SetRow(lblEtiqueta, fila);
                Grid.SetColumn(lblEtiqueta, 0);
                grid.Children.Add(lblEtiqueta);

                var lblValor = new TextBlock
                {
                    Text = valor,
                    FontSize = 11,
                    FontWeight = resaltar ? FontWeights.Bold : FontWeights.Normal,
                    Foreground = resaltar
                        ? new SolidColorBrush((Color)ColorConverter.ConvertFromString(selloColor))
                        : new SolidColorBrush(Color.FromRgb(0x26, 0x36, 0x45)),
                    Margin = new Thickness(0, 3, 0, 3)
                };
                Grid.SetRow(lblValor, fila);
                Grid.SetColumn(lblValor, 1);
                grid.Children.Add(lblValor);
            }

            AgregarFila(GridDatosProveedor, "Proveedor", _nombreProveedor, 0);
            AgregarFila(GridDatosProveedor, "ID transacción", _transaccion.Id.ToString(), 1);
            AgregarFila(GridDatosProveedor, "Fecha suministro", _transaccion.FechaProvision.ToString("dd/MM/yyyy"), 2);
            AgregarFila(GridDatosProveedor, "Cantidad", _transaccion.CantidadMercancia.ToString(), 3);
            if (_transaccion.FechaLimitePago.HasValue)
                AgregarFila(GridDatosProveedor, "Fecha límite", _transaccion.FechaLimitePago.Value.ToString("dd/MM/yyyy"), 4);

            // ── Desglose ─────────────────────────────────────
            PanelDesglose.Children.Clear();

            void AgregarLinea(string concepto, string monto, string? colorMonto = null)
            {
                var grid = new Grid { Margin = new Thickness(0, 4, 0, 4) };
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                grid.Children.Add(new TextBlock { Text = concepto, FontSize = 12 });

                var lblMonto = new TextBlock
                {
                    Text = monto,
                    FontSize = 12,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = colorMonto != null
                        ? new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorMonto))
                        : new SolidColorBrush(Colors.Black)
                };
                Grid.SetColumn(lblMonto, 1);
                grid.Children.Add(lblMonto);
                PanelDesglose.Children.Add(grid);
            }

            AgregarLinea("Total original", _transaccion.MontoTotal.ToString("C"));

            if (_saldoAnterior.HasValue)
                AgregarLinea("Saldo anterior", _saldoAnterior.Value.ToString("C"));

            AgregarLinea("Monto de este pago", _montoMovimiento.ToString("C"), "#1A2E4A");

            PanelDesglose.Children.Add(new Separator
            {
                Margin = new Thickness(0, 8, 0, 8),
                Background = new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE0))
            });

            AgregarLinea("SALDO RESTANTE", _transaccion.SaldoPendiente.ToString("C"),
                pagado ? "#27AE60" : "#D68910");

            if (!string.IsNullOrWhiteSpace(_transaccion.Descripcion))
            {
                PanelDesglose.Children.Add(new TextBlock
                {
                    Text = _transaccion.Descripcion,
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x7F, 0x8C, 0x8D)),
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 8, 0, 0)
                });
            }
        }

        private void BtnExportarExcel_Click(object sender, RoutedEventArgs e)
        {
            ExcelExportService.GenerarComprobanteProveedor(
                _nombreProveedor,
                _transaccion,
                _montoMovimiento,
                _saldoAnterior,
                _titulo);
        }


        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
            => Close();

        private void BtnExportarPdf_Click(object sender, RoutedEventArgs e)
        {
            FacturaPdfService.GenerarComprobanteProveedor(
                _nombreProveedor,
                _transaccion,
                _montoMovimiento,
                _saldoAnterior,
                _titulo);
        }
    }
}