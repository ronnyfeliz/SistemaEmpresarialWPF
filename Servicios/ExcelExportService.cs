using ClosedXML.Excel;
using Project_v1.Modelos;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Win32;   // ← Importante para SaveFileDialog

namespace Project_v1.Servicios
{
    public static class ExcelExportService
    {

        // ═══════════════════════════════════════════════════
        // GENERAR COMPROBANTE INDIVIDUAL DE PROVEEDOR
        // ═══════════════════════════════════════════════════
        public static void GenerarComprobanteProveedor(
            string proveedor,
            TransaccionProveedor transaccion,
            decimal montoMovimiento,
            decimal? saldoAnterior,
            string titulo)
        {
            var dialog = new SaveFileDialog
            {
                FileName = $"Comprobante_{proveedor.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmmss}",
                DefaultExt = ".xlsx",
                Filter = "Excel files (.xlsx)|*.xlsx"
            };
            if (dialog.ShowDialog() != true) return;

            try
            {
                using var wb = new XLWorkbook();
                var ws = wb.Worksheets.Add("Comprobante");

                // ── Título ──────────────────────────────────
                ws.Cell("A1").Value = titulo;
                ws.Range("A1:B1").Merge();
                ws.Cell("A1").Style
                    .Font.SetBold(true).Font.SetFontSize(14)
                    .Font.SetFontColor(XLColor.White)
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#1A2E4A"))
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                ws.Cell("A2").Value = $"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}";
                ws.Range("A2:B2").Merge();
                ws.Cell("A2").Style
                    .Font.SetFontSize(9).Font.SetFontColor(XLColor.Gray)
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                // ── Datos ────────────────────────────────────
                int row = 4;

                void Fila(string etiqueta, string valor, string? colorValor = null)
                {
                    ws.Cell(row, 1).Value = etiqueta;
                    ws.Cell(row, 1).Style.Font.SetFontColor(XLColor.FromHtml("#64748B"));

                    ws.Cell(row, 2).Value = valor;
                    ws.Cell(row, 2).Style.Font.SetBold(true);
                    if (colorValor != null)
                        ws.Cell(row, 2).Style.Font.SetFontColor(XLColor.FromHtml(colorValor));

                    var bg = row % 2 == 0 ? XLColor.FromHtml("#F8F9FA") : XLColor.White;
                    ws.Range(row, 1, row, 2).Style
                        .Fill.SetBackgroundColor(bg)
                        .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                        .Border.SetInsideBorder(XLBorderStyleValues.Thin);
                    row++;
                }

                bool pagado = transaccion.SaldoPendiente <= 0;
                string colorEstado = pagado ? "#27AE60" : "#D68910";

                Fila("Proveedor", proveedor);
                Fila("ID transacción", transaccion.Id.ToString());
                Fila("Fecha suministro", transaccion.FechaProvision.ToString("dd/MM/yyyy"));
                Fila("Cantidad", transaccion.CantidadMercancia.ToString());
                Fila("Total original", transaccion.MontoTotal.ToString("C"));

                if (saldoAnterior.HasValue)
                    Fila("Saldo anterior", saldoAnterior.Value.ToString("C"));

                Fila("Monto de este pago", montoMovimiento.ToString("C"), "#1A2E4A");

                if (transaccion.FechaLimitePago.HasValue)
                    Fila("Fecha límite", transaccion.FechaLimitePago.Value.ToString("dd/MM/yyyy"));

                Fila("Estado", transaccion.EstadoPago.ToString(), colorEstado);

                // ── Saldo restante destacado ─────────────────
                ws.Cell(row, 1).Value = "SALDO RESTANTE";
                ws.Cell(row, 1).Style.Font.SetBold(true);
                ws.Cell(row, 2).Value = transaccion.SaldoPendiente.ToString("C");
                ws.Cell(row, 2).Style.Font.SetBold(true).Font.SetFontColor(XLColor.FromHtml(colorEstado));
                ws.Range(row, 1, row, 2).Style
                    .Fill.SetBackgroundColor(pagado
                        ? XLColor.FromHtml("#EAFAF1")
                        : XLColor.FromHtml("#FEF9E7"))
                    .Border.SetOutsideBorder(XLBorderStyleValues.Medium);
                row++;

                // ── Descripción si existe ────────────────────
                if (!string.IsNullOrWhiteSpace(transaccion.Descripcion))
                {
                    row++;
                    ws.Cell(row, 1).Value = "Descripción";
                    ws.Cell(row, 1).Style.Font.SetFontColor(XLColor.FromHtml("#64748B"));
                    ws.Cell(row, 2).Value = transaccion.Descripcion;
                    ws.Range(row, 1, row, 2).Style
                        .Fill.SetBackgroundColor(XLColor.FromHtml("#F8FAFC"))
                        .Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                }

                ws.Column(1).Width = 22;
                ws.Column(2).AdjustToContents();
                wb.SaveAs(dialog.FileName);
                AbrirArchivo(dialog.FileName);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error al exportar comprobante: {ex.Message}",
                    "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }


        // ═══════════════════════════════════════════════════
        // 1. EXPORTAR CLIENTES DEUDORES ACTIVOS
        // ═══════════════════════════════════════════════════
        public static void ExportarDeudoresActivos(List<ClienteDeudor> clientes)
        {
            var dialog = new SaveFileDialog
            {
                FileName = $"Clientes_Deudores_{DateTime.Now:yyyyMMdd}",
                DefaultExt = ".xlsx",
                Filter = "Excel files (.xlsx)|*.xlsx"
            };

            if (dialog.ShowDialog() != true) return;

            try
            {
                using var wb = new XLWorkbook();
                var ws = wb.Worksheets.Add("Clientes Deudores");

                // Título
                ws.Cell("A1").Value = "CLIENTES DEUDORES ACTIVOS";
                ws.Range("A1:I1").Merge();
                ws.Cell("A1").Style.Font.SetBold(true).Font.SetFontSize(14)
                    .Font.SetFontColor(XLColor.White)
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#1A2E4A"))
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                ws.Cell("A2").Value = $"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}";
                ws.Range("A2:I2").Merge();
                ws.Cell("A2").Style.Font.SetFontSize(9).Font.SetFontColor(XLColor.Gray)
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                // Encabezados
                var headers = new[] { "Nombre", "Cédula", "Teléfono", "Deuda", "Mora", "Total", "F. Inicio", "F. Límite", "F. Pago" };
                for (int i = 0; i < headers.Length; i++)
                {
                    var cell = ws.Cell(4, i + 1);
                    cell.Value = headers[i];
                    cell.Style.Font.SetBold(true).Font.SetFontColor(XLColor.White)
                        .Fill.SetBackgroundColor(XLColor.FromHtml("#1A2E4A"))
                        .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                        .Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                }

                // Datos
                int row = 5;
                decimal totalGeneral = 0;

                foreach (var c in clientes)
                {
                    bool vencido = DateTime.Today > c.FechaPago;
                    bool moraAplicada = c.TieneMora && vencido;
                    decimal total = c.MontoDeuda + (moraAplicada ? c.MontoMora : 0);
                    totalGeneral += total;

                    var bgRow = row % 2 == 0 ? XLColor.FromHtml("#F8F9FA") : XLColor.White;

                    ws.Cell(row, 1).Value = c.Nombre;
                    ws.Cell(row, 2).Value = c.Cedula;
                    ws.Cell(row, 3).Value = c.Telefono;
                    ws.Cell(row, 4).Value = c.MontoDeuda;
                    ws.Cell(row, 4).Style.NumberFormat.Format = "$#,##0.00";

                    ws.Cell(row, 5).Value = moraAplicada ? c.MontoMora : 0;
                    ws.Cell(row, 5).Style.NumberFormat.Format = "$#,##0.00";
                    if (moraAplicada) ws.Cell(row, 5).Style.Font.SetFontColor(XLColor.Red);

                    ws.Cell(row, 6).Value = total;
                    ws.Cell(row, 6).Style.NumberFormat.Format = "$#,##0.00";
                    ws.Cell(row, 6).Style.Font.SetBold(true).Font.SetFontColor(XLColor.FromHtml("#1A6B3C"));

                    ws.Cell(row, 7).Value = c.FechaInicio.ToString("dd/MM/yyyy");
                    ws.Cell(row, 8).Value = c.FechaPago.ToString("dd/MM/yyyy");
                    ws.Cell(row, 9).Value = c.FechaPagado?.ToString("dd/MM/yyyy") ?? "-";

                    ws.Range(row, 1, row, 9).Style
                        .Fill.SetBackgroundColor(bgRow)
                        .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                        .Border.SetInsideBorder(XLBorderStyleValues.Thin);
                    row++;
                }

                // Totales
                ws.Cell(row, 1).Value = $"Total clientes: {clientes.Count}";
                ws.Range(row, 1, row, 5).Merge();
                ws.Cell(row, 6).Value = totalGeneral;
                ws.Cell(row, 6).Style.NumberFormat.Format = "$#,##0.00";
                ws.Cell(row, 6).Style.Font.SetBold(true).Font.SetFontColor(XLColor.FromHtml("#1A6B3C"));

                ws.Range(row, 1, row, 9).Style
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#EAFAF1"))
                    .Border.SetOutsideBorder(XLBorderStyleValues.Medium);

                ws.Columns().AdjustToContents();
                wb.SaveAs(dialog.FileName);
                AbrirArchivo(dialog.FileName);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error al exportar Excel: {ex.Message}");
            }
        }

        // ═══════════════════════════════════════════════════
        // 2. EXPORTAR CATÁLOGO DE SERVICIOS
        // ═══════════════════════════════════════════════════
        public static void ExportarServicios(List<Servicio> servicios)
        {
            var dialog = new SaveFileDialog
            {
                FileName = $"Catalogo_Servicios_{DateTime.Now:yyyyMMdd}",
                DefaultExt = ".xlsx",
                Filter = "Excel files (.xlsx)|*.xlsx"
            };
            if (dialog.ShowDialog() != true) return;

            try
            {
                using var wb = new XLWorkbook();
                var ws = wb.Worksheets.Add("Catálogo de Servicios");

                // ── Título ──────────────────────────────────────────
                ws.Cell("A1").Value = "CATÁLOGO DE SERVICIOS";
                ws.Range("A1:F1").Merge();
                ws.Cell("A1").Style
                    .Font.SetBold(true).Font.SetFontSize(14)
                    .Font.SetFontColor(XLColor.White)
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#1A5276"))
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                ws.Cell("A2").Value = $"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}";
                ws.Range("A2:F2").Merge();
                ws.Cell("A2").Style
                    .Font.SetFontSize(9).Font.SetFontColor(XLColor.Gray)
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                // ── Encabezados ─────────────────────────────────────
                var headers = new[] { "ID", "Nombre", "Categoría", "Descripción", "Precio", "Estado" };
                for (int i = 0; i < headers.Length; i++)
                {
                    var cell = ws.Cell(4, i + 1);
                    cell.Value = headers[i];
                    cell.Style
                        .Font.SetBold(true).Font.SetFontColor(XLColor.White)
                        .Fill.SetBackgroundColor(XLColor.FromHtml("#1A5276"))
                        .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                        .Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                }

                // ── Filas de datos ──────────────────────────────────
                int row = 5;
                foreach (var s in servicios)
                {
                    var bg = row % 2 == 0 ? XLColor.FromHtml("#F8F9FA") : XLColor.White;

                    ws.Cell(row, 1).Value = s.Id;
                    ws.Cell(row, 2).Value = s.Nombre;
                    ws.Cell(row, 3).Value = s.Categoria;
                    ws.Cell(row, 4).Value = s.Descripcion;
                    ws.Cell(row, 5).Value = s.Precio;
                    ws.Cell(row, 5).Style.NumberFormat.Format = "$#,##0.00";
                    ws.Cell(row, 6).Value = s.Activo ? "Activo" : "Inactivo";
                    ws.Cell(row, 6).Style.Font.SetBold(true);
                    ws.Cell(row, 6).Style.Font.SetFontColor(s.Activo
                        ? XLColor.FromHtml("#27AE60")
                        : XLColor.FromHtml("#E74C3C"));

                    ws.Range(row, 1, row, 6).Style
                        .Fill.SetBackgroundColor(bg)
                        .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                        .Border.SetInsideBorder(XLBorderStyleValues.Thin);

                    row++;
                }

                // ── Fila de totales ─────────────────────────────────
                int activos = servicios.Count(s => s.Activo);
                int inactivos = servicios.Count(s => !s.Activo);

                ws.Cell(row, 1).Value = $"Total: {servicios.Count}  |  Activos: {activos}  |  Inactivos: {inactivos}";
                ws.Range(row, 1, row, 6).Merge();
                ws.Cell(row, 1).Style
                    .Font.SetBold(true).Font.SetFontColor(XLColor.FromHtml("#1A5276"))
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#EBF5FB"))
                    .Border.SetOutsideBorder(XLBorderStyleValues.Medium)
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left);

                // ── Ajuste de columnas ──────────────────────────────
                ws.Columns().AdjustToContents();
                wb.SaveAs(dialog.FileName);
                AbrirArchivo(dialog.FileName);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error al exportar Excel: {ex.Message}",
                    "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        // ═══════════════════════════════════════════════════
        // 3. EXPORTAR INVENTARIO (usado también para Stock Bajo)
        // ═══════════════════════════════════════════════════
        public static void ExportarInventario(List<Producto> productos)
        {
            var dialog = new SaveFileDialog
            {
                FileName = $"Inventario_{DateTime.Now:yyyyMMdd}",
                DefaultExt = ".xlsx",
                Filter = "Excel files (.xlsx)|*.xlsx"
            };
            if (dialog.ShowDialog() != true) return;

            try
            {
                using var wb = new XLWorkbook();
                var ws = wb.Worksheets.Add("Inventario");

                ws.Cell("A1").Value = "INVENTARIO DE PRODUCTOS";
                ws.Range("A1:H1").Merge();
                ws.Cell("A1").Style.Font.SetBold(true).Font.SetFontSize(14)
                    .Font.SetFontColor(XLColor.White)
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#1A2E4A"))
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                ws.Cell("A2").Value = $"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}";
                ws.Range("A2:H2").Merge();
                ws.Cell("A2").Style.Font.SetFontSize(9).Font.SetFontColor(XLColor.Gray)
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                var headers = new[] { "ID", "Nombre", "Marca", "Descripción", "Precio", "Stock", "Stock Mín.", "Estado" };
                for (int i = 0; i < headers.Length; i++)
                {
                    var cell = ws.Cell(4, i + 1);
                    cell.Value = headers[i];
                    cell.Style.Font.SetBold(true).Font.SetFontColor(XLColor.White)
                        .Fill.SetBackgroundColor(XLColor.FromHtml("#1A2E4A"))
                        .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                        .Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                }

                int row = 5;
                foreach (var p in productos)
                {
                    bool stockBajo = p.Stock <= 10;
                    var bgRow = row % 2 == 0 ? XLColor.FromHtml("#F8F9FA") : XLColor.White;

                    ws.Cell(row, 1).Value = p.Id;
                    ws.Cell(row, 2).Value = p.Nombre;
                    ws.Cell(row, 3).Value = p.Marca;
                    ws.Cell(row, 4).Value = p.Descripcion;
                    ws.Cell(row, 5).Value = p.Precio;
                    ws.Cell(row, 5).Style.NumberFormat.Format = "$#,##0.00";
                    ws.Cell(row, 6).Value = p.Stock;
                    ws.Cell(row, 7).Value = 10;
                    ws.Cell(row, 8).Value = !p.Activo ? "Inactivo" : stockBajo ? "Stock bajo" : "OK";

                    if (!p.Activo)
                        ws.Cell(row, 8).Style.Font.SetFontColor(XLColor.Gray);
                    else if (stockBajo)
                    {
                        ws.Cell(row, 6).Style.Font.SetFontColor(XLColor.Red).Font.SetBold(true);
                        ws.Cell(row, 8).Style.Font.SetFontColor(XLColor.Orange).Font.SetBold(true);
                    }
                    else
                        ws.Cell(row, 8).Style.Font.SetFontColor(XLColor.FromHtml("#27AE60")).Font.SetBold(true);

                    ws.Range(row, 1, row, 8).Style
                        .Fill.SetBackgroundColor(bgRow)
                        .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                        .Border.SetInsideBorder(XLBorderStyleValues.Thin);
                    row++;
                }

                // Totales
                ws.Cell(row, 1).Value = $"Total productos: {productos.Count}";
                ws.Range(row, 1, row, 4).Merge();
                ws.Cell(row, 5).Value = "Valor total inventario:";
                ws.Cell(row, 5).Style.Font.SetBold(true);
                ws.Cell(row, 6).FormulaA1 = $"=SUMPRODUCT(E5:E{row - 1}, F5:F{row - 1})";
                ws.Cell(row, 6).Style.NumberFormat.Format = "$#,##0.00";
                ws.Cell(row, 6).Style.Font.SetBold(true).Font.SetFontColor(XLColor.FromHtml("#27AE60"));

                ws.Range(row, 1, row, 8).Style
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#F0F4F8"))
                    .Border.SetOutsideBorder(XLBorderStyleValues.Medium);

                ws.Columns().AdjustToContents();
                wb.SaveAs(dialog.FileName);
                AbrirArchivo(dialog.FileName);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error al exportar Excel: {ex.Message}");
            }
        }

        // ═══════════════════════════════════════════════════
        // 4. EXPORTAR HISTORIAL DE CLIENTES PAGADOS
        // ═══════════════════════════════════════════════════
        public static void ExportarHistorialPagados(List<ClienteDeudor> clientes)
        {
            var dialog = new SaveFileDialog
            {
                FileName = $"Historial_Pagados_{DateTime.Now:yyyyMMdd}",
                DefaultExt = ".xlsx",
                Filter = "Excel files (.xlsx)|*.xlsx"
            };

            if (dialog.ShowDialog() != true) return;

            try
            {
                using var wb = new XLWorkbook();
                var ws = wb.Worksheets.Add("Clientes Pagados");

                // ── Título ──────────────────────────────────────────
                ws.Cell("A1").Value = "HISTORIAL DE CLIENTES PAGADOS";
                ws.Range("A1:I1").Merge();
                ws.Cell("A1").Style
                    .Font.SetBold(true).Font.SetFontSize(14)
                    .Font.SetFontColor(XLColor.White)
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#1A6B3C"))
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                ws.Cell("A2").Value = $"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}";
                ws.Range("A2:I2").Merge();
                ws.Cell("A2").Style
                    .Font.SetFontSize(9).Font.SetFontColor(XLColor.Gray)
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                // ── Encabezados ─────────────────────────────────────
                var headers = new[] { "Nombre", "Cédula", "Teléfono", "Deuda", "Mora", "Total Pagado", "F. Inicio", "F. Límite", "F. Pago" };
                for (int i = 0; i < headers.Length; i++)
                {
                    var cell = ws.Cell(4, i + 1);
                    cell.Value = headers[i];
                    cell.Style
                        .Font.SetBold(true).Font.SetFontColor(XLColor.White)
                        .Fill.SetBackgroundColor(XLColor.FromHtml("#1A6B3C"))
                        .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                        .Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                }

                // ── Datos ───────────────────────────────────────────
                int row = 5;
                decimal totalRecuperado = 0;

                foreach (var c in clientes)
                {
                    bool moraAplicada = c.TieneMora &&
                                        c.FechaPagado.HasValue &&
                                        c.FechaPagado.Value.Date > c.FechaPago.Date;

                    decimal total = c.MontoDeuda + (moraAplicada ? c.MontoMora : 0);
                    totalRecuperado += total;

                    var bgRow = row % 2 == 0 ? XLColor.FromHtml("#F8F9FA") : XLColor.White;

                    ws.Cell(row, 1).Value = c.Nombre;
                    ws.Cell(row, 2).Value = c.Cedula;
                    ws.Cell(row, 3).Value = c.Telefono;

                    ws.Cell(row, 4).Value = c.MontoDeuda;
                    ws.Cell(row, 4).Style.NumberFormat.Format = "$#,##0.00";

                    ws.Cell(row, 5).Value = moraAplicada ? c.MontoMora : 0;
                    ws.Cell(row, 5).Style.NumberFormat.Format = "$#,##0.00";
                    if (moraAplicada)
                        ws.Cell(row, 5).Style.Font.SetFontColor(XLColor.Red);

                    ws.Cell(row, 6).Value = total;
                    ws.Cell(row, 6).Style.NumberFormat.Format = "$#,##0.00";
                    ws.Cell(row, 6).Style.Font.SetBold(true).Font.SetFontColor(XLColor.FromHtml("#1A6B3C"));

                    ws.Cell(row, 7).Value = c.FechaInicio.ToString("dd/MM/yyyy");
                    ws.Cell(row, 8).Value = c.FechaPago.ToString("dd/MM/yyyy");
                    ws.Cell(row, 9).Value = c.FechaPagado?.ToString("dd/MM/yyyy") ?? "-";

                    ws.Range(row, 1, row, 9).Style
                        .Fill.SetBackgroundColor(bgRow)
                        .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                        .Border.SetInsideBorder(XLBorderStyleValues.Thin);

                    row++;
                }

                // ── Fila de totales ─────────────────────────────────
                ws.Cell(row, 1).Value = $"Total clientes pagados: {clientes.Count}";
                ws.Range(row, 1, row, 5).Merge();
                ws.Cell(row, 1).Style.Font.SetBold(true);

                ws.Cell(row, 6).Value = totalRecuperado;
                ws.Cell(row, 6).Style.NumberFormat.Format = "$#,##0.00";
                ws.Cell(row, 6).Style.Font.SetBold(true).Font.SetFontColor(XLColor.FromHtml("#1A6B3C"));

                ws.Range(row, 1, row, 9).Style
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#EAFAF1"))
                    .Border.SetOutsideBorder(XLBorderStyleValues.Medium);

                ws.Columns().AdjustToContents();
                wb.SaveAs(dialog.FileName);
                AbrirArchivo(dialog.FileName);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error al exportar Excel: {ex.Message}",
                    "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        // ═══════════════════════════════════════════════════
        // 5. EXPORTAR SOLO PRODUCTOS CON STOCK BAJO
        // ═══════════════════════════════════════════════════
        public static void ExportarStockBajo(List<Producto> productos)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                FileName = $"Stock_Bajo_{DateTime.Now:yyyyMMdd_HHmm}",
                DefaultExt = ".xlsx",
                Filter = "Excel files (.xlsx)|*.xlsx"
            };

            if (dialog.ShowDialog() != true) return;

            try
            {
                using var wb = new XLWorkbook();
                var ws = wb.Worksheets.Add("Stock Bajo");

                // Título específico
                ws.Cell("A1").Value = "PRODUCTOS CON STOCK BAJO (≤ 10 unidades)";
                ws.Range("A1:G1").Merge();
                ws.Cell("A1").Style.Font.SetBold(true).Font.SetFontSize(14)
                    .Font.SetFontColor(XLColor.White)
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#E67E22"))
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                ws.Cell("A2").Value = $"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}";
                ws.Range("A2:G2").Merge();
                ws.Cell("A2").Style.Font.SetFontSize(9).Font.SetFontColor(XLColor.Gray)
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                // Encabezados (7 columnas)
                var headers = new[] { "ID", "Nombre", "Marca", "Descripción", "Precio", "Stock", "Estado" };
                for (int i = 0; i < headers.Length; i++)
                {
                    var cell = ws.Cell(4, i + 1);
                    cell.Value = headers[i];
                    cell.Style.Font.SetBold(true).Font.SetFontColor(XLColor.White)
                        .Fill.SetBackgroundColor(XLColor.FromHtml("#E67E22"))
                        .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                        .Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                }

                // Datos
                int row = 5;
                foreach (var p in productos)
                {
                    var bgRow = row % 2 == 0 ? XLColor.FromHtml("#F8F9FA") : XLColor.White;

                    ws.Cell(row, 1).Value = p.Id;
                    ws.Cell(row, 2).Value = p.Nombre;
                    ws.Cell(row, 3).Value = p.Marca;
                    ws.Cell(row, 4).Value = p.Descripcion;
                    ws.Cell(row, 5).Value = p.Precio;
                    ws.Cell(row, 5).Style.NumberFormat.Format = "$#,##0.00";
                    ws.Cell(row, 6).Value = p.Stock;
                    ws.Cell(row, 7).Value = "Stock bajo";

                    // Estilos de alerta
                    ws.Cell(row, 6).Style.Font.SetFontColor(XLColor.Red).Font.SetBold(true);
                    ws.Cell(row, 7).Style.Font.SetFontColor(XLColor.Orange).Font.SetBold(true);

                    ws.Range(row, 1, row, 7).Style
                        .Fill.SetBackgroundColor(bgRow)
                        .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                        .Border.SetInsideBorder(XLBorderStyleValues.Thin);

                    row++;
                }

                // Fila de totales
                ws.Cell(row, 1).Value = $"Total productos con stock bajo: {productos.Count}";
                ws.Range(row, 1, row, 4).Merge();
                ws.Cell(row, 1).Style.Font.SetBold(true);

                ws.Range(row, 1, row, 7).Style
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#FFF2E0"))
                    .Border.SetOutsideBorder(XLBorderStyleValues.Medium);

                ws.Columns().AdjustToContents();
                wb.SaveAs(dialog.FileName);
                AbrirArchivo(dialog.FileName);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error al exportar Excel: {ex.Message}");
            }
        }

        // ═══════════════════════════════════════════════════
        // EXPORTAR RESUMEN DE VENTAS POR FECHA ESPECÍFICA
        // ═══════════════════════════════════════════════════
        public static void ExportarResumenPorFecha(
            DateTime fechaConsultada,
            decimal totalGanancias,
            int cantidadVentas,
            int productosVendidos,
            int serviciosVendidos,
            List<Venta> ventas)
        {
            var dialog = new SaveFileDialog
            {
                FileName = $"Ventas_{fechaConsultada:yyyyMMdd}",
                DefaultExt = ".xlsx",
                Filter = "Excel files (.xlsx)|*.xlsx"
            };

            if (dialog.ShowDialog() != true) return;

            try
            {
                using var wb = new XLWorkbook();
                var ws = wb.Worksheets.Add("Ventas por Fecha");

                // ── Título ──────────────────────────────────
                ws.Cell("A1").Value = $"REPORTE DE VENTAS — {fechaConsultada:dd/MM/yyyy}";
                ws.Range("A1:F1").Merge();
                ws.Cell("A1").Style
                    .Font.SetBold(true).Font.SetFontSize(14)
                    .Font.SetFontColor(XLColor.White)
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#1A2E4A"))
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                ws.Cell("A2").Value = $"Fecha consultada: {fechaConsultada:dddd dd/MM/yyyy}   |   Generado: {DateTime.Now:dd/MM/yyyy HH:mm}";
                ws.Range("A2:F2").Merge();
                ws.Cell("A2").Style
                    .Font.SetFontSize(9).Font.SetFontColor(XLColor.Gray)
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                // ── Tarjetas de resumen ──────────────────────
                var labels = new[] { "Ganancias del día", "Ventas realizadas", "Productos vendidos", "Servicios vendidos" };
                var colors = new[] { "#27AE60", "#1A2E4A", "#2980B9", "#8E44AD" };

                for (int i = 0; i < labels.Length; i++)
                {
                    var cellLabel = ws.Cell(4, i + 1);
                    cellLabel.Value = labels[i];
                    cellLabel.Style
                        .Font.SetBold(true).Font.SetFontSize(10).Font.SetFontColor(XLColor.White)
                        .Fill.SetBackgroundColor(XLColor.FromHtml(colors[i]))
                        .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                        .Border.SetOutsideBorder(XLBorderStyleValues.Thin);

                    var cellVal = ws.Cell(5, i + 1);
                    if (i == 0)
                    {
                        cellVal.Value = totalGanancias;
                        cellVal.Style.NumberFormat.Format = "$#,##0.00";
                        cellVal.Style.Font.SetFontColor(XLColor.FromHtml("#27AE60"));
                    }
                    else
                    {
                        cellVal.Value = i == 1 ? cantidadVentas : i == 2 ? productosVendidos : serviciosVendidos;
                    }

                    cellVal.Style
                        .Font.SetBold(true).Font.SetFontSize(16)
                        .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                        .Fill.SetBackgroundColor(XLColor.FromHtml("#F8F9FA"))
                        .Border.SetOutsideBorder(XLBorderStyleValues.Thin);

                    ws.Row(5).Height = 30;
                }

                // ── Detalle de ventas ────────────────────────
                ws.Cell("A7").Value = $"DETALLE DE VENTAS — {fechaConsultada:dd/MM/yyyy}";
                ws.Range("A7:F7").Merge();
                ws.Cell("A7").Style
                    .Font.SetBold(true).Font.SetFontSize(11).Font.SetFontColor(XLColor.White)
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#2C3E50"))
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                var headers = new[] { "N° Factura", "Fecha", "Hora", "Productos", "Servicios", "Total" };
                for (int i = 0; i < headers.Length; i++)
                {
                    var cell = ws.Cell(8, i + 1);
                    cell.Value = headers[i];
                    cell.Style
                        .Font.SetBold(true).Font.SetFontColor(XLColor.White)
                        .Fill.SetBackgroundColor(XLColor.FromHtml("#1A2E4A"))
                        .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                        .Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                }

                using var db = new AppDbContext();
                int row = 9;
                foreach (var v in ventas.OrderBy(v => v.Fecha))
                {
                    var cantProd = db.DetallesVenta
                        .Where(d => d.VentaId == v.Id)
                        .Sum(d => (int?)d.Cantidad) ?? 0;

                    var cantServ = db.DetallesServicioVenta
                        .Where(d => d.VentaId == v.Id)
                        .Sum(d => (int?)d.Cantidad) ?? 0;

                    var bg = row % 2 == 0 ? XLColor.FromHtml("#F8F9FA") : XLColor.White;

                    ws.Cell(row, 1).Value = v.NumFactura;
                    ws.Cell(row, 2).Value = v.Fecha.ToString("dd/MM/yyyy");  // ← fecha explícita
                    ws.Cell(row, 3).Value = v.Fecha.ToString("HH:mm");
                    ws.Cell(row, 4).Value = cantProd;
                    ws.Cell(row, 5).Value = cantServ;
                    ws.Cell(row, 6).Value = v.Total;
                    ws.Cell(row, 6).Style.NumberFormat.Format = "$#,##0.00";
                    ws.Cell(row, 6).Style.Font.SetFontColor(XLColor.FromHtml("#27AE60")).Font.SetBold(true);

                    ws.Range(row, 1, row, 6).Style
                        .Fill.SetBackgroundColor(bg)
                        .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                        .Border.SetInsideBorder(XLBorderStyleValues.Thin);

                    row++;
                }

                // ── Fila de total ────────────────────────────
                ws.Cell(row, 1).Value = $"Total ventas: {ventas.Count}   |   Fecha: {fechaConsultada:dd/MM/yyyy}";
                ws.Range(row, 1, row, 5).Merge();
                ws.Cell(row, 1).Style.Font.SetBold(true);

                ws.Cell(row, 6).Value = totalGanancias;
                ws.Cell(row, 6).Style
                    .NumberFormat.SetFormat("$#,##0.00")
                    .Font.SetBold(true).Font.SetFontColor(XLColor.FromHtml("#27AE60"));

                ws.Range(row, 1, row, 6).Style
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#EAFAF1"))
                    .Border.SetOutsideBorder(XLBorderStyleValues.Medium);

                ws.Columns().AdjustToContents();
                wb.SaveAs(dialog.FileName);
                AbrirArchivo(dialog.FileName);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error al exportar Excel: {ex.Message}");
            }
        }

        // ═══════════════════════════════════════════════════
        // 6. EXPORTAR RESUMEN DIARIO DE VENTAS
        // ═══════════════════════════════════════════════════
        public static void ExportarResumenDiario(
            decimal totalGanancias,
            int cantidadVentas,
            int productosVendidos,
            int serviciosVendidos,
            List<Venta> ventas)
        {
            var dialog = new SaveFileDialog
            {
                FileName = $"Resumen_Ventas_{DateTime.Now:yyyyMMdd}",
                DefaultExt = ".xlsx",
                Filter = "Excel files (.xlsx)|*.xlsx"
            };

            if (dialog.ShowDialog() != true) return;

            try
            {
                using var wb = new XLWorkbook();
                var ws = wb.Worksheets.Add("Resumen del Día");

                // ── Título ──────────────────────────────────
                ws.Cell("A1").Value = "RESUMEN DE VENTAS DEL DÍA";
                ws.Range("A1:F1").Merge();
                ws.Cell("A1").Style
                    .Font.SetBold(true).Font.SetFontSize(14)
                    .Font.SetFontColor(XLColor.White)
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#1A2E4A"))
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                ws.Cell("A2").Value = $"Fecha: {DateTime.Now:dd/MM/yyyy} Generado: {DateTime.Now:HH:mm}";
                ws.Range("A2:F2").Merge();
                ws.Cell("A2").Style
                    .Font.SetFontSize(9).Font.SetFontColor(XLColor.Gray)
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                // ── Tarjetas de resumen ──────────────────────
                var labels = new[] { "Ganancias del día", "Ventas realizadas", "Productos vendidos", "Servicios vendidos" };
                var colors = new[] { "#27AE60", "#1A2E4A", "#2980B9", "#8E44AD" };

                for (int i = 0; i < labels.Length; i++)
                {
                    var cellLabel = ws.Cell(4, i + 1);
                    cellLabel.Value = labels[i];
                    cellLabel.Style
                        .Font.SetBold(true).Font.SetFontSize(10).Font.SetFontColor(XLColor.White)
                        .Fill.SetBackgroundColor(XLColor.FromHtml(colors[i]))
                        .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                        .Border.SetOutsideBorder(XLBorderStyleValues.Thin);

                    var cellVal = ws.Cell(5, i + 1);

                    // Asignación corregida
                    if (i == 0)
                    {
                        cellVal.Value = totalGanancias;
                        cellVal.Style.NumberFormat.Format = "$#,##0.00";
                        cellVal.Style.Font.SetFontColor(XLColor.FromHtml("#27AE60"));
                    }
                    else
                    {
                        cellVal.Value = (i == 1 ? cantidadVentas : i == 2 ? productosVendidos : serviciosVendidos);
                    }

                    cellVal.Style
                        .Font.SetBold(true).Font.SetFontSize(16)
                        .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                        .Fill.SetBackgroundColor(XLColor.FromHtml("#F8F9FA"))
                        .Border.SetOutsideBorder(XLBorderStyleValues.Thin);

                    ws.Row(5).Height = 30;
                }

                // ── Detalle de ventas ────────────────────────
                ws.Cell("A7").Value = "DETALLE DE VENTAS";
                ws.Range("A7:F7").Merge();
                ws.Cell("A7").Style
                    .Font.SetBold(true).Font.SetFontSize(11).Font.SetFontColor(XLColor.White)
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#2C3E50"))
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                var headers = new[] { "N° Factura", "Hora", "Cajero", "Productos", "Servicios", "Total" };
                for (int i = 0; i < headers.Length; i++)
                {
                    var cell = ws.Cell(8, i + 1);
                    cell.Value = headers[i];
                    cell.Style
                        .Font.SetBold(true).Font.SetFontColor(XLColor.White)
                        .Fill.SetBackgroundColor(XLColor.FromHtml("#1A2E4A"))
                        .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                        .Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                }

                using var db = new AppDbContext();
                int row = 9;
                foreach (var v in ventas.OrderBy(v => v.Fecha))
                {
                    var cantProd = db.DetallesVenta
                        .Where(d => d.VentaId == v.Id)
                        .Sum(d => (int?)d.Cantidad) ?? 0;

                    var cantServ = db.DetallesServicioVenta
                        .Where(d => d.VentaId == v.Id)
                        .Sum(d => (int?)d.Cantidad) ?? 0;

                    var cajero = db.Usuarios.Find(v.UsuarioId)?.Nombre ?? "-";

                    var bg = row % 2 == 0 ? XLColor.FromHtml("#F8F9FA") : XLColor.White;

                    ws.Cell(row, 1).Value = v.NumFactura;
                    ws.Cell(row, 2).Value = v.Fecha.ToString("HH:mm");
                    ws.Cell(row, 3).Value = cajero;
                    ws.Cell(row, 4).Value = cantProd;
                    ws.Cell(row, 5).Value = cantServ;
                    ws.Cell(row, 6).Value = v.Total;
                    ws.Cell(row, 6).Style.NumberFormat.Format = "$#,##0.00";
                    ws.Cell(row, 6).Style.Font.SetFontColor(XLColor.FromHtml("#27AE60")).Font.SetBold(true);

                    ws.Range(row, 1, row, 6).Style
                        .Fill.SetBackgroundColor(bg)
                        .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                        .Border.SetInsideBorder(XLBorderStyleValues.Thin);

                    row++;
                }

                // ── Fila de total ────────────────────────────
                ws.Cell(row, 1).Value = $"Total ventas: {ventas.Count}";
                ws.Range(row, 1, row, 5).Merge();
                ws.Cell(row, 1).Style.Font.SetBold(true);

                ws.Cell(row, 6).Value = totalGanancias;
                ws.Cell(row, 6).Style
                    .NumberFormat.SetFormat("$#,##0.00")
                    .Font.SetBold(true).Font.SetFontColor(XLColor.FromHtml("#27AE60"));

                ws.Range(row, 1, row, 6).Style
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#EAFAF1"))
                    .Border.SetOutsideBorder(XLBorderStyleValues.Medium);

                ws.Columns().AdjustToContents();
                wb.SaveAs(dialog.FileName);
                AbrirArchivo(dialog.FileName);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error al exportar Excel: {ex.Message}");
            }
        }

        // ═══════════════════════════════════════════════════
        // 7. EXPORTAR DEUDORES NOTIFICADOS
        // ═══════════════════════════════════════════════════
        public static void ExportarDeudoresNotificados(List<ClienteDeudor> clientes)
        {
            var dialog = new SaveFileDialog
            {
                FileName = $"deudoresNotificados_{DateTime.Now:yyyyMMdd}",
                DefaultExt = ".xlsx",
                Filter = "Excel files (.xlsx)|*.xlsx"
            };

            if (dialog.ShowDialog() != true) return;

            try
            {
                using var wb = new XLWorkbook();
                var ws = wb.Worksheets.Add("Deudores Notificados");

                // Título
                ws.Cell("A1").Value = "CLIENTES DEUDORES NOTIFICADOS";
                ws.Range("A1:I1").Merge();
                ws.Cell("A1").Style.Font.SetBold(true).Font.SetFontSize(14)
                    .Font.SetFontColor(XLColor.White)
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#E67E22")) // Naranja para alertas
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                ws.Cell("A2").Value = $"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}";
                ws.Range("A2:I2").Merge();
                ws.Cell("A2").Style.Font.SetFontSize(9).Font.SetFontColor(XLColor.Gray)
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                // Encabezados
                var headers = new[] { "Nombre", "Cédula", "Teléfono", "Deuda", "Mora", "Total", "F. Inicio", "F. Límite", "F. Pago" };
                for (int i = 0; i < headers.Length; i++)
                {
                    var cell = ws.Cell(4, i + 1);
                    cell.Value = headers[i];
                    cell.Style.Font.SetBold(true).Font.SetFontColor(XLColor.White)
                        .Fill.SetBackgroundColor(XLColor.FromHtml("#E67E22"))
                        .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                        .Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                }

                // Datos
                int row = 5;
                decimal totalGeneral = 0;

                foreach (var c in clientes)
                {
                    bool vencido = DateTime.Today > c.FechaPago;
                    bool moraAplicada = c.TieneMora && vencido;
                    decimal total = c.MontoDeuda + (moraAplicada ? c.MontoMora : 0);
                    totalGeneral += total;

                    var bgRow = row % 2 == 0 ? XLColor.FromHtml("#F8F9FA") : XLColor.White;

                    ws.Cell(row, 1).Value = c.Nombre;
                    ws.Cell(row, 2).Value = c.Cedula;
                    ws.Cell(row, 3).Value = c.Telefono;
                    ws.Cell(row, 4).Value = c.MontoDeuda;
                    ws.Cell(row, 4).Style.NumberFormat.Format = "$#,##0.00";
                    ws.Cell(row, 5).Value = moraAplicada ? c.MontoMora : 0;
                    ws.Cell(row, 5).Style.NumberFormat.Format = "$#,##0.00";
                    if (moraAplicada) ws.Cell(row, 5).Style.Font.SetFontColor(XLColor.Red);

                    ws.Cell(row, 6).Value = total;
                    ws.Cell(row, 6).Style.NumberFormat.Format = "$#,##0.00";
                    ws.Cell(row, 6).Style.Font.SetBold(true).Font.SetFontColor(XLColor.FromHtml("#E74C3C"));

                    ws.Cell(row, 7).Value = c.FechaInicio.ToString("dd/MM/yyyy");
                    ws.Cell(row, 8).Value = c.FechaPago.ToString("dd/MM/yyyy");
                    ws.Cell(row, 9).Value = c.FechaPagado?.ToString("dd/MM/yyyy") ?? "-";

                    ws.Range(row, 1, row, 9).Style
                        .Fill.SetBackgroundColor(bgRow)
                        .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                        .Border.SetInsideBorder(XLBorderStyleValues.Thin);

                    row++;
                }

                // Totales
                ws.Cell(row, 1).Value = $"Total clientes notificados: {clientes.Count}";
                ws.Range(row, 1, row, 5).Merge();
                ws.Cell(row, 6).Value = totalGeneral;
                ws.Cell(row, 6).Style.NumberFormat.Format = "$#,##0.00";
                ws.Cell(row, 6).Style.Font.SetBold(true).Font.SetFontColor(XLColor.FromHtml("#E74C3C"));

                ws.Range(row, 1, row, 9).Style
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#FFF2E0"))
                    .Border.SetOutsideBorder(XLBorderStyleValues.Medium);

                ws.Columns().AdjustToContents();
                wb.SaveAs(dialog.FileName);
                AbrirArchivo(dialog.FileName);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error al exportar Excel: {ex.Message}");
            }
        }

        // ═══════════════════════════════════════════════════
        // 8. EXPORTAR HISTORIAL DE BACKUPS
        // ═══════════════════════════════════════════════════
        public static void ExportarHistorialBackups(List<(string FechaTexto, bool Exitoso, string Mensaje, string RutaArchivo)> registros)
        {
            var dialog = new SaveFileDialog
            {
                FileName = $"Historial_Backups_{DateTime.Now:yyyyMMdd}",
                DefaultExt = ".xlsx",
                Filter = "Excel files (.xlsx)|*.xlsx"
            };
            if (dialog.ShowDialog() != true) return;

            try
            {
                using var wb = new XLWorkbook();
                var ws = wb.Worksheets.Add("Historial Backups");

                ws.Cell("A1").Value = "HISTORIAL DE BACKUPS DEL SISTEMA";
                ws.Range("A1:D1").Merge();
                ws.Cell("A1").Style
                    .Font.SetBold(true).Font.SetFontSize(14)
                    .Font.SetFontColor(XLColor.White)
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#1E2937"))
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                ws.Cell("A2").Value = $"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}";
                ws.Range("A2:D2").Merge();
                ws.Cell("A2").Style
                    .Font.SetFontSize(9).Font.SetFontColor(XLColor.Gray)
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                var headers = new[] { "Fecha", "Estado", "Mensaje", "Ruta" };
                for (int i = 0; i < headers.Length; i++)
                {
                    var cell = ws.Cell(4, i + 1);
                    cell.Value = headers[i];
                    cell.Style
                        .Font.SetBold(true).Font.SetFontColor(XLColor.White)
                        .Fill.SetBackgroundColor(XLColor.FromHtml("#1E2937"))
                        .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                        .Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                }

                int row = 5;
                foreach (var r in registros)
                {
                    var bgRow = row % 2 == 0 ? XLColor.FromHtml("#F8F9FA") : XLColor.White;

                    ws.Cell(row, 1).Value = r.FechaTexto;

                    ws.Cell(row, 2).Value = r.Exitoso ? "OK" : "Error";
                    ws.Cell(row, 2).Style.Font.SetBold(true);
                    ws.Cell(row, 2).Style.Font.SetFontColor(
                        r.Exitoso ? XLColor.FromHtml("#15803D") : XLColor.FromHtml("#B91C1C"));

                    ws.Cell(row, 3).Value = r.Mensaje;

                    ws.Cell(row, 4).Value = r.RutaArchivo;
                    ws.Cell(row, 4).Style.Font.SetFontColor(XLColor.Gray);
                    ws.Cell(row, 4).Style.Font.SetFontSize(9);

                    ws.Range(row, 1, row, 4).Style
                        .Fill.SetBackgroundColor(bgRow)
                        .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                        .Border.SetInsideBorder(XLBorderStyleValues.Thin);
                    row++;
                }

                int ok = registros.Count(r => r.Exitoso);
                int error = registros.Count(r => !r.Exitoso);

                ws.Cell(row, 1).Value = $"Total: {registros.Count}  |  OK: {ok}  |  Errores: {error}";
                ws.Range(row, 1, row, 4).Merge();
                ws.Cell(row, 1).Style
                    .Font.SetBold(true).Font.SetFontColor(XLColor.FromHtml("#475569"))
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#F1F5F9"))
                    .Border.SetOutsideBorder(XLBorderStyleValues.Medium)
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left);

                ws.Columns().AdjustToContents();
                wb.SaveAs(dialog.FileName);
                AbrirArchivo(dialog.FileName);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error al exportar Excel: {ex.Message}",
                    "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        // ═══════════════════════════════════════════════════
        // 9. EXPORTAR LISTA GENERAL DE PROVEEDORES
        // ═══════════════════════════════════════════════════
        public static void ExportarProveedores(List<Proveedor> proveedores)
        {
            var dialog = new SaveFileDialog
            {
                FileName = $"Proveedores_{DateTime.Now:yyyyMMdd_HHmm}",
                DefaultExt = ".xlsx",
                Filter = "Excel files (.xlsx)|*.xlsx"
            };
            if (dialog.ShowDialog() != true) return;

            try
            {
                using var wb = new XLWorkbook();
                var ws = wb.Worksheets.Add("Proveedores");

                ws.Cell("A1").Value = "LISTA GENERAL DE PROVEEDORES";
                ws.Range("A1:I1").Merge();
                ws.Cell("A1").Style.Font.SetBold(true).Font.SetFontSize(14)
                    .Font.SetFontColor(XLColor.White)
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#263645"))
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                ws.Cell("A2").Value = $"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}";
                ws.Range("A2:I2").Merge();
                ws.Cell("A2").Style.Font.SetFontSize(9).Font.SetFontColor(XLColor.Gray)
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                var headers = new[] { "ID", "Nombre", "Tipo", "RNC/Cédula", "Teléfono", "Correo", "Mercancía", "Deuda", "Estado" };
                EscribirEncabezados(ws, headers, "#263645");

                int row = 5;
                foreach (var p in proveedores)
                {
                    var bg = row % 2 == 0 ? XLColor.FromHtml("#F8F9FA") : XLColor.White;
                    ws.Cell(row, 1).Value = p.Id;
                    ws.Cell(row, 2).Value = p.Nombre;
                    ws.Cell(row, 3).Value = p.TipoTexto;
                    ws.Cell(row, 4).Value = p.RncOCedula;
                    ws.Cell(row, 5).Value = p.Telefono;
                    ws.Cell(row, 6).Value = p.Correo;
                    ws.Cell(row, 7).Value = p.Transacciones?.Sum(t => t.CantidadMercancia) ?? 0;
                    ws.Cell(row, 8).Value = p.DeudaTotal;
                    ws.Cell(row, 8).Style.NumberFormat.Format = "$#,##0.00";
                    ws.Cell(row, 9).Value = p.EstadoTexto;
                    ws.Cell(row, 9).Style.Font.SetBold(true).Font.SetFontColor(
                        p.DeudaTotal > 0 ? XLColor.FromHtml("#C0392B") : XLColor.FromHtml("#1E8449"));

                    ws.Range(row, 1, row, 9).Style
                        .Fill.SetBackgroundColor(bg)
                        .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                        .Border.SetInsideBorder(XLBorderStyleValues.Thin);
                    row++;
                }

                ws.Cell(row, 1).Value = $"Pagados: {proveedores.Count(p => p.DeudaTotal <= 0)} | Adeudados: {proveedores.Count(p => p.DeudaTotal > 0)}";
                ws.Range(row, 1, row, 7).Merge();
                ws.Cell(row, 8).Value = proveedores.Sum(p => p.DeudaTotal);
                ws.Cell(row, 8).Style.NumberFormat.Format = "$#,##0.00";
                ws.Range(row, 1, row, 9).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#EEF6FB"))
                    .Font.SetBold(true).Border.SetOutsideBorder(XLBorderStyleValues.Medium);

                ws.Columns().AdjustToContents();
                wb.SaveAs(dialog.FileName);
                AbrirArchivo(dialog.FileName);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error al exportar proveedores: {ex.Message}");
            }
        }

        public static void ExportarTransaccionesProveedor(List<TransaccionProveedor> transacciones, string titulo = "HISTORIAL DE TRANSACCIONES DE PROVEEDORES")
        {
            var dialog = new SaveFileDialog
            {
                FileName = $"Transacciones_Proveedores_{DateTime.Now:yyyyMMdd_HHmm}",
                DefaultExt = ".xlsx",
                Filter = "Excel files (.xlsx)|*.xlsx"
            };
            if (dialog.ShowDialog() != true) return;

            try
            {
                using var wb = new XLWorkbook();
                var ws = wb.Worksheets.Add("Transacciones");

                ws.Cell("A1").Value = titulo;
                ws.Range("A1:J1").Merge();
                ws.Cell("A1").Style.Font.SetBold(true).Font.SetFontSize(14)
                    .Font.SetFontColor(XLColor.White)
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#1A2E4A"))
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                ws.Cell("A2").Value = $"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}";
                ws.Range("A2:J2").Merge();
                ws.Cell("A2").Style.Font.SetFontSize(9).Font.SetFontColor(XLColor.Gray)
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                var headers = new[] { "ID Trans.", "ID Prov.", "Proveedor", "Tipo Prov.", "Fecha", "Cantidad", "Total", "Pagado", "Pendiente", "Estado" };
                EscribirEncabezados(ws, headers, "#1A2E4A");

                int row = 5;
                foreach (var t in transacciones.OrderByDescending(t => t.FechaProvision))
                {
                    var bg = row % 2 == 0 ? XLColor.FromHtml("#F8F9FA") : XLColor.White;
                    ws.Cell(row, 1).Value = t.Id;
                    ws.Cell(row, 2).Value = t.ProveedorId;
                    ws.Cell(row, 3).Value = t.Proveedor?.Nombre ?? "-";
                    ws.Cell(row, 4).Value = t.Proveedor?.TipoTexto ?? "-";
                    ws.Cell(row, 5).Value = t.FechaProvision.ToString("dd/MM/yyyy");
                    ws.Cell(row, 6).Value = t.CantidadMercancia;
                    ws.Cell(row, 7).Value = t.MontoTotal;
                    ws.Cell(row, 8).Value = t.MontoPagado;
                    ws.Cell(row, 9).Value = t.SaldoPendiente;
                    ws.Cell(row, 10).Value = t.EstadoPago.ToString();
                    ws.Range(row, 7, row, 9).Style.NumberFormat.Format = "$#,##0.00";
                    ws.Range(row, 1, row, 10).Style
                        .Fill.SetBackgroundColor(bg)
                        .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                        .Border.SetInsideBorder(XLBorderStyleValues.Thin);
                    row++;
                }

                ws.Cell(row, 1).Value = $"Total transacciones: {transacciones.Count}";
                ws.Range(row, 1, row, 6).Merge();
                ws.Cell(row, 7).Value = transacciones.Sum(t => t.MontoTotal);
                ws.Cell(row, 8).Value = transacciones.Sum(t => t.MontoPagado);
                ws.Cell(row, 9).Value = transacciones.Sum(t => t.SaldoPendiente);
                ws.Range(row, 7, row, 9).Style.NumberFormat.Format = "$#,##0.00";
                ws.Range(row, 1, row, 10).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#EEF6FB"))
                    .Font.SetBold(true).Border.SetOutsideBorder(XLBorderStyleValues.Medium);

                ws.Columns().AdjustToContents();
                wb.SaveAs(dialog.FileName);
                AbrirArchivo(dialog.FileName);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error al exportar transacciones: {ex.Message}");
            }
        }

        public static void ExportarHistorialProveedor(List<HistorialProveedor> movimientos, string proveedor)
        {
            var dialog = new SaveFileDialog
            {
                FileName = $"Historial_{proveedor.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmm}",
                DefaultExt = ".xlsx",
                Filter = "Excel files (.xlsx)|*.xlsx"
            };
            if (dialog.ShowDialog() != true) return;

            try
            {
                using var wb = new XLWorkbook();
                var ws = wb.Worksheets.Add("Historial");

                ws.Cell("A1").Value = $"HISTORIAL DE PROVEEDOR: {proveedor}";
                ws.Range("A1:G1").Merge();
                ws.Cell("A1").Style.Font.SetBold(true).Font.SetFontSize(14)
                    .Font.SetFontColor(XLColor.White)
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#566573"))
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                var headers = new[] { "ID", "Fecha", "Tipo", "ID Trans.", "Descripción", "Monto", "Saldo" };
                EscribirEncabezados(ws, headers, "#566573");

                int row = 5;
                foreach (var h in movimientos.OrderByDescending(h => h.Fecha))
                {
                    ws.Cell(row, 1).Value = h.Id;
                    ws.Cell(row, 2).Value = h.Fecha.ToString("dd/MM/yyyy HH:mm");
                    ws.Cell(row, 3).Value = h.TipoMovimiento.ToString();
                    ws.Cell(row, 4).Value = h.TransaccionProveedorId;
                    ws.Cell(row, 5).Value = h.Descripcion;
                    ws.Cell(row, 6).Value = h.Monto;
                    ws.Cell(row, 7).Value = h.SaldoResultante;
                    ws.Range(row, 6, row, 7).Style.NumberFormat.Format = "$#,##0.00";
                    ws.Range(row, 1, row, 7).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                    row++;
                }

                ws.Columns().AdjustToContents();
                wb.SaveAs(dialog.FileName);
                AbrirArchivo(dialog.FileName);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error al exportar historial: {ex.Message}");
            }
        }

        public static void ExportarVencimientosProveedor(IEnumerable<VencimientoProveedorItem> transacciones)
        {
            var lista = transacciones.ToList();

            var dialog = new SaveFileDialog
            {
                FileName = $"Vencimientos_Proveedores_{DateTime.Now:yyyyMMdd_HHmm}",
                DefaultExt = ".xlsx",
                Filter = "Excel files (.xlsx)|*.xlsx"
            };
            if (dialog.ShowDialog() != true) return;

            try
            {
                using var wb = new XLWorkbook();
                var ws = wb.Worksheets.Add("Vencimientos");

                // ── Título ───────────────────────────────────────────
                ws.Cell("A1").Value = "VENCIMIENTOS DE PAGOS A PROVEEDORES";
                ws.Range("A1:F1").Merge();
                ws.Cell("A1").Style
                    .Font.SetBold(true).Font.SetFontSize(14)
                    .Font.SetFontColor(XLColor.White)
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#1A2E4A"))
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                ws.Cell("A2").Value = $"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}";
                ws.Range("A2:F2").Merge();
                ws.Cell("A2").Style
                    .Font.SetFontSize(9).Font.SetFontColor(XLColor.Gray)
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                // ── Encabezados ──────────────────────────────────────
                var headers = new[] { "ID Trans.", "Proveedor", "Fecha límite", "Total", "Pendiente", "Alerta" };
                EscribirEncabezados(ws, headers, "#1A2E4A");

                // ── Filas ────────────────────────────────────────────
                int row = 5;
                foreach (var t in lista)
                {
                    var bg = row % 2 == 0 ? XLColor.FromHtml("#F8F9FA") : XLColor.White;

                    ws.Cell(row, 1).Value = t.IdTransaccion;
                    ws.Cell(row, 2).Value = t.NombreProveedor;
                    ws.Cell(row, 3).Value = t.FechaLimite.ToString("dd/MM/yyyy");
                    ws.Cell(row, 4).Value = t.MontoTotal;
                    ws.Cell(row, 5).Value = t.SaldoPendiente;
                    ws.Cell(row, 6).Value = t.EstadoAlerta;

                    ws.Range(row, 4, row, 5).Style.NumberFormat.Format = "$#,##0.00";

                    // Color de fondo en columna Alerta según urgencia
                    var colorAlerta = XLColor.FromHtml(t.ColorAlerta);
                    ws.Cell(row, 6).Style
                        .Fill.SetBackgroundColor(colorAlerta)
                        .Font.SetFontColor(XLColor.White)
                        .Font.SetBold(true);

                    ws.Range(row, 1, row, 6).Style
                        .Fill.SetBackgroundColor(bg)
                        .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                        .Border.SetInsideBorder(XLBorderStyleValues.Thin);

                    // La celda de alerta sobreescribe el fondo de la fila
                    ws.Cell(row, 6).Style.Fill.SetBackgroundColor(colorAlerta);

                    row++;
                }

                // ── Fila de totales ──────────────────────────────────
                ws.Cell(row, 1).Value = $"Total: {lista.Count} transacción(es)";
                ws.Range(row, 1, row, 3).Merge();
                ws.Cell(row, 4).Value = lista.Sum(t => t.MontoTotal);
                ws.Cell(row, 5).Value = lista.Sum(t => t.SaldoPendiente);
                ws.Range(row, 4, row, 5).Style.NumberFormat.Format = "$#,##0.00";
                ws.Range(row, 1, row, 6).Style
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#EEF6FB"))
                    .Font.SetBold(true)
                    .Border.SetOutsideBorder(XLBorderStyleValues.Medium);

                ws.Columns().AdjustToContents();
                wb.SaveAs(dialog.FileName);
                AbrirArchivo(dialog.FileName);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error al exportar vencimientos: {ex.Message}");
            }
        }

        private static void EscribirEncabezados(IXLWorksheet ws, string[] headers, string color)
        {
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(4, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.SetBold(true).Font.SetFontColor(XLColor.White)
                    .Fill.SetBackgroundColor(XLColor.FromHtml(color))
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                    .Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            }

        }


        // ═══════════════════════════════════════════════════
        // APOYO
        // ═══════════════════════════════════════════════════
        private static void AbrirArchivo(string ruta)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = ruta,
                    UseShellExecute = true
                });
            }
            catch { }
        }
    }
}
