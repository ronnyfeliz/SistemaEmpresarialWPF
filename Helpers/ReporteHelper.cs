using ClosedXML.Excel;
using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.draw;
using Project_v1.Modelos;
using Project_v1.Servicios;
using Project_v1.Vista;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using Font = iTextSharp.text.Font;

namespace Project_v1.Helpers
{
    public static class ReporteHelper
    {
        // ── Colores PDF (evita CS0117: BaseColor no tiene White/Black/Gray estáticos) ──
        private static readonly BaseColor PdfBlanco = new BaseColor(255, 255, 255);
        private static readonly BaseColor PdfNegro = new BaseColor(0, 0, 0);
        private static readonly BaseColor PdfGris = new BaseColor(140, 140, 140);
        private static readonly BaseColor PdfVerde = new BaseColor(26, 107, 58);   // #1A6B3A
        private static readonly BaseColor PdfOscuro = new BaseColor(44, 62, 80);   // #2C3E50
        private static readonly BaseColor PdfClaro = new BaseColor(240, 244, 248);  // #F0F4F8
        private static readonly BaseColor PdfTotal = new BaseColor(39, 174, 96);   // #27AE60

        // ── Punto de entrada ──────────────────────────────────────────────────
        /// <summary>
        /// Filtra las ventas según la selección del usuario y genera el archivo.
        /// </summary>
        public static void Generar(ReporteSeleccion seleccion, IEnumerable<Venta> ventas)
        {
            var (desde, hasta) = seleccion.ObtenerRango();

            var filtradas = ventas
                .Where(v => v.Fecha.Date >= desde && v.Fecha.Date <= hasta)
                .OrderBy(v => v.Fecha)
                .ToList();

            var titulo = seleccion.ObtenerTitulo();
            var nombre = seleccion.NombreArchivo();

            if (seleccion.Formato == ReporteSeleccion.FormatoArchivo.Excel)
                GenerarExcel(filtradas, titulo, nombre);
            else
                GenerarPDF(filtradas, titulo, nombre);
        }

        // ── EXCEL ─────────────────────────────────────────────────────────────
        private static void GenerarExcel(List<Venta> ventas, string titulo, string nombreSugerido)
        {
            try
            {
                var ruta = GuardarDialogo(nombreSugerido);
                if (ruta == null) return;

                using (var wb = new XLWorkbook())
                {
                    var ws = wb.Worksheets.Add("Ventas");

                    // ── Fila 1: título ──
                    ws.Cell(1, 1).Value = titulo;
                    ws.Range(1, 1, 1, 5).Merge();
                    var estiloTitulo = ws.Cell(1, 1).Style;
                    estiloTitulo.Font.Bold = true;
                    estiloTitulo.Font.FontSize = 14;
                    estiloTitulo.Font.FontColor = XLColor.White;
                    estiloTitulo.Fill.BackgroundColor = XLColor.FromHtml("#1A6B3A");
                    estiloTitulo.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    estiloTitulo.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    ws.Row(1).Height = 28;

                    // ── Fila 2: subtítulo con rango ──
                    var (desde, hasta) = ObtenerRangoTexto(ventas);
                    ws.Cell(2, 1).Value = desde;
                    ws.Range(2, 1, 2, 5).Merge();
                    var estiloSub = ws.Cell(2, 1).Style;
                    estiloSub.Font.Italic = true;
                    estiloSub.Font.FontSize = 10;
                    estiloSub.Font.FontColor = XLColor.Gray;
                    estiloSub.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    // ── Fila 3: encabezados ──
                    string[] headers = { "Factura", "Fecha", "Hora", "Usuario", "Total" };
                    for (int i = 0; i < headers.Length; i++)
                    {
                        var cell = ws.Cell(3, i + 1);
                        cell.Value = headers[i];
                        cell.Style.Font.Bold = true;
                        cell.Style.Font.FontColor = XLColor.White;
                        cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#2C3E50");
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        cell.Style.Border.OutsideBorderColor = XLColor.White;
                    }
                    ws.Row(3).Height = 22;

                    // ── Filas de datos ──
                    int fila = 4;
                    decimal totalGeneral = 0;
                    bool sombreado = false;

                    foreach (var v in ventas)
                    {
                        totalGeneral += v.Total;

                        ws.Cell(fila, 1).Value = v.NumFactura ?? "";
                        ws.Cell(fila, 2).Value = v.Fecha.ToString("dd/MM/yyyy");
                        ws.Cell(fila, 3).Value = v.Fecha.ToString("HH:mm");
                        ws.Cell(fila, 4).Value = v.Usuario?.Nombre ?? "";
                        ws.Cell(fila, 5).Value = v.Total;

                        // Formato numérico en la columna Total
                        ws.Cell(fila, 5).Style.NumberFormat.Format = "#,##0.00";
                        ws.Cell(fila, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                        if (sombreado)
                        {
                            ws.Range(fila, 1, fila, 5).Style
                                .Fill.SetBackgroundColor(XLColor.FromHtml("#F0F4F8"));
                        }

                        // Borde inferior suave
                        ws.Range(fila, 1, fila, 5).Style
                            .Border.SetBottomBorder(XLBorderStyleValues.Hair)
                            .Border.SetBottomBorderColor(XLColor.FromHtml("#CCCCCC"));

                        sombreado = !sombreado;
                        fila++;
                    }

                    // ── Fila de totales ──
                    ws.Cell(fila, 4).Value = "TOTAL:";
                    ws.Cell(fila, 4).Style.Font.Bold = true;
                    ws.Cell(fila, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                    ws.Cell(fila, 4).Style.Border.TopBorder = XLBorderStyleValues.Medium;

                    ws.Cell(fila, 5).Value = totalGeneral;
                    ws.Cell(fila, 5).Style.Font.Bold = true;
                    ws.Cell(fila, 5).Style.Font.FontColor = XLColor.FromHtml("#27AE60");
                    ws.Cell(fila, 5).Style.NumberFormat.Format = "#,##0.00";
                    ws.Cell(fila, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                    ws.Cell(fila, 5).Style.Border.TopBorder = XLBorderStyleValues.Medium;

                    // ── Nota al pie ──
                    fila += 2;
                    ws.Cell(fila, 1).Value = $"Total de registros: {ventas.Count}   |   Generado el: {DateTime.Now:dd/MM/yyyy HH:mm}";
                    ws.Range(fila, 1, fila, 5).Merge();
                    ws.Cell(fila, 1).Style.Font.Italic = true;
                    ws.Cell(fila, 1).Style.Font.FontSize = 9;
                    ws.Cell(fila, 1).Style.Font.FontColor = XLColor.Gray;

                    // ── Ajuste de ancho ──
                    ws.Column(1).Width = 18; // Factura
                    ws.Column(2).Width = 14; // Fecha
                    ws.Column(3).Width = 10; // Hora
                    ws.Column(4).Width = 20; // Usuario
                    ws.Column(5).Width = 14; // Total

                    wb.SaveAs(ruta);
                }

                AbrirArchivo(ruta);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al generar Excel:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── PDF ───────────────────────────────────────────────────────────────
        private static void GenerarPDF(List<Venta> ventas, string titulo, string nombreSugerido)
        {
            try
            {
                var ruta = GuardarDialogo(nombreSugerido);
                if (ruta == null) return;

                if (ventas == null || ventas.Count == 0)
                {
                    MessageBox.Show("No hay ventas en el período seleccionado.",
                                   "Sin datos", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Cargar información del negocio
                Negocio? negocio;
                using (var db = new AppDbContext())
                {
                    negocio = db.Negocios.FirstOrDefault();
                }

                var doc = new Document(PageSize.A4, 40f, 40f, 50f, 40f);

                using (var stream = new FileStream(ruta, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    var writer = PdfWriter.GetInstance(doc, stream);
                    doc.Open();

                    // ====================== FUENTES ======================
                    var fTitulo = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16f, PdfBlanco);
                    var fHeader = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10f, PdfBlanco);
                    var fNormal = FontFactory.GetFont(FontFactory.HELVETICA, 9f, PdfNegro);
                    var fTotal = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10f, PdfTotal);
                    var fTotalLbl = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10f, PdfNegro);   // ← Corregido
                    var fGris = FontFactory.GetFont(FontFactory.HELVETICA_OBLIQUE, 8.5f, PdfGris);
                    var fNegocio = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18f, PdfOscuro);
                    var fSlogan = FontFactory.GetFont(FontFactory.HELVETICA_OBLIQUE, 10f, PdfGris);

                    // ====================== ENCABEZADO ======================
                    var headerTable = new PdfPTable(3) { WidthPercentage = 100f };
                    headerTable.SetWidths(new float[] { 1.2f, 3f, 2.2f });

                    // Logo
                    var celdaLogo = new PdfPCell { Border = Rectangle.NO_BORDER, Padding = 8f, VerticalAlignment = Element.ALIGN_MIDDLE };
                    if (!string.IsNullOrWhiteSpace(negocio?.RutaLogo) && File.Exists(negocio.RutaLogo))
                    {
                        try
                        {
                            var img = Image.GetInstance(negocio.RutaLogo);
                            img.ScaleToFit(85f, 85f);
                            celdaLogo.AddElement(img);
                        }
                        catch { }
                    }
                    headerTable.AddCell(celdaLogo);

                    // Nombre + Slogan
                    var celdaNombre = new PdfPCell { Border = Rectangle.NO_BORDER, VerticalAlignment = Element.ALIGN_MIDDLE, Padding = 8f };
                    celdaNombre.AddElement(new Paragraph(negocio?.NombreNegocio ?? "Colmado Reyes", fNegocio));
                    celdaNombre.AddElement(new Paragraph(negocio?.Slogan ?? "La mejor mercancía", fSlogan));
                    headerTable.AddCell(celdaNombre);

                    // Datos de contacto
                    var contacto = new Phrase();
                    contacto.Add(new Chunk($"RNC: {(negocio?.RNC ?? "05126007")}\n", fGris));
                    contacto.Add(new Chunk($"Tel: {(negocio?.Telefono ?? "809-522-1235")}\n", fGris));
                    contacto.Add(new Chunk($"Email: {(negocio?.Email ?? "colmado_reyes01@gmail.com")}\n", fGris));
                    contacto.Add(new Chunk(negocio?.Direccion ?? "Barrio Uno, Calle #2", fGris));

                    var celdaContacto = new PdfPCell(contacto)
                    {
                        Border = Rectangle.NO_BORDER,
                        VerticalAlignment = Element.ALIGN_MIDDLE,
                        HorizontalAlignment = Element.ALIGN_RIGHT,
                        Padding = 8f
                    };
                    headerTable.AddCell(celdaContacto);

                    doc.Add(headerTable);

                    // Línea separadora
                    doc.Add(new LineSeparator(2f, 100f, PdfVerde, Element.ALIGN_CENTER, -5));

                    // Título del Reporte
                    var tTitulo = new PdfPTable(1) { WidthPercentage = 100f, SpacingBefore = 15f };
                    tTitulo.AddCell(new PdfPCell(new Phrase(titulo, fTitulo))
                    {
                        BackgroundColor = PdfVerde,
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        VerticalAlignment = Element.ALIGN_MIDDLE,
                        Padding = 12f,
                        Border = Rectangle.NO_BORDER
                    });
                    doc.Add(tTitulo);

                    // Subtítulo
                    doc.Add(new Paragraph(
                        $"Total de registros: {ventas.Count} | Generado: {DateTime.Now:dd/MM/yyyy HH:mm}", fGris)
                    {
                        SpacingBefore = 8f,
                        SpacingAfter = 12f,
                        Alignment = Element.ALIGN_RIGHT
                    });

                    // ====================== TABLA DE VENTAS ======================
                    var tabla = new PdfPTable(5) { WidthPercentage = 100f, SpacingBefore = 4f };
                    tabla.SetWidths(new float[] { 2.2f, 1.6f, 1.0f, 2.2f, 1.4f });

                    string[] cols = { "Factura", "Fecha", "Hora", "Usuario", "Total" };
                    foreach (var h in cols)
                    {
                        tabla.AddCell(new PdfPCell(new Phrase(h, fHeader))
                        {
                            BackgroundColor = PdfOscuro,
                            HorizontalAlignment = Element.ALIGN_CENTER,
                            VerticalAlignment = Element.ALIGN_MIDDLE,
                            Padding = 7f,
                            Border = Rectangle.NO_BORDER
                        });
                    }

                    decimal totalGeneral = 0;
                    bool alterno = false;
                    foreach (var v in ventas)
                    {
                        totalGeneral += v.Total;
                        var bg = alterno ? PdfClaro : PdfBlanco;

                        tabla.AddCell(Celda(v.NumFactura ?? "", fNormal, bg, Element.ALIGN_LEFT));
                        tabla.AddCell(Celda(v.Fecha.ToString("dd/MM/yyyy"), fNormal, bg, Element.ALIGN_CENTER));
                        tabla.AddCell(Celda(v.Fecha.ToString("HH:mm"), fNormal, bg, Element.ALIGN_CENTER));
                        tabla.AddCell(Celda(v.Usuario?.Nombre ?? "", fNormal, bg, Element.ALIGN_LEFT));
                        tabla.AddCell(Celda(v.Total.ToString("N2"), fNormal, bg, Element.ALIGN_RIGHT));

                        alterno = !alterno;
                    }

                    // Fila TOTAL
                    for (int i = 0; i < 3; i++)
                    {
                        tabla.AddCell(new PdfPCell(new Phrase(""))
                        {
                            Border = Rectangle.TOP_BORDER,
                            BorderColorTop = PdfGris,
                            Padding = 6f
                        });
                    }

                    tabla.AddCell(new PdfPCell(new Phrase("TOTAL", fTotalLbl))
                    {
                        HorizontalAlignment = Element.ALIGN_RIGHT,
                        Border = Rectangle.TOP_BORDER,
                        BorderColorTop = PdfGris,
                        Padding = 6f
                    });

                    tabla.AddCell(new PdfPCell(new Phrase(totalGeneral.ToString("N2"), fTotal))
                    {
                        HorizontalAlignment = Element.ALIGN_RIGHT,
                        Border = Rectangle.TOP_BORDER,
                        BorderColorTop = PdfGris,
                        Padding = 6f
                    });

                    doc.Add(tabla);
                    doc.Close();
                }

                AbrirArchivo(ruta);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al generar PDF:\n{ex.Message}", "Error PDF", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private static PdfPCell Celda(string texto, Font fuente, BaseColor bg, int align)
        {
            return new PdfPCell(new Phrase(texto, fuente))
            {
                BackgroundColor = bg,
                HorizontalAlignment = align,
                VerticalAlignment = Element.ALIGN_MIDDLE,
                Padding = 6f,
                Border = Rectangle.NO_BORDER
            };
        }

        private static (string rango, string _) ObtenerRangoTexto(List<Venta> ventas)
        {
            if (!ventas.Any()) return ("Sin ventas en el período", "");
            var min = ventas.Min(v => v.Fecha);
            var max = ventas.Max(v => v.Fecha);
            return min.Date == max.Date
                ? ($"Período: {min:dd/MM/yyyy}", "")
                : ($"Período: {min:dd/MM/yyyy} — {max:dd/MM/yyyy}", "");
        }

        private static string GuardarDialogo(string nombreSugerido)
        {
            bool esExcel = nombreSugerido.EndsWith(".xlsx");
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                FileName = nombreSugerido,
                Filter = esExcel ? "Excel (*.xlsx)|*.xlsx" : "PDF (*.pdf)|*.pdf",
                Title = "Guardar reporte",
                DefaultExt = esExcel ? "xlsx" : "pdf"
            };
            return dlg.ShowDialog() == true ? dlg.FileName : null;
        }

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
            catch { /* no crítico */ }
        }
    }
}