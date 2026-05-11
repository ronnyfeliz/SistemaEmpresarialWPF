using System;
using System.Windows;
using System.Windows.Controls;

namespace Project_v1.Vista
{
    /// <summary>
    /// Ventana modal que permite al usuario elegir año/mes/día y formato (Excel o PDF).
    /// Uso:
    ///   var dlg = new ReporteDialogWindow(TipoReporte.Dia) { Owner = Application.Current.MainWindow };
    ///   if (dlg.ShowDialog() == true) { var sel = dlg.Seleccion; }
    /// </summary>
    public partial class ReporteDialogWindow : Window
    {
        public enum TipoReporte { Dia, Mes, Anio }

        /// <summary>Resultado que el ViewModel lee después de ShowDialog()==true.</summary>
        public ReporteSeleccion Seleccion { get; private set; }

        private readonly TipoReporte _tipo;

        public ReporteDialogWindow(TipoReporte tipo)
        {
            InitializeComponent();
            _tipo = tipo;
            Configurar();
        }

        // ── Configuración inicial ─────────────────────────────────────────────
        private void Configurar()
        {
            int hoy = DateTime.Today.Year;

            // Título
            TxtTitulo.Text = _tipo switch
            {
                TipoReporte.Dia => "Reporte del Día",
                TipoReporte.Mes => "Reporte del Mes",
                _ => "Reporte del Año"
            };

            // Años disponibles: últimos 5 años hasta el actual
            for (int y = hoy; y >= hoy - 5; y--)
                CmbAnio.Items.Add(y);
            CmbAnio.SelectedIndex = 0;

            // Ocultar paneles según tipo
            PanelMes.Visibility = (_tipo == TipoReporte.Anio)
                ? Visibility.Collapsed : Visibility.Visible;

            PanelDia.Visibility = (_tipo == TipoReporte.Dia)
                ? Visibility.Visible : Visibility.Collapsed;

            // Preseleccionar mes actual
            CmbMes.SelectedIndex = DateTime.Today.Month - 1;

            // Poblar días iniciales
            if (_tipo == TipoReporte.Dia)
                RellenarDias(hoy, DateTime.Today.Month);
        }

        // ── Rellena el combo de días según año/mes ────────────────────────────
        private void RellenarDias(int anio, int mes)
        {
            int diaActual = CmbDia.SelectedItem is int d ? d : DateTime.Today.Day;
            CmbDia.Items.Clear();
            int total = DateTime.DaysInMonth(anio, mes);
            for (int i = 1; i <= total; i++)
                CmbDia.Items.Add(i);

            // Intentar mantener el día seleccionado o usar el actual
            CmbDia.SelectedItem = (diaActual <= total) ? diaActual : total;
        }

        private int AnioSeleccionado => CmbAnio.SelectedItem is int y ? y : DateTime.Today.Year;
        private int MesSeleccionado => CmbMes.SelectedIndex >= 0 ? CmbMes.SelectedIndex + 1 : DateTime.Today.Month;
        private int DiaSeleccionado => CmbDia.SelectedItem is int d ? d : DateTime.Today.Day;

        // ── Eventos de los combos ─────────────────────────────────────────────
        private void CmbAnio_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_tipo == TipoReporte.Dia && CmbDia != null)
                RellenarDias(AnioSeleccionado, MesSeleccionado);
        }

        private void CmbMes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_tipo == TipoReporte.Dia && CmbDia != null)
                RellenarDias(AnioSeleccionado, MesSeleccionado);
        }

        // ── Botones ───────────────────────────────────────────────────────────
        private void BtnGenerar_Click(object sender, RoutedEventArgs e)
        {
            var formato = RbExcel.IsChecked == true
                ? ReporteSeleccion.FormatoArchivo.Excel
                : ReporteSeleccion.FormatoArchivo.PDF;

            Seleccion = _tipo switch
            {
                TipoReporte.Dia => new ReporteSeleccion(formato, AnioSeleccionado, MesSeleccionado, DiaSeleccionado),
                TipoReporte.Mes => new ReporteSeleccion(formato, AnioSeleccionado, MesSeleccionado),
                _ => new ReporteSeleccion(formato, AnioSeleccionado)
            };

            DialogResult = true;
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }

    /// <summary>DTO con la selección del usuario.</summary>
    public class ReporteSeleccion
    {
        public enum FormatoArchivo { Excel, PDF }

        public FormatoArchivo Formato { get; }
        public int Anio { get; }
        public int? Mes { get; }
        public int? Dia { get; }

        /// <summary>Reporte anual.</summary>
        public ReporteSeleccion(FormatoArchivo formato, int anio)
        {
            Formato = formato; Anio = anio;
        }

        /// <summary>Reporte mensual.</summary>
        public ReporteSeleccion(FormatoArchivo formato, int anio, int mes)
        {
            Formato = formato; Anio = anio; Mes = mes;
        }

        /// <summary>Reporte diario.</summary>
        public ReporteSeleccion(FormatoArchivo formato, int anio, int mes, int dia)
        {
            Formato = formato; Anio = anio; Mes = mes; Dia = dia;
        }

        /// <summary>Rango de fechas completo para filtrar ventas.</summary>
        public (DateTime Desde, DateTime Hasta) ObtenerRango()
        {
            if (Dia.HasValue)
            {
                var d = new DateTime(Anio, Mes.Value, Dia.Value);
                return (d, d);
            }
            if (Mes.HasValue)
            {
                var desde = new DateTime(Anio, Mes.Value, 1);
                var hasta = desde.AddMonths(1).AddDays(-1);
                return (desde, hasta);
            }
            return (new DateTime(Anio, 1, 1), new DateTime(Anio, 12, 31));
        }

        /// <summary>Texto amigable para el título del reporte.</summary>
        public string ObtenerTitulo()
        {
            if (Dia.HasValue)
                return $"Reporte del Día — {new DateTime(Anio, Mes.Value, Dia.Value):dd/MM/yyyy}";
            if (Mes.HasValue)
                return $"Reporte del Mes — {new DateTime(Anio, Mes.Value, 1):MMMM yyyy}";
            return $"Reporte del Año — {Anio}";
        }

        /// <summary>Nombre sugerido para el archivo.</summary>
        public string NombreArchivo()
        {
            string ext = Formato == FormatoArchivo.Excel ? "xlsx" : "pdf";
            if (Dia.HasValue) return $"Reporte_Dia_{Anio}{Mes.Value:D2}{Dia.Value:D2}.{ext}";
            if (Mes.HasValue) return $"Reporte_Mes_{Anio}{Mes.Value:D2}.{ext}";
            return $"Reporte_Anio_{Anio}.{ext}";
        }
    }
}