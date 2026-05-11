using ClosedXML.Excel;
using Microsoft.Win32;
using Project_v1.Servicios;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Project_v1.Vista
{
    public partial class HistorialBackupsWindow : Window
    {
        private List<RegistroBackupVista> _lista = new();

        public HistorialBackupsWindow()
        {
            InitializeComponent();
            CargarHistorial();
        }

        private void CargarHistorial()
        {
            _lista = BackupService.ObtenerHistorial()
                .Select(x => new RegistroBackupVista
                {
                    FechaTexto = x.Fecha.ToString("dd/MM/yyyy HH:mm:ss"),
                    Exitoso = x.Exitoso,
                    Mensaje = x.Mensaje,
                    RutaArchivo = string.IsNullOrWhiteSpace(x.RutaArchivo) ? "-" : x.RutaArchivo
                })
                .ToList();

            GridHistorial.ItemsSource = _lista;
            TxtResumen.Text = $"{_lista.Count} registro(s) mostrados";
        }

        private void BtnActualizar_Click(object sender, RoutedEventArgs e)
            => CargarHistorial();

        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
            => Close();

        // ── Excel ────────────────────────────────────────────────
        private void BtnExportarExcel_Click(object sender, RoutedEventArgs e)
        {
            if (_lista.Count == 0)
            {
                MessageBox.Show("No hay registros para exportar.",
                    "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

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

                ws.Cell("A1").Value = "HISTORIAL DE BACKUPS";
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
                foreach (var r in _lista)
                {
                    var bg = row % 2 == 0 ? XLColor.FromHtml("#F8FAFC") : XLColor.White;

                    ws.Cell(row, 1).Value = r.FechaTexto;
                    ws.Cell(row, 2).Value = r.Exitoso ? "OK" : "Error";
                    ws.Cell(row, 2).Style.Font.SetFontColor(
                                            r.Exitoso ? XLColor.FromHtml("#15803D") : XLColor.FromHtml("#B91C1C"));
                    ws.Cell(row, 2).Style.Font.Bold = true;

                    ws.Cell(row, 3).Value = r.Mensaje;
                    ws.Cell(row, 4).Value = r.RutaArchivo;
                    ws.Cell(row, 4).Style.Font.SetFontColor(XLColor.Gray);

                    ws.Range(row, 1, row, 4).Style
                        .Fill.SetBackgroundColor(bg)
                        .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                        .Border.SetInsideBorder(XLBorderStyleValues.Thin);
                    row++;
                }

                ws.Cell(row, 1).Value = $"Total: {_lista.Count} registros";
                ws.Range(row, 1, row, 4).Merge();
                ws.Cell(row, 1).Style
                    .Font.SetBold(true).Font.SetFontColor(XLColor.Gray)
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#F1F5F9"))
                    .Border.SetOutsideBorder(XLBorderStyleValues.Medium);

                ws.Columns().AdjustToContents();
                wb.SaveAs(dialog.FileName);

                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = dialog.FileName,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al exportar Excel: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── PDF ──────────────────────────────────────────────────
        private void BtnExportarPdf_Click(object sender, RoutedEventArgs e)
        {
            if (_lista.Count == 0)
            {
                MessageBox.Show("No hay registros para exportar.",
                    "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new SaveFileDialog
            {
                FileName = $"Historial_Backups_{DateTime.Now:yyyyMMdd}",
                DefaultExt = ".pdf",
                Filter = "PDF files (.pdf)|*.pdf"
            };
            if (dialog.ShowDialog() != true) return;

            try
            {
                QuestPDF.Settings.License = LicenseType.Community;

                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4.Landscape());
                        page.Margin(32);
                        page.DefaultTextStyle(x => x.FontSize(10));

                        page.Header().Column(col =>
                        {
                            col.Item().Text("HISTORIAL DE BACKUPS")
                                .FontSize(16).Bold()
                                .FontColor(Color.FromHex("#1E2937"));
                            col.Item().Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}  |  {_lista.Count} registros")
                                .FontSize(9).FontColor(Color.FromHex("#64748B"));
                            col.Item().PaddingTop(6).LineHorizontal(0.5f)
                                .LineColor(Color.FromHex("#E2E8F0"));
                        });

                        page.Content().PaddingTop(12).Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.ConstantColumn(110);
                                cols.ConstantColumn(60);
                                cols.RelativeColumn(2);
                                cols.RelativeColumn(3);
                            });

                            table.Header(header =>
                            {
                                foreach (var h in new[] { "Fecha", "Estado", "Mensaje", "Ruta" })
                                {
                                    header.Cell().Background("#1E2937").Padding(6)
                                        .Text(h).FontColor(Colors.White).Bold().FontSize(10);
                                }
                            });

                            int i = 0;
                            foreach (var r in _lista)
                            {
                                var bg = i % 2 == 0 ? "#F8FAFC" : "#FFFFFF";
                                i++;

                                table.Cell().Background(bg).Padding(6)
                                    .Text(r.FechaTexto).FontColor("#1E2937");

                                var estadoColor = r.Exitoso ? "#15803D" : "#B91C1C";
                                var estadoBg = r.Exitoso ? "#DCFCE7" : "#FEE2E2";
                                table.Cell().Background(bg).Padding(4).AlignLeft()
                                    .Element(el => el
                                        .Background(estadoBg)
                                        .Padding(4)
                                        .Text(r.Exitoso ? "OK" : "Error")
                                        .Bold().FontSize(10).FontColor(estadoColor));

                                table.Cell().Background(bg).Padding(6)
                                    .Text(r.Mensaje).FontColor("#1E2937");

                                table.Cell().Background(bg).Padding(6)
                                    .Text(r.RutaArchivo).FontSize(8).FontColor("#64748B");
                            }
                        });

                        page.Footer().AlignCenter()
                            .Text(t =>
                            {
                                t.Span("Página ").FontSize(9).FontColor("#64748B");
                                t.CurrentPageNumber().FontSize(9).FontColor("#64748B");
                                t.Span(" de ").FontSize(9).FontColor("#64748B");
                                t.TotalPages().FontSize(9).FontColor("#64748B");
                            });
                    });
                }).GeneratePdf(dialog.FileName);

                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = dialog.FileName,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al exportar PDF: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private class RegistroBackupVista
        {
            public string FechaTexto { get; set; } = string.Empty;
            public bool Exitoso { get; set; }
            public string Mensaje { get; set; } = string.Empty;
            public string RutaArchivo { get; set; } = string.Empty;
        }
    }
}