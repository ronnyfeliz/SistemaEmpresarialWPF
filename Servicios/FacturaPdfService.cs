using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Project_v1.Modelos;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.IO.Compression;

namespace Project_v1.Servicios
{
    public static class FacturaPdfService
    {
        // ═══════════════════════════════════════════════════
        // 1. EXPORTAR VARIAS VENTAS EN UN ZIP
        // ═══════════════════════════════════════════════════
        public static void ExportarHistorialZip(List<Venta> ventas)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            using var db = new AppDbContext();
            var negocio = db.Negocios.FirstOrDefault();

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                FileName = $"Historial_Ventas_{DateTime.Now:yyyyMMdd}",
                DefaultExt = ".zip",
                Filter = "Zip files (.zip)|*.zip"
            };

            if (dialog.ShowDialog() != true) return;

            try
            {
                using var zipStream = new FileStream(dialog.FileName, FileMode.Create);
                using var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, true);

                foreach (var v in ventas)
                {
                    var ventaCompleta = db.Ventas
                       .Include(x => x.Usuario)
                       .Include(x => x.Detalles).ThenInclude(d => d.Producto)
                       .Include(x => x.DetallesServicio).ThenInclude(d => d.Servicio)  // ← AGREGAR
                       .FirstOrDefault(x => x.Id == v.Id);

                    if (ventaCompleta == null) continue;

                    var entry = archive.CreateEntry(
                        $"Historial_Ventas/{ventaCompleta.Fecha:yyyy}/{ventaCompleta.Fecha:MM}/{ventaCompleta.Fecha:dd}/Factura_{ventaCompleta.NumFactura}.pdf");

                    using var entryStream = entry.Open();
                    ComponerFactura(ventaCompleta, negocio).GeneratePdf(entryStream);
                }

                AbrirCarpetaContenedora(dialog.FileName);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error al exportar ZIP: {ex.Message}");
            }
        }

        // ═══════════════════════════════════════════════════
        // 2. GUARDAR UNA SOLA FACTURA DE VENTA
        // ═══════════════════════════════════════════════════
        public static void GenerarYGuardar(Venta venta)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            using var db = new AppDbContext();
            var ventaCompleta = db.Ventas
                .Include(v => v.Usuario)
                .Include(v => v.Detalles).ThenInclude(d => d.Producto)
                .Include(v => v.DetallesServicio).ThenInclude(d => d.Servicio)  // ← AGREGAR
                .FirstOrDefault(v => v.Id == venta.Id) ?? venta;

            var negocio = db.Negocios.FirstOrDefault();

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                FileName = $"Factura_{ventaCompleta.NumFactura}",
                DefaultExt = ".pdf",
                Filter = "PDF documents (.pdf)|*.pdf"
            };

            if (dialog.ShowDialog() != true) return;

            try
            {
                ComponerFactura(ventaCompleta, negocio).GeneratePdf(dialog.FileName);
                AbrirArchivo(dialog.FileName);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error al guardar PDF: {ex.Message}");
            }
        }

        // ═══════════════════════════════════════════════════
        // 3. DISEÑO DE FACTURA DE VENTA
        // ═══════════════════════════════════════════════════
        private static IDocument ComponerFactura(Venta venta, Negocio? negocio)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A5);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            if (!string.IsNullOrWhiteSpace(negocio?.RutaLogo) && File.Exists(negocio.RutaLogo))
                                row.ConstantItem(55).Image(negocio.RutaLogo).FitArea();

                            row.RelativeItem().PaddingLeft(10).Column(c =>
                            {
                                c.Item().Text(negocio?.NombreNegocio ?? "Mi Negocio")
                                    .FontSize(18).Bold().FontColor("#1A2E4A");
                                if (!string.IsNullOrWhiteSpace(negocio?.Slogan))
                                    c.Item().Text(negocio.Slogan)
                                        .FontSize(9).Italic().FontColor("#7F8C8D");
                            });
                        });

                        col.Item().PaddingTop(6).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                if (!string.IsNullOrWhiteSpace(negocio?.RNC))
                                    c.Item().Text($"RNC: {negocio.RNC}").FontSize(8).FontColor("#555555");
                                if (!string.IsNullOrWhiteSpace(negocio?.Telefono))
                                    c.Item().Text($"Tel: {negocio.Telefono}").FontSize(8).FontColor("#555555");
                                if (!string.IsNullOrWhiteSpace(negocio?.Email))
                                    c.Item().Text($"Email: {negocio.Email}").FontSize(8).FontColor("#555555");
                            });
                            row.RelativeItem().AlignRight().Column(c =>
                            {
                                if (!string.IsNullOrWhiteSpace(negocio?.Direccion))
                                    c.Item().Text(negocio.Direccion).FontSize(8).FontColor("#555555").AlignRight();
                                if (!string.IsNullOrWhiteSpace(negocio?.SitioWeb))
                                    c.Item().Text(negocio.SitioWeb).FontSize(8).FontColor("#555555").AlignRight();
                            });
                        });

                        col.Item().PaddingTop(6).LineHorizontal(1.5f).LineColor("#1A2E4A");
                    });

                    page.Content().PaddingTop(14).Column(col =>
                    {
                        // ── Encabezado de factura ──────────────────
                        col.Item().Background("#F0F4F8").Padding(10).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("FACTURA").FontSize(15).Bold().FontColor("#1A2E4A");
                                c.Item().Text($"N°: {venta.NumFactura}").FontSize(10).Bold().FontColor("#2C3E50");
                            });
                            row.RelativeItem().AlignRight().Column(c =>
                            {
                                c.Item().Text($"Fecha:   {venta.Fecha:dd/MM/yyyy}").FontSize(9).AlignRight();
                                c.Item().Text($"Hora:    {venta.Fecha:HH:mm}").FontSize(9).AlignRight();
                                c.Item().Text($"Cajero:  {venta.Usuario?.Nombre ?? "-"}").FontSize(9).AlignRight();
                            });
                        });

                        // ── Determinar qué hay ────────────────────
                        bool tieneProductos = venta.Detalles?.Any() == true;
                        bool tieneServicios = venta.DetallesServicio?.Any() == true;

                        // ── Tabla de PRODUCTOS ────────────────────
                        if (tieneProductos)
                        {
                            if (tieneServicios)
                            {
                                // Solo mostrar encabezado de sección si hay ambos
                                col.Item().PaddingTop(10).Background("#2C3E50").Padding(6)
                                    .Text("Productos").FontColor(Colors.White).FontSize(9).Bold();
                            }
                            else
                            {
                                col.Item().PaddingTop(10);
                            }

                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(cols =>
                                {
                                    cols.RelativeColumn(4);
                                    cols.RelativeColumn(1);
                                    cols.RelativeColumn(2);
                                    cols.RelativeColumn(2);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Background("#1A2E4A").Padding(6)
                                        .Text("Producto").FontColor(Colors.White).Bold().FontSize(9);
                                    header.Cell().Background("#1A2E4A").Padding(6)
                                        .Text("Cant.").FontColor(Colors.White).Bold().FontSize(9).AlignCenter();
                                    header.Cell().Background("#1A2E4A").Padding(6)
                                        .Text("Precio").FontColor(Colors.White).Bold().FontSize(9).AlignRight();
                                    header.Cell().Background("#1A2E4A").Padding(6)
                                        .Text("Total").FontColor(Colors.White).Bold().FontSize(9).AlignRight();
                                });

                                bool alt = false;
                                /* Se usa una colección vacía como respaldo para evitar advertencias
                                   del compilador si la navegación llega nula.*/
                                foreach (var d in venta.Detalles ?? Enumerable.Empty<DetalleVenta>())

                                {
                                    var bg = alt ? "#F8F9FA" : "#FFFFFF";
                                    alt = !alt;

                                    table.Cell().Background(bg).BorderBottom(0.5f).BorderColor("#DDDDDD")
                                        .Padding(6).Text(d.Producto?.Nombre ?? "-").FontSize(9);
                                    table.Cell().Background(bg).BorderBottom(0.5f).BorderColor("#DDDDDD")
                                        .Padding(6).Text(d.Cantidad.ToString()).FontSize(9).AlignCenter();
                                    table.Cell().Background(bg).BorderBottom(0.5f).BorderColor("#DDDDDD")
                                        .Padding(6).Text(d.PrecioUnitario.ToString("C")).FontSize(9).AlignRight();
                                    table.Cell().Background(bg).BorderBottom(0.5f).BorderColor("#DDDDDD")
                                        .Padding(6).Text(d.Subtotal.ToString("C")).FontSize(9).AlignRight();
                                }
                            });
                        }

                        // ── Tabla de SERVICIOS ────────────────────
                        if (tieneServicios)
                        {
                            if (tieneProductos)
                            {
                                col.Item().PaddingTop(10).Background("#1A5276").Padding(6)
                                    .Text("Servicios").FontColor(Colors.White).FontSize(9).Bold();
                            }
                            else
                            {
                                col.Item().PaddingTop(10);
                            }

                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(cols =>
                                {
                                    cols.RelativeColumn(4);
                                    cols.RelativeColumn(1);
                                    cols.RelativeColumn(2);
                                    cols.RelativeColumn(2);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Background("#1A2E4A").Padding(6)
                                        .Text("Servicio").FontColor(Colors.White).Bold().FontSize(9);
                                    header.Cell().Background("#1A2E4A").Padding(6)
                                        .Text("Cant.").FontColor(Colors.White).Bold().FontSize(9).AlignCenter();
                                    header.Cell().Background("#1A2E4A").Padding(6)
                                        .Text("Precio").FontColor(Colors.White).Bold().FontSize(9).AlignRight();
                                    header.Cell().Background("#1A2E4A").Padding(6)
                                        .Text("Total").FontColor(Colors.White).Bold().FontSize(9).AlignRight();
                                });

                                bool alt = false;
                                /* Se usa una colección vacía como respaldo para evitar advertencias
                                   del compilador si la navegación llega nula.*/
                                foreach (var d in venta.DetallesServicio ?? Enumerable.Empty<DetalleServicioVenta>())
                                {
                                    var bg = alt ? "#F8F9FA" : "#FFFFFF";
                                    alt = !alt;

                                    table.Cell().Background(bg).BorderBottom(0.5f).BorderColor("#DDDDDD")
                                        .Padding(6).Text(d.Servicio?.Nombre ?? "-").FontSize(9);
                                    table.Cell().Background(bg).BorderBottom(0.5f).BorderColor("#DDDDDD")
                                        .Padding(6).Text(d.Cantidad.ToString()).FontSize(9).AlignCenter();
                                    table.Cell().Background(bg).BorderBottom(0.5f).BorderColor("#DDDDDD")
                                        .Padding(6).Text(d.PrecioUnitario.ToString("C")).FontSize(9).AlignRight();
                                    table.Cell().Background(bg).BorderBottom(0.5f).BorderColor("#DDDDDD")
                                        .Padding(6).Text(d.Subtotal.ToString("C")).FontSize(9).AlignRight();
                                }
                            });
                        }

                        // ── Total ─────────────────────────────────
                        col.Item().PaddingTop(2).Background("#F0F4F8").Padding(10).Row(row =>
                        {
                            row.RelativeItem();
                            row.AutoItem().Column(c =>
                            {
                                c.Item().Text($"TOTAL:  {venta.Total:C}")
                                    .FontSize(15).Bold().FontColor("#27AE60").AlignRight();
                            });
                        });
                    });

                    page.Footer().Column(col =>
                    {
                        col.Item().LineHorizontal(0.5f).LineColor("#CCCCCC");
                        col.Item().PaddingTop(5).AlignCenter()
                            .Text($"Gracias por su compra  •  {negocio?.NombreNegocio ?? ""}")
                            .FontSize(8).Italic().FontColor("#7F8C8D");
                    });
                });
            });
        }

        // ═══════════════════════════════════════════════════
        // 4. RECIBO DE PAGO — CLIENTE DEUDOR (archivo)
        // ═══════════════════════════════════════════════════
        public static void GenerarReciboPago(ClienteDeudor cliente)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            using var db = new AppDbContext();
            var negocio = db.Negocios.FirstOrDefault();

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                FileName = $"Recibo_{cliente.Nombre.Replace(" ", "_")}_{DateTime.Now:yyyyMMddHHmmss}",
                DefaultExt = ".pdf",
                Filter = "PDF documents (.pdf)|*.pdf"
            };

            if (dialog.ShowDialog() != true) return;

            bool moraAplicada = cliente.TieneMora && DateTime.Today > cliente.FechaPago;
            decimal totalPagado = cliente.MontoDeuda + (moraAplicada ? cliente.MontoMora : 0);

            try
            {
                using var stream = new FileStream(dialog.FileName, FileMode.Create);
                ComponerRecibo(cliente, negocio, moraAplicada, totalPagado, stream);
                AbrirArchivo(dialog.FileName);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error al generar recibo: {ex.Message}");
            }
        }

        // ═══════════════════════════════════════════════════
        // 5. DISEÑO DE RECIBO — escribe directo al stream
        // ═══════════════════════════════════════════════════
        public static void ComponerRecibo(ClienteDeudor cliente, Negocio? negocio,
                                          bool moraAplicada, decimal totalPagado, Stream destino)
        {
            var ahora = cliente.FechaPagado ?? DateTime.Now;
            string nombre = cliente.Nombre;
            string cedula = cliente.Cedula;
            string telefono = cliente.Telefono;
            DateTime fechaInicio = cliente.FechaInicio;
            DateTime fechaPago = cliente.FechaPago;
            decimal montoDeuda = cliente.MontoDeuda;
            decimal montoMora = cliente.MontoMora;

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A5);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            if (!string.IsNullOrWhiteSpace(negocio?.RutaLogo) && File.Exists(negocio.RutaLogo))
                                row.ConstantItem(55).Image(negocio.RutaLogo).FitArea();

                            row.RelativeItem().PaddingLeft(10).Column(c =>
                            {
                                c.Item().Text(negocio?.NombreNegocio ?? "Mi Negocio")
                                    .FontSize(18).Bold().FontColor("#1A2E4A");
                                if (!string.IsNullOrWhiteSpace(negocio?.Slogan))
                                    c.Item().Text(negocio.Slogan)
                                        .FontSize(9).Italic().FontColor("#7F8C8D");
                            });
                        });

                        col.Item().PaddingTop(6).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                if (!string.IsNullOrWhiteSpace(negocio?.RNC))
                                    c.Item().Text($"RNC: {negocio.RNC}").FontSize(8).FontColor("#555555");
                                if (!string.IsNullOrWhiteSpace(negocio?.Telefono))
                                    c.Item().Text($"Tel: {negocio.Telefono}").FontSize(8).FontColor("#555555");
                                if (!string.IsNullOrWhiteSpace(negocio?.Email))
                                    c.Item().Text($"Email: {negocio.Email}").FontSize(8).FontColor("#555555");
                            });
                            row.RelativeItem().AlignRight().Column(c =>
                            {
                                if (!string.IsNullOrWhiteSpace(negocio?.Direccion))
                                    c.Item().Text(negocio.Direccion).FontSize(8).FontColor("#555555").AlignRight();
                                if (!string.IsNullOrWhiteSpace(negocio?.SitioWeb))
                                    c.Item().Text(negocio.SitioWeb).FontSize(8).FontColor("#555555").AlignRight();
                            });
                        });

                        col.Item().PaddingTop(6).LineHorizontal(1.5f).LineColor("#1A2E4A");
                    });

                    page.Content().PaddingTop(14).Column(col =>
                    {
                        col.Item().Background("#F0F4F8").Padding(10).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("RECIBO DE PAGO").FontSize(15).Bold().FontColor("#1A2E4A");
                                c.Item().Text($"Fecha: {ahora:dd/MM/yyyy}   Hora: {ahora:HH:mm}")
                                    .FontSize(9).FontColor("#555555");
                            });
                            row.ConstantItem(75).Border(2).BorderColor("#27AE60")
                                .Background("#EAFAF1").AlignCenter().AlignMiddle()
                                .Text("✓ PAGADO").FontSize(11).Bold().FontColor("#27AE60");
                        });

                        col.Item().PaddingTop(12).Text("DATOS DEL CLIENTE")
                            .FontSize(9).Bold().FontColor("#7F8C8D");

                        col.Item().PaddingTop(5).Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn(2);
                                cols.RelativeColumn(3);
                            });

                            void Fila(string label, string valor, bool sombreado = false)
                            {
                                var bg = sombreado ? "#F8F9FA" : "#FFFFFF";
                                table.Cell().Background(bg).BorderBottom(0.5f).BorderColor("#EEEEEE")
                                    .Padding(5).Text(label).Bold().FontSize(9).FontColor("#555555");
                                table.Cell().Background(bg).BorderBottom(0.5f).BorderColor("#EEEEEE")
                                    .Padding(5).Text(valor).FontSize(9);
                            }

                            Fila("Nombre", nombre, false);
                            Fila("Cédula", cedula, true);
                            Fila("Teléfono", telefono, false);
                            Fila("Fecha deuda", fechaInicio.ToString("dd/MM/yyyy"), true);
                            Fila("Fecha límite", fechaPago.ToString("dd/MM/yyyy"), false);
                            Fila("Fecha de pago", ahora.ToString("dd/MM/yyyy"), true);
                        });

                        col.Item().PaddingTop(14).Text("DESGLOSE DEL PAGO")
                            .FontSize(9).Bold().FontColor("#7F8C8D");

                        col.Item().PaddingTop(5).Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn(3);
                                cols.RelativeColumn(2);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background("#1A2E4A").Padding(6)
                                    .Text("Concepto").FontColor(Colors.White).Bold().FontSize(9);
                                header.Cell().Background("#1A2E4A").Padding(6)
                                    .Text("Monto").FontColor(Colors.White).Bold().FontSize(9).AlignRight();
                            });

                            table.Cell().BorderBottom(0.5f).BorderColor("#DDDDDD")
                                .Padding(6).Text("Deuda principal").FontSize(9);
                            table.Cell().BorderBottom(0.5f).BorderColor("#DDDDDD")
                                .Padding(6).Text(montoDeuda.ToString("C")).FontSize(9).AlignRight();

                            if (moraAplicada)
                            {
                                table.Cell().Background("#FEF9F9").BorderBottom(0.5f).BorderColor("#DDDDDD")
                                    .Padding(6).Text("Mora aplicada").FontSize(9).FontColor("#E74C3C");
                                table.Cell().Background("#FEF9F9").BorderBottom(0.5f).BorderColor("#DDDDDD")
                                    .Padding(6).Text(montoMora.ToString("C"))
                                    .FontSize(9).AlignRight().FontColor("#E74C3C");
                            }
                        });

                        col.Item().PaddingTop(2).Background("#F0F4F8").Padding(10).Row(row =>
                        {
                            row.RelativeItem();
                            row.AutoItem().Text($"TOTAL PAGADO:  {totalPagado:C}")
                                .FontSize(15).Bold().FontColor("#27AE60").AlignRight();
                        });
                    });

                    page.Footer().Column(col =>
                    {
                        col.Item().LineHorizontal(0.5f).LineColor("#CCCCCC");
                        col.Item().PaddingTop(5).AlignCenter()
                            .Text($"Gracias por su pago  •  {negocio?.NombreNegocio ?? ""}")
                            .FontSize(8).Italic().FontColor("#7F8C8D");
                    });
                });
            })
            .GeneratePdf(destino);
        }

        // ═══════════════════════════════════════════════════
        // 6. EXPORTAR LISTA DE CLIENTES DEUDORES ACTIVOS A PDF
        // ═══════════════════════════════════════════════════
        public static void ExportarDeudoresActivos(List<ClienteDeudor> clientes)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            using var db = new AppDbContext();
            var negocio = db.Negocios.FirstOrDefault();

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                FileName = $"Clientes_Deudores_{DateTime.Now:yyyyMMdd}",
                DefaultExt = ".pdf",
                Filter = "PDF documents (.pdf)|*.pdf"
            };

            if (dialog.ShowDialog() != true) return;

            try
            {
                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(1.5f, Unit.Centimetre);
                        page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                        page.Header().Column(col =>
                        {
                            col.Item().Row(row =>
                            {
                                if (!string.IsNullOrWhiteSpace(negocio?.RutaLogo) && File.Exists(negocio.RutaLogo))
                                    row.ConstantItem(55).Image(negocio.RutaLogo).FitArea();

                                row.RelativeItem().PaddingLeft(10).Column(c =>
                                {
                                    c.Item().Text(negocio?.NombreNegocio ?? "Mi Negocio")
                                        .FontSize(18).Bold().FontColor("#1A2E4A");
                                    if (!string.IsNullOrWhiteSpace(negocio?.Slogan))
                                        c.Item().Text(negocio.Slogan)
                                            .FontSize(9).Italic().FontColor("#7F8C8D");
                                });
                            });

                            col.Item().PaddingTop(6).Row(row =>
                            {
                                row.RelativeItem().Column(c =>
                                {
                                    if (!string.IsNullOrWhiteSpace(negocio?.RNC))
                                        c.Item().Text($"RNC: {negocio.RNC}").FontSize(8).FontColor("#555555");
                                    if (!string.IsNullOrWhiteSpace(negocio?.Telefono))
                                        c.Item().Text($"Tel: {negocio.Telefono}").FontSize(8).FontColor("#555555");
                                });
                                row.RelativeItem().AlignRight().Column(c =>
                                {
                                    if (!string.IsNullOrWhiteSpace(negocio?.Direccion))
                                        c.Item().Text(negocio.Direccion).FontSize(8).FontColor("#555555").AlignRight();
                                });
                            });

                            col.Item().PaddingTop(6).LineHorizontal(1.5f).LineColor("#1A2E4A");
                        });

                        page.Content().PaddingTop(14).Column(col =>
                        {
                            col.Item().Background("#F0F4F8").Padding(10).Row(row =>
                            {
                                row.RelativeItem().Column(c =>
                                {
                                    c.Item().Text("CLIENTES DEUDORES ACTIVOS")
                                        .FontSize(15).Bold().FontColor("#1A2E4A");
                                    c.Item().Text($"Generado: {DateTime.Now:dd/MM/yyyy  HH:mm}")
                                        .FontSize(9).FontColor("#555555");
                                });
                                row.AutoItem().AlignRight().AlignMiddle().Column(c =>
                                {
                                    c.Item().Text($"Total registros: {clientes.Count}")
                                        .FontSize(10).Bold().FontColor("#2C3E50").AlignRight();
                                    var totalDeuda = clientes.Sum(x => x.MontoDeuda + (x.TieneMora && DateTime.Today > x.FechaPago ? x.MontoMora : 0));
                                    c.Item().Text($"Deuda total: {totalDeuda:C}")
                                        .FontSize(10).Bold().FontColor("#E74C3C").AlignRight();
                                });
                            });

                            col.Item().PaddingTop(10).Table(table =>
                            {
                                table.ColumnsDefinition(cols =>
                                {
                                    cols.RelativeColumn(3);
                                    cols.RelativeColumn(2);
                                    cols.RelativeColumn(2);
                                    cols.RelativeColumn(2);
                                    cols.RelativeColumn(2);
                                    cols.RelativeColumn(2);
                                });

                                table.Header(header =>
                                {
                                    void Th(string texto) =>
                                        header.Cell().Background("#1A2E4A").Padding(6)
                                            .Text(texto).FontColor(Colors.White).Bold().FontSize(8);

                                    Th("Nombre");
                                    Th("Cédula");
                                    Th("Teléfono");
                                    Th("Monto");
                                    Th("Vence");
                                    Th("Estado");
                                });

                                bool alt = false;
                                foreach (var c in clientes)
                                {
                                    var bg = alt ? "#F8F9FA" : "#FFFFFF";
                                    alt = !alt;

                                    bool vencido = DateTime.Today > c.FechaPago;
                                    bool moraAplicada = c.TieneMora && vencido;
                                    decimal totalCliente = c.MontoDeuda + (moraAplicada ? c.MontoMora : 0);
                                    string estadoTexto = vencido ? "VENCIDO" : "Al día";
                                    string estadoColor = vencido ? "#E74C3C" : "#27AE60";

                                    void Td(string texto, string? color = null, bool negrita = false)
                                    {
                                        var cell = table.Cell().Background(bg)
                                            .BorderBottom(0.5f).BorderColor("#DDDDDD").Padding(5);
                                        var t = cell.Text(texto).FontSize(8);
                                        if (color != null) t.FontColor(color);
                                        if (negrita) t.Bold();
                                    }

                                    Td(c.Nombre);
                                    Td(c.Cedula);
                                    Td(c.Telefono);
                                    Td(totalCliente.ToString("C"), moraAplicada ? "#E74C3C" : null, moraAplicada);
                                    Td(c.FechaPago.ToString("dd/MM/yyyy"), vencido ? "#E74C3C" : null);
                                    Td(estadoTexto, estadoColor, true);
                                }
                            });
                        });

                        page.Footer().Column(col =>
                        {
                            col.Item().LineHorizontal(0.5f).LineColor("#CCCCCC");
                            col.Item().PaddingTop(5).AlignCenter()
                                .Text($"{negocio?.NombreNegocio ?? ""}  •  Reporte de deudores activos  •  {DateTime.Now:dd/MM/yyyy}")
                                .FontSize(8).Italic().FontColor("#7F8C8D");
                        });
                    });
                }).GeneratePdf(dialog.FileName);

                AbrirArchivo(dialog.FileName);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error al exportar PDF: {ex.Message}");
            }
        }

        // ═══════════════════════════════════════════════════
        // 7. EXPORTAR LISTA DE CLIENTES PAGADOS A PDF
        // ═══════════════════════════════════════════════════
        public static void ExportarDeudoresPagados(List<ClienteDeudor> clientes)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            using var db = new AppDbContext();
            var negocio = db.Negocios.FirstOrDefault();

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                FileName = $"Clientes_Pagados_{DateTime.Now:yyyyMMdd}",
                DefaultExt = ".pdf",
                Filter = "PDF documents (.pdf)|*.pdf"
            };

            if (dialog.ShowDialog() != true) return;

            try
            {
                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(1.5f, Unit.Centimetre);
                        page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                        page.Header().Column(col =>
                        {
                            col.Item().Row(row =>
                            {
                                if (!string.IsNullOrWhiteSpace(negocio?.RutaLogo) && File.Exists(negocio.RutaLogo))
                                    row.ConstantItem(55).Image(negocio.RutaLogo).FitArea();

                                row.RelativeItem().PaddingLeft(10).Column(c =>
                                {
                                    c.Item().Text(negocio?.NombreNegocio ?? "Mi Negocio")
                                        .FontSize(18).Bold().FontColor("#1A2E4A");

                                    if (!string.IsNullOrWhiteSpace(negocio?.Slogan))
                                        c.Item().Text(negocio.Slogan)
                                            .FontSize(9).Italic().FontColor("#7F8C8D");
                                });
                            });

                            col.Item().PaddingTop(6).Row(row =>
                            {
                                row.RelativeItem().Column(c =>
                                {
                                    if (!string.IsNullOrWhiteSpace(negocio?.RNC))
                                        c.Item().Text($"RNC: {negocio.RNC}").FontSize(8).FontColor("#555555");
                                    if (!string.IsNullOrWhiteSpace(negocio?.Telefono))
                                        c.Item().Text($"Tel: {negocio.Telefono}").FontSize(8).FontColor("#555555");
                                    if (!string.IsNullOrWhiteSpace(negocio?.Email))
                                        c.Item().Text($"Email: {negocio.Email}").FontSize(8).FontColor("#555555");
                                });

                                row.RelativeItem().AlignRight().Column(c =>
                                {
                                    if (!string.IsNullOrWhiteSpace(negocio?.Direccion))
                                        c.Item().Text(negocio.Direccion).FontSize(8).FontColor("#555555").AlignRight();
                                });
                            });

                            col.Item().PaddingTop(6).LineHorizontal(1.5f).LineColor("#1A2E4A");
                        });

                        page.Content().PaddingTop(14).Column(col =>
                        {
                            col.Item().Background("#F0F4F8").Padding(10).Row(row =>
                            {
                                row.RelativeItem().Column(c =>
                                {
                                    c.Item().Text("CLIENTES PAGADOS")
                                        .FontSize(15).Bold().FontColor("#1A2E4A");

                                    c.Item().Text($"Generado: {DateTime.Now:dd/MM/yyyy  HH:mm}")
                                        .FontSize(9).FontColor("#555555");
                                });

                                row.AutoItem().AlignRight().AlignMiddle().Column(c =>
                                {
                                    c.Item().Text($"Total registros: {clientes.Count}")
                                        .FontSize(10).Bold().FontColor("#2C3E50").AlignRight();

                                    decimal totalRecuperado = clientes.Sum(x =>
                                    {
                                        bool moraAplicada = x.TieneMora &&
                                                            x.FechaPagado.HasValue &&
                                                            x.FechaPagado.Value.Date > x.FechaPago.Date;
                                        return x.MontoDeuda + (moraAplicada ? x.MontoMora : 0);
                                    });

                                    c.Item().Text($"Total recuperado: {totalRecuperado:C}")
                                        .FontSize(10).Bold().FontColor("#27AE60").AlignRight();
                                });
                            });

                            col.Item().PaddingTop(10).Table(table =>
                            {
                                table.ColumnsDefinition(cols =>
                                {
                                    cols.RelativeColumn(3); // Nombre
                                    cols.RelativeColumn(2); // Cédula
                                    cols.RelativeColumn(2); // Teléfono
                                    cols.RelativeColumn(2); // Total pagado
                                    cols.RelativeColumn(2); // Fecha límite
                                    cols.RelativeColumn(2); // Pagado el
                                });

                                table.Header(header =>
                                {
                                    void Th(string texto) =>
                                        header.Cell().Background("#1A2E4A").Padding(6)
                                            .Text(texto).FontColor(Colors.White).Bold().FontSize(8);

                                    Th("Nombre");
                                    Th("Cédula");
                                    Th("Teléfono");
                                    Th("Total pagado");
                                    Th("Fecha límite");
                                    Th("Pagado el");
                                });

                                bool alt = false;
                                foreach (var c in clientes)
                                {
                                    var bg = alt ? "#F8F9FA" : "#FFFFFF";
                                    alt = !alt;

                                    bool moraAplicada = c.TieneMora &&
                                                        c.FechaPagado.HasValue &&
                                                        c.FechaPagado.Value.Date > c.FechaPago.Date;

                                    decimal totalPagado = c.MontoDeuda + (moraAplicada ? c.MontoMora : 0);

                                    void Td(string texto, string? color = null, bool negrita = false)
                                    {
                                        var cell = table.Cell().Background(bg)
                                            .BorderBottom(0.5f).BorderColor("#DDDDDD").Padding(5);

                                        var t = cell.Text(texto).FontSize(8);
                                        if (color != null) t.FontColor(color);
                                        if (negrita) t.Bold();
                                    }

                                    Td(c.Nombre);
                                    Td(c.Cedula);
                                    Td(c.Telefono);
                                    Td(totalPagado.ToString("C"), "#27AE60", true);
                                    Td(c.FechaPago.ToString("dd/MM/yyyy"));
                                    Td(c.FechaPagado?.ToString("dd/MM/yyyy") ?? "-",
                                       "#1A6B3C",
                                       true);
                                }
                            });
                        });

                        page.Footer().Column(col =>
                        {
                            col.Item().LineHorizontal(0.5f).LineColor("#CCCCCC");
                            col.Item().PaddingTop(5).AlignCenter()
                                .Text($"{negocio?.NombreNegocio ?? ""}  •  Reporte de clientes pagados  •  {DateTime.Now:dd/MM/yyyy}")
                                .FontSize(8).Italic().FontColor("#7F8C8D");
                        });
                    });
                }).GeneratePdf(dialog.FileName);

                AbrirArchivo(dialog.FileName);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error al exportar PDF: {ex.Message}");
            }
        }

        // ═══════════════════════════════════════════════════
        // 7. EXPORTAR FICHA DE UN CLIENTE DEUDOR INDIVIDUAL
        // ═══════════════════════════════════════════════════
        public static void ExportarFichaDeudorIndividual(ClienteDeudor c)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            using var db = new AppDbContext();
            var negocio = db.Negocios.FirstOrDefault();

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                FileName = $"Deudor_{c.Nombre.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd}",
                DefaultExt = ".pdf",
                Filter = "PDF documents (.pdf)|*.pdf"
            };

            if (dialog.ShowDialog() != true) return;

            try
            {
                bool vencido = DateTime.Today > c.FechaPago;
                bool moraAplicada = c.TieneMora && vencido;
                decimal total = c.MontoDeuda + (moraAplicada ? c.MontoMora : 0);
                string estado = vencido ? "VENCIDO" : "Al día";
                string estadoColor = vencido ? "#E74C3C" : "#27AE60";

                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(1.5f, Unit.Centimetre);
                        page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                        page.Header().Column(col =>
                        {
                            col.Item().Row(row =>
                            {
                                if (!string.IsNullOrWhiteSpace(negocio?.RutaLogo) && File.Exists(negocio.RutaLogo))
                                    row.ConstantItem(55).Image(negocio.RutaLogo).FitArea();

                                row.RelativeItem().PaddingLeft(10).Column(c2 =>
                                {
                                    c2.Item().Text(negocio?.NombreNegocio ?? "Mi Negocio")
                                        .FontSize(18).Bold().FontColor("#1A2E4A");
                                    if (!string.IsNullOrWhiteSpace(negocio?.Slogan))
                                        c2.Item().Text(negocio.Slogan)
                                            .FontSize(9).Italic().FontColor("#7F8C8D");
                                });
                            });

                            col.Item().PaddingTop(6).Row(row =>
                            {
                                row.RelativeItem().Column(c2 =>
                                {
                                    if (!string.IsNullOrWhiteSpace(negocio?.RNC))
                                        c2.Item().Text($"RNC: {negocio.RNC}").FontSize(8).FontColor("#555555");
                                    if (!string.IsNullOrWhiteSpace(negocio?.Telefono))
                                        c2.Item().Text($"Tel: {negocio.Telefono}").FontSize(8).FontColor("#555555");
                                    if (!string.IsNullOrWhiteSpace(negocio?.Email))
                                        c2.Item().Text($"Email: {negocio.Email}").FontSize(8).FontColor("#555555");
                                });
                                row.RelativeItem().AlignRight().Column(c2 =>
                                {
                                    if (!string.IsNullOrWhiteSpace(negocio?.Direccion))
                                        c2.Item().Text(negocio.Direccion).FontSize(8).FontColor("#555555").AlignRight();
                                });
                            });

                            col.Item().PaddingTop(6).LineHorizontal(1.5f).LineColor("#1A2E4A");
                        });

                        page.Content().PaddingTop(14).Column(col =>
                        {
                            col.Item().Background("#F0F4F8").Padding(10).Row(row =>
                            {
                                row.RelativeItem().Column(c2 =>
                                {
                                    c2.Item().Text("FICHA DE CLIENTE DEUDOR")
                                        .FontSize(15).Bold().FontColor("#1A2E4A");
                                    c2.Item().Text($"Generado: {DateTime.Now:dd/MM/yyyy  HH:mm}")
                                        .FontSize(9).FontColor("#555555");
                                });
                                row.AutoItem().AlignRight().AlignMiddle()
                                    .Text(estado).FontSize(13).Bold().FontColor(estadoColor);
                            });

                            col.Item().PaddingTop(16).Column(info =>
                            {
                                void Fila(string etiqueta, string valor, string? colorValor = null)
                                {
                                    info.Item().BorderBottom(0.5f).BorderColor("#E0E0E0")
                                        .PaddingVertical(7).Row(row =>
                                        {
                                            row.ConstantItem(160)
                                                .Text(etiqueta).FontSize(10).FontColor("#555555");
                                            var t = row.RelativeItem()
                                                .Text(valor).FontSize(10).Bold();
                                            if (colorValor != null) t.FontColor(colorValor);
                                        });
                                }

                                Fila("Nombre", c.Nombre);
                                Fila("Cédula", c.Cedula);
                                Fila("Teléfono", c.Telefono);
                                Fila("Fecha de inicio", c.FechaInicio.ToString("dd/MM/yyyy"));
                                Fila("Fecha límite de pago", c.FechaPago.ToString("dd/MM/yyyy"),
                                                             vencido ? "#E74C3C" : null);
                                Fila("Monto original", c.MontoDeuda.ToString("C"));

                                if (c.TieneMora)
                                    Fila("Mora aplicada",
                                         moraAplicada ? c.MontoMora.ToString("C") : $"{c.MontoMora:C} (pendiente)",
                                         moraAplicada ? "#E74C3C" : "#F39C12");

                                Fila("TOTAL A COBRAR", total.ToString("C"),
                                     moraAplicada ? "#E74C3C" : "#1A2E4A");
                                Fila("Estado", estado, estadoColor);
                            });

                            if (moraAplicada)
                            {
                                col.Item().PaddingTop(20)
                                    .Background("#FFF5F5").Border(1).BorderColor("#E74C3C")
                                    .Padding(10).Column(c2 =>
                                    {
                                        c2.Item().Text("⚠ Mora activa")
                                            .FontSize(10).Bold().FontColor("#E74C3C");
                                        c2.Item().PaddingTop(4)
                                            .Text($"Este cliente superó su fecha límite ({c.FechaPago:dd/MM/yyyy}). " +
                                                  $"Se aplica una mora de {c.MontoMora:C}, " +
                                                  $"llevando el total a {total:C}.")
                                            .FontSize(9).FontColor("#555555");
                                    });
                            }
                        });

                        page.Footer().Column(col =>
                        {
                            col.Item().LineHorizontal(0.5f).LineColor("#CCCCCC");
                            col.Item().PaddingTop(5).AlignCenter()
                                .Text($"{negocio?.NombreNegocio ?? ""}  •  Ficha de deudor  •  {DateTime.Now:dd/MM/yyyy}")
                                .FontSize(8).Italic().FontColor("#7F8C8D");
                        });
                    });
                }).GeneratePdf(dialog.FileName);

                AbrirArchivo(dialog.FileName);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error al exportar ficha: {ex.Message}");
            }
        }

        // ═══════════════════════════════════════════════════
        // EXPORTAR RESUMEN DE VENTAS POR FECHA ESPECÍFICA A PDF
        // ═══════════════════════════════════════════════════
        public static void ExportarResumenPorFecha(
            DateTime fechaConsultada,
            decimal totalGanancias,
            int cantidadVentas,
            int productosVendidos,
            int serviciosVendidos,
            List<Venta> ventas)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            using var db = new AppDbContext();
            var negocio = db.Negocios.FirstOrDefault();

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                FileName = $"Ventas_{fechaConsultada:yyyyMMdd}",
                DefaultExt = ".pdf",
                Filter = "PDF documents (.pdf)|*.pdf"
            };

            if (dialog.ShowDialog() != true) return;

            try
            {
                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(1.5f, Unit.Centimetre);
                        page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                        page.Header().Column(col =>
                        {
                            EncabezadoInstitucional(col, negocio,
                                $"REPORTE DE VENTAS — {fechaConsultada:dd/MM/yyyy}", "#1A2E4A");
                            col.Item().PaddingTop(4)
                                .Text($"Fecha consultada: {fechaConsultada:dddd dd/MM/yyyy}   |   Generado: {DateTime.Now:dd/MM/yyyy HH:mm}")
                                .FontSize(9).FontColor("#64748B");
                        });

                        page.Content().PaddingTop(14).Column(col =>
                        {
                            // ── Tarjetas de métricas ─────────
                            col.Item().PaddingTop(6).Row(row =>
                            {
                                void Tarjeta(string titulo, string valor, string color)
                                {
                                    row.RelativeItem().Border(1).BorderColor(color)
                                        .Background("#FAFAFA").Padding(10).Column(c =>
                                        {
                                            c.Item().Text(titulo).FontSize(8).FontColor("#777777");
                                            c.Item().PaddingTop(4).Text(valor)
                                                .FontSize(16).Bold().FontColor(color);
                                        });
                                }

                                Tarjeta("Ganancias", totalGanancias.ToString("C"), "#27AE60");
                                row.ConstantItem(8);
                                Tarjeta("Ventas", cantidadVentas.ToString(), "#1A2E4A");
                                row.ConstantItem(8);
                                Tarjeta("Productos", productosVendidos.ToString(), "#2980B9");
                                row.ConstantItem(8);
                                Tarjeta("Servicios", serviciosVendidos.ToString(), "#8E44AD");
                            });

                            // ── Tabla de detalle ─────────────
                            col.Item().PaddingTop(16)
                                .Text($"DETALLE DE VENTAS — {fechaConsultada:dd/MM/yyyy}")
                                .FontSize(9).Bold().FontColor("#7F8C8D");

                            col.Item().PaddingTop(6).Table(table =>
                            {
                                table.ColumnsDefinition(cols =>
                                {
                                    cols.RelativeColumn(3); // Factura
                                    cols.RelativeColumn(2); // Fecha
                                    cols.RelativeColumn(1); // Hora
                                    cols.RelativeColumn(1); // Prods
                                    cols.RelativeColumn(1); // Servs
                                    cols.RelativeColumn(2); // Total
                                });

                                table.Header(header =>
                                {
                                    void Th(string texto) =>
                                        header.Cell().Background("#1A2E4A").Padding(6)
                                            .Text(texto).FontColor(Colors.White).Bold().FontSize(8);

                                    Th("N° Factura");
                                    Th("Fecha");
                                    Th("Hora");
                                    Th("Prods.");
                                    Th("Servs.");
                                    Th("Total");
                                });

                                bool alt = false;
                                foreach (var v in ventas.OrderBy(v => v.Fecha))
                                {
                                    var bg = alt ? "#F8F9FA" : "#FFFFFF";
                                    alt = !alt;

                                    var cantProd = db.DetallesVenta
                                        .Where(d => d.VentaId == v.Id)
                                        .Sum(d => (int?)d.Cantidad) ?? 0;
                                    var cantServ = db.DetallesServicioVenta
                                        .Where(d => d.VentaId == v.Id)
                                        .Sum(d => (int?)d.Cantidad) ?? 0;

                                    void Td(string texto, string? color = null, bool bold = false)
                                    {
                                        var cell = table.Cell().Background(bg)
                                            .BorderBottom(0.5f).BorderColor("#DDDDDD").Padding(5);
                                        var t = cell.Text(texto).FontSize(8);
                                        if (color != null) t.FontColor(color);
                                        if (bold) t.Bold();
                                    }

                                    Td(v.NumFactura);
                                    Td(v.Fecha.ToString("dd/MM/yyyy")); // ← fecha explícita
                                    Td(v.Fecha.ToString("HH:mm"));
                                    Td(cantProd.ToString());
                                    Td(cantServ.ToString());
                                    Td(v.Total.ToString("C"), "#27AE60", true);
                                }
                            });

                            // ── Total ────────────────────────
                            col.Item().PaddingTop(2).Background("#F0F4F8").Padding(10).Row(row =>
                            {
                                row.RelativeItem().Text($"Fecha: {fechaConsultada:dd/MM/yyyy}   |   Ventas: {ventas.Count}")
                                    .FontSize(9).FontColor("#555555");
                                row.AutoItem().Text($"TOTAL:  {totalGanancias:C}")
                                    .FontSize(14).Bold().FontColor("#27AE60").AlignRight();
                            });
                        });

                        page.Footer().Column(col =>
                        {
                            col.Item().LineHorizontal(0.5f).LineColor("#CCCCCC");
                            col.Item().PaddingTop(5).AlignCenter()
                                .Text($"{negocio?.NombreNegocio ?? ""}  •  Ventas del {fechaConsultada:dd/MM/yyyy}  •  Generado: {DateTime.Now:dd/MM/yyyy HH:mm}")
                                .FontSize(8).Italic().FontColor("#7F8C8D");
                        });
                    });
                }).GeneratePdf(dialog.FileName);

                AbrirArchivo(dialog.FileName);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error al exportar PDF: {ex.Message}");
            }
        }

        // ═══════════════════════════════════════════════════
        // 8. EXPORTAR RESUMEN DIARIO DE VENTAS A PDF
        // ═══════════════════════════════════════════════════
        public static void ExportarResumenDiario(
            decimal totalGanancias,
            int cantidadVentas,
            int productosVendidos,
            int serviciosVendidos,
            List<Venta> ventas)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            using var db = new AppDbContext();
            var negocio = db.Negocios.FirstOrDefault();

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                FileName = $"Resumen_Ventas_{DateTime.Now:yyyyMMdd}",
                DefaultExt = ".pdf",
                Filter = "PDF documents (.pdf)|*.pdf"
            };

            if (dialog.ShowDialog() != true) return;

            try
            {
                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(1.5f, Unit.Centimetre);
                        page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                        // ── Header del negocio ───────────────
                        page.Header().Column(col =>
                        {
                            col.Item().Row(row =>
                            {
                                if (!string.IsNullOrWhiteSpace(negocio?.RutaLogo) && File.Exists(negocio.RutaLogo))
                                    row.ConstantItem(55).Image(negocio.RutaLogo).FitArea();

                                row.RelativeItem().PaddingLeft(10).Column(c =>
                                {
                                    c.Item().Text(negocio?.NombreNegocio ?? "Mi Negocio")
                                        .FontSize(18).Bold().FontColor("#1A2E4A");
                                    if (!string.IsNullOrWhiteSpace(negocio?.Slogan))
                                        c.Item().Text(negocio.Slogan)
                                            .FontSize(9).Italic().FontColor("#7F8C8D");
                                });
                            });

                            col.Item().PaddingTop(6).Row(row =>
                            {
                                row.RelativeItem().Column(c =>
                                {
                                    if (!string.IsNullOrWhiteSpace(negocio?.RNC))
                                        c.Item().Text($"RNC: {negocio.RNC}").FontSize(8).FontColor("#555555");
                                    if (!string.IsNullOrWhiteSpace(negocio?.Telefono))
                                        c.Item().Text($"Tel: {negocio.Telefono}").FontSize(8).FontColor("#555555");
                                });
                                row.RelativeItem().AlignRight().Column(c =>
                                {
                                    if (!string.IsNullOrWhiteSpace(negocio?.Direccion))
                                        c.Item().Text(negocio.Direccion).FontSize(8).FontColor("#555555").AlignRight();
                                });
                            });

                            col.Item().PaddingTop(6).LineHorizontal(1.5f).LineColor("#1A2E4A");
                        });

                        page.Content().PaddingTop(14).Column(col =>
                        {
                            // ── Título del reporte ───────────
                            col.Item().Background("#F0F4F8").Padding(10).Row(row =>
                            {
                                row.RelativeItem().Column(c =>
                                {
                                    c.Item().Text("RESUMEN DE VENTAS DEL DÍA")
                                        .FontSize(15).Bold().FontColor("#1A2E4A");
                                    c.Item().Text($"Fecha: {DateTime.Now:dd/MM/yyyy}   Generado: {DateTime.Now:HH:mm}")
                                        .FontSize(9).FontColor("#555555");
                                });
                            });

                            // ── Tarjetas de métricas ─────────
                            col.Item().PaddingTop(14).Row(row =>
                            {
                                void Tarjeta(string titulo, string valor, string color)
                                {
                                    row.RelativeItem().Border(1).BorderColor(color)
                                        .Background("#FAFAFA").Padding(10).Column(c =>
                                        {
                                            c.Item().Text(titulo)
                                                .FontSize(8).FontColor("#777777");
                                            c.Item().PaddingTop(4).Text(valor)
                                                .FontSize(16).Bold().FontColor(color);
                                        });
                                }

                                Tarjeta("Ganancias", totalGanancias.ToString("C"), "#27AE60");
                                row.ConstantItem(8);
                                Tarjeta("Ventas", cantidadVentas.ToString(), "#1A2E4A");
                                row.ConstantItem(8);
                                Tarjeta("Productos", productosVendidos.ToString(), "#2980B9");
                                row.ConstantItem(8);
                                Tarjeta("Servicios", serviciosVendidos.ToString(), "#8E44AD");
                            });

                            // ── Tabla de detalle ─────────────
                            col.Item().PaddingTop(16).Text("DETALLE DE VENTAS")
                                .FontSize(9).Bold().FontColor("#7F8C8D");

                            col.Item().PaddingTop(6).Table(table =>
                            {
                                table.ColumnsDefinition(cols =>
                                {
                                    cols.RelativeColumn(3); // Factura
                                    cols.RelativeColumn(1); // Hora
                                    cols.RelativeColumn(2); // Cajero
                                    cols.RelativeColumn(1); // Prods
                                    cols.RelativeColumn(1); // Servs
                                    cols.RelativeColumn(2); // Total
                                });

                                table.Header(header =>
                                {
                                    void Th(string texto) =>
                                        header.Cell().Background("#1A2E4A").Padding(6)
                                            .Text(texto).FontColor(Colors.White).Bold().FontSize(8);

                                    Th("N° Factura");
                                    Th("Hora");
                                    Th("Cajero");
                                    Th("Prods.");
                                    Th("Servs.");
                                    Th("Total");
                                });

                                bool alt = false;
                                foreach (var v in ventas.OrderBy(v => v.Fecha))
                                {
                                    var bg = alt ? "#F8F9FA" : "#FFFFFF";
                                    alt = !alt;

                                    var cantProd = db.DetallesVenta
                                        .Where(d => d.VentaId == v.Id)
                                        .Sum(d => (int?)d.Cantidad) ?? 0;
                                    var cantServ = db.DetallesServicioVenta
                                        .Where(d => d.VentaId == v.Id)
                                        .Sum(d => (int?)d.Cantidad) ?? 0;
                                    var cajero = db.Usuarios.Find(v.UsuarioId)?.Nombre ?? "-";

                                    void Td(string texto, string? color = null, bool bold = false)
                                    {
                                        var cell = table.Cell().Background(bg)
                                            .BorderBottom(0.5f).BorderColor("#DDDDDD").Padding(5);
                                        var t = cell.Text(texto).FontSize(8);
                                        if (color != null) t.FontColor(color);
                                        if (bold) t.Bold();
                                    }

                                    Td(v.NumFactura);
                                    Td(v.Fecha.ToString("HH:mm"));
                                    Td(cajero);
                                    Td(cantProd.ToString());
                                    Td(cantServ.ToString());
                                    Td(v.Total.ToString("C"), "#27AE60", true);
                                }
                            });

                            // ── Línea de total ───────────────
                            col.Item().PaddingTop(2).Background("#F0F4F8").Padding(10).Row(row =>
                            {
                                row.RelativeItem();
                                row.AutoItem().Text($"TOTAL DEL DÍA:  {totalGanancias:C}")
                                    .FontSize(14).Bold().FontColor("#27AE60").AlignRight();
                            });
                        });

                        page.Footer().Column(col =>
                        {
                            col.Item().LineHorizontal(0.5f).LineColor("#CCCCCC");
                            col.Item().PaddingTop(5).AlignCenter()
                                .Text($"{negocio?.NombreNegocio ?? ""}  •  Resumen de ventas  •  {DateTime.Now:dd/MM/yyyy}")
                                .FontSize(8).Italic().FontColor("#7F8C8D");
                        });
                    });
                }).GeneratePdf(dialog.FileName);

                AbrirArchivo(dialog.FileName);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error al exportar PDF: {ex.Message}");
            }

        }


        // ═══════════════════════════════════════════════════
        // 9. EXPORTAR HISTORIAL DE BACKUPS A PDF
        // ═══════════════════════════════════════════════════
        public static void ExportarHistorialBackups(List<(string FechaTexto, bool Exitoso, string Mensaje, string RutaArchivo)> registros)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                FileName = $"Historial_Backups_{DateTime.Now:yyyyMMdd}",
                DefaultExt = ".pdf",
                Filter = "PDF documents (.pdf)|*.pdf"
            };
            if (dialog.ShowDialog() != true) return;

            try
            {
                int ok = registros.Count(r => r.Exitoso);
                int error = registros.Count(r => !r.Exitoso);

                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4.Landscape());
                        page.Margin(1.5f, Unit.Centimetre);
                        page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                        page.Header().Column(col =>
                        {
                            col.Item().Row(row =>
                            {
                                row.RelativeItem().Column(c =>
                                {
                                    c.Item().Text("HISTORIAL DE BACKUPS DEL SISTEMA")
                                        .FontSize(16).Bold().FontColor("#1E2937");
                                    c.Item().Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}  |  {registros.Count} registros  |  OK: {ok}  |  Errores: {error}")
                                        .FontSize(9).FontColor("#64748B");
                                });
                            });
                            col.Item().PaddingTop(6).LineHorizontal(1.5f).LineColor("#1E2937");
                        });

                        page.Content().PaddingTop(12).Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.ConstantColumn(120); // Fecha
                                cols.ConstantColumn(55);  // Estado
                                cols.RelativeColumn(2);   // Mensaje
                                cols.RelativeColumn(3);   // Ruta
                            });

                            table.Header(header =>
                            {
                                void Th(string texto) =>
                                    header.Cell().Background("#1E2937").Padding(7)
                                        .Text(texto).FontColor(Colors.White).Bold().FontSize(9);

                                Th("Fecha");
                                Th("Estado");
                                Th("Mensaje");
                                Th("Ruta");
                            });

                            bool alt = false;
                            foreach (var r in registros)
                            {
                                var bg = alt ? "#F8FAFC" : "#FFFFFF";
                                alt = !alt;

                                var estadoBg = r.Exitoso ? "#DCFCE7" : "#FEE2E2";
                                var estadoColor = r.Exitoso ? "#15803D" : "#B91C1C";
                                var estadoTexto = r.Exitoso ? "OK" : "Error";

                                table.Cell().Background(bg).BorderBottom(0.5f).BorderColor("#E2E8F0")
                                    .Padding(6).Text(r.FechaTexto).FontSize(9).FontColor("#1E2937");

                                table.Cell().Background(bg).BorderBottom(0.5f).BorderColor("#E2E8F0")
                                    .Padding(4).AlignLeft().Element(el =>
                                        el.Background(estadoBg).Padding(4)
                                          .Text(estadoTexto).Bold().FontSize(9).FontColor(estadoColor));

                                table.Cell().Background(bg).BorderBottom(0.5f).BorderColor("#E2E8F0")
                                    .Padding(6).Text(r.Mensaje).FontSize(9).FontColor("#1E2937");

                                table.Cell().Background(bg).BorderBottom(0.5f).BorderColor("#E2E8F0")
                                    .Padding(6).Text(r.RutaArchivo).FontSize(8).FontColor("#64748B");
                            }
                        });

                        page.Footer().Column(col =>
                        {
                            col.Item().LineHorizontal(0.5f).LineColor("#E2E8F0");
                            col.Item().PaddingTop(5).Row(row =>
                            {
                                row.RelativeItem().AlignLeft()
                                    .Text($"OK: {ok}  |  Errores: {error}")
                                    .FontSize(8).FontColor("#64748B");
                                row.RelativeItem().AlignCenter()
                                    .Text(t =>
                                    {
                                        t.Span("Página ").FontSize(8).FontColor("#64748B");
                                        t.CurrentPageNumber().FontSize(8).FontColor("#64748B");
                                        t.Span(" de ").FontSize(8).FontColor("#64748B");
                                        t.TotalPages().FontSize(8).FontColor("#64748B");
                                    });
                                row.RelativeItem().AlignRight()
                                    .Text($"Sistema de Gestión Comercial  •  {DateTime.Now:dd/MM/yyyy}")
                                    .FontSize(8).FontColor("#64748B");
                            });
                        });
                    });
                }).GeneratePdf(dialog.FileName);

                AbrirArchivo(dialog.FileName);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error al exportar PDF: {ex.Message}",
                    "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        // ═══════════════════════════════════════════════════
        // 10. REPORTES DE PROVEEDORES
        // ═══════════════════════════════════════════════════
        public static void ExportarProveedores(List<Proveedor> proveedores)
        {
            QuestPDF.Settings.License = LicenseType.Community;
            using var db = new AppDbContext();
            var negocio = db.Negocios.FirstOrDefault();

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                FileName = $"Proveedores_{DateTime.Now:yyyyMMdd_HHmm}",
                DefaultExt = ".pdf",
                Filter = "PDF documents (.pdf)|*.pdf"
            };
            if (dialog.ShowDialog() != true) return;

            try
            {
                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4.Landscape());
                        page.Margin(1.2f, Unit.Centimetre);
                        page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

                        page.Header().Column(col =>
                        {
                            EncabezadoInstitucional(col, negocio, "LISTA GENERAL DE PROVEEDORES", "#263645");
                            col.Item().PaddingTop(4).Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm} | Adeudados: {proveedores.Count(p => p.DeudaTotal > 0)} | Pagados: {proveedores.Count(p => p.DeudaTotal <= 0)} | Deuda total: {proveedores.Sum(p => p.DeudaTotal):C}")
                                .FontSize(9).FontColor("#64748B");
                        });

                        page.Content().PaddingTop(10).Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.ConstantColumn(45);
                                cols.RelativeColumn(2);
                                cols.RelativeColumn(2);
                                cols.RelativeColumn(1);
                                cols.RelativeColumn(1);
                                cols.RelativeColumn(1);
                                cols.RelativeColumn(1);
                            });

                            PdfHeader(table, "ID", "Nombre", "Tipo", "RNC", "Teléfono", "Deuda", "Estado");

                            bool alt = false;
                            foreach (var p in proveedores)
                            {
                                var bg = alt ? "#F8FAFC" : "#FFFFFF";
                                alt = !alt;
                                PdfCell(table, p.Id.ToString(), bg);
                                PdfCell(table, p.Nombre, bg);
                                PdfCell(table, p.TipoTexto, bg);
                                PdfCell(table, p.RncOCedula, bg);
                                PdfCell(table, p.Telefono, bg);
                                PdfCell(table, p.DeudaTotal.ToString("C"), bg, p.DeudaTotal > 0 ? "#C0392B" : "#1E8449", true);
                                PdfCell(table, p.EstadoTexto, bg, p.DeudaTotal > 0 ? "#C0392B" : "#1E8449", true);
                            }
                        });
                    });
                }).GeneratePdf(dialog.FileName);

                AbrirArchivo(dialog.FileName);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error al exportar PDF: {ex.Message}");
            }
        }

        public static void ExportarTransaccionesProveedor(List<TransaccionProveedor> transacciones, string titulo = "HISTORIAL DE TRANSACCIONES DE PROVEEDORES")
        {
            QuestPDF.Settings.License = LicenseType.Community;
            using var db = new AppDbContext();
            var negocio = db.Negocios.FirstOrDefault();

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                FileName = $"Transacciones_Proveedores_{DateTime.Now:yyyyMMdd_HHmm}",
                DefaultExt = ".pdf",
                Filter = "PDF documents (.pdf)|*.pdf"
            };
            if (dialog.ShowDialog() != true) return;

            try
            {
                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4.Landscape());
                        page.Margin(1.2f, Unit.Centimetre);
                        page.DefaultTextStyle(x => x.FontSize(8).FontFamily("Arial"));

                        page.Header().Column(col =>
                        {
                            EncabezadoInstitucional(col, negocio, titulo, "#1A2E4A");
                            col.Item().PaddingTop(4).Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm} | Transacciones: {transacciones.Count} | Pendiente: {transacciones.Sum(t => t.SaldoPendiente):C}")
                                .FontSize(9).FontColor("#64748B");
                        });

                        page.Content().PaddingTop(10).Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.ConstantColumn(55);
                                cols.ConstantColumn(55);
                                cols.RelativeColumn(2);
                                cols.ConstantColumn(70);
                                cols.ConstantColumn(65);
                                cols.ConstantColumn(75);
                                cols.ConstantColumn(75);
                                cols.ConstantColumn(75);
                                cols.ConstantColumn(75);
                            });

                            PdfHeader(table, "ID Trans.", "ID Prov.", "Proveedor", "Fecha", "Cant.", "Total", "Pagado", "Pendiente", "Estado");

                            bool alt = false;
                            foreach (var t in transacciones.OrderByDescending(t => t.FechaProvision))
                            {
                                var bg = alt ? "#F8FAFC" : "#FFFFFF";
                                alt = !alt;
                                PdfCell(table, t.Id.ToString(), bg);
                                PdfCell(table, t.ProveedorId.ToString(), bg);
                                PdfCell(table, t.Proveedor?.Nombre ?? "-", bg);
                                PdfCell(table, t.FechaProvision.ToString("dd/MM/yyyy"), bg);
                                PdfCell(table, t.CantidadMercancia.ToString(), bg);
                                PdfCell(table, t.MontoTotal.ToString("C"), bg);
                                PdfCell(table, t.MontoPagado.ToString("C"), bg);
                                PdfCell(table, t.SaldoPendiente.ToString("C"), bg, t.SaldoPendiente > 0 ? "#C0392B" : "#1E8449", true);
                                PdfCell(table, t.EstadoPago.ToString(), bg);
                            }
                        });
                    });
                }).GeneratePdf(dialog.FileName);

                AbrirArchivo(dialog.FileName);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error al exportar PDF: {ex.Message}");
            }
        }

        public static void ExportarHistorialProveedor(List<HistorialProveedor> movimientos, string proveedor)
        {
            var transacciones = movimientos
                .Select(h => h.TransaccionProveedor)
                .Where(t => t != null)
                .DistinctBy(t => t!.Id)
                .Select(t => t!)
                .ToList();

            ExportarMovimientosProveedor(movimientos, proveedor, transacciones);
        }

        public static void GenerarComprobanteProveedor(string proveedor, TransaccionProveedor transaccion, decimal montoMovimiento, decimal? saldoAnterior, string titulo)
        {
            QuestPDF.Settings.License = LicenseType.Community;
            using var db = new AppDbContext();
            var negocio = db.Negocios.FirstOrDefault();

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                FileName = $"Comprobante_Proveedor_{proveedor.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmmss}",
                DefaultExt = ".pdf",
                Filter = "PDF documents (.pdf)|*.pdf"
            };
            if (dialog.ShowDialog() != true) return;

            try
            {
                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A5);
                        page.Margin(1.4f, Unit.Centimetre);
                        page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                        page.Header().Column(col =>
                        {
                            EncabezadoInstitucional(col, negocio, titulo, "#1A2E4A");
                            col.Item().PaddingTop(4).Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(9).FontColor("#64748B");
                        });

                        page.Content().PaddingTop(14).Column(col =>
                        {
                            void Fila(string label, string value)
                            {
                                col.Item().PaddingBottom(5).Row(row =>
                                {
                                    row.ConstantItem(115).Text(label).FontColor("#64748B");
                                    row.RelativeItem().Text(value).Bold().FontColor("#263645");
                                });
                            }

                            Fila("Proveedor", proveedor);
                            Fila("ID transaccion", transaccion.Id.ToString());
                            Fila("Fecha provision", transaccion.FechaProvision.ToString("dd/MM/yyyy"));
                            Fila("Cantidad", transaccion.CantidadMercancia.ToString());
                            Fila("Total original", transaccion.MontoTotal.ToString("C"));
                            if (saldoAnterior.HasValue)
                                Fila("Saldo anterior", saldoAnterior.Value.ToString("C"));
                            Fila("Monto pagado", montoMovimiento.ToString("C"));
                            Fila("Saldo restante", transaccion.SaldoPendiente.ToString("C"));
                            Fila("Fecha limite", transaccion.FechaLimitePago?.ToString("dd/MM/yyyy") ?? "-");
                            Fila("Estado", transaccion.EstadoPago.ToString());

                            if (!string.IsNullOrWhiteSpace(transaccion.Descripcion))
                                col.Item().PaddingTop(8).Border(1).BorderColor("#DDE5EC").Padding(8)
                                    .Text(transaccion.Descripcion).FontSize(9).FontColor("#475569");
                        });

                        page.Footer().AlignCenter()
                            .Text("Comprobante de proveedor generado por el sistema")
                            .FontSize(8).Italic().FontColor("#64748B");
                    });
                }).GeneratePdf(dialog.FileName);

                AbrirArchivo(dialog.FileName);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error al generar comprobante: {ex.Message}");
            }
        }

        private static void ExportarMovimientosProveedor(List<HistorialProveedor> movimientos, string proveedor, List<TransaccionProveedor> transacciones)
        {
            QuestPDF.Settings.License = LicenseType.Community;
            using var db = new AppDbContext();
            var negocio = db.Negocios.FirstOrDefault();

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                FileName = $"Historial_{proveedor.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmm}",
                DefaultExt = ".pdf",
                Filter = "PDF documents (.pdf)|*.pdf"
            };
            if (dialog.ShowDialog() != true) return;

            try
            {
                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(1.2f, Unit.Centimetre);
                        page.DefaultTextStyle(x => x.FontSize(8).FontFamily("Arial"));

                        page.Header().Column(col =>
                        {
                            EncabezadoInstitucional(col, negocio, $"HISTORIAL DE PROVEEDOR: {proveedor}", "#566573");
                            col.Item().PaddingTop(4).Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm} | Movimientos: {movimientos.Count}")
                                .FontSize(9).FontColor("#64748B");
                        });

                        page.Content().PaddingTop(10).Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.ConstantColumn(45);
                                cols.ConstantColumn(85);
                                cols.RelativeColumn(1);
                                cols.ConstantColumn(55);
                                cols.RelativeColumn(2);
                                cols.ConstantColumn(70);
                                cols.ConstantColumn(70);
                            });

                            PdfHeader(table, "ID", "Fecha", "Tipo", "Trans.", "Descripción", "Monto", "Saldo");

                            bool alt = false;
                            foreach (var h in movimientos.OrderByDescending(h => h.Fecha))
                            {
                                var bg = alt ? "#F8FAFC" : "#FFFFFF";
                                alt = !alt;
                                PdfCell(table, h.Id.ToString(), bg);
                                PdfCell(table, h.Fecha.ToString("dd/MM/yyyy HH:mm"), bg);
                                PdfCell(table, h.TipoMovimiento.ToString(), bg);
                                PdfCell(table, h.TransaccionProveedorId?.ToString() ?? "-", bg);
                                PdfCell(table, h.Descripcion, bg);
                                PdfCell(table, h.Monto.ToString("C"), bg);
                                PdfCell(table, h.SaldoResultante.ToString("C"), bg);
                            }
                        });
                    });
                }).GeneratePdf(dialog.FileName);

                AbrirArchivo(dialog.FileName);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error al exportar PDF: {ex.Message}");
            }
        }

        private static void PdfHeader(TableDescriptor table, params string[] headers)
        {
            foreach (var h in headers)
                table.Cell().Background("#1A2E4A").Padding(6).Text(h).FontColor(Colors.White).Bold().FontSize(8);
        }

        private static void EncabezadoInstitucional(ColumnDescriptor col, Negocio? negocio, string titulo, string color)
        {
            col.Item().Row(row =>
            {
                if (!string.IsNullOrWhiteSpace(negocio?.RutaLogo) && File.Exists(negocio.RutaLogo))
                    row.ConstantItem(55).Image(negocio.RutaLogo).FitArea();

                row.RelativeItem().PaddingLeft(10).Column(c =>
                {
                    c.Item().Text(negocio?.NombreNegocio ?? "Mi Negocio")
                        .FontSize(17).Bold().FontColor(color);

                    if (!string.IsNullOrWhiteSpace(negocio?.Slogan))
                        c.Item().Text(negocio.Slogan).FontSize(9).Italic().FontColor("#64748B");

                    c.Item().PaddingTop(4).Text(titulo)
                        .FontSize(12).Bold().FontColor("#263645");
                });
            });

            col.Item().PaddingTop(6).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    if (!string.IsNullOrWhiteSpace(negocio?.RNC))
                        c.Item().Text($"RNC: {negocio.RNC}").FontSize(8).FontColor("#475569");
                    if (!string.IsNullOrWhiteSpace(negocio?.Telefono))
                        c.Item().Text($"Tel: {negocio.Telefono}").FontSize(8).FontColor("#475569");
                    if (!string.IsNullOrWhiteSpace(negocio?.Email))
                        c.Item().Text($"Email: {negocio.Email}").FontSize(8).FontColor("#475569");
                });

                row.RelativeItem().AlignRight().Column(c =>
                {
                    if (!string.IsNullOrWhiteSpace(negocio?.Direccion))
                        c.Item().Text(negocio.Direccion).FontSize(8).FontColor("#475569").AlignRight();
                    if (!string.IsNullOrWhiteSpace(negocio?.SitioWeb))
                        c.Item().Text(negocio.SitioWeb).FontSize(8).FontColor("#475569").AlignRight();
                });
            });

            col.Item().PaddingTop(6).LineHorizontal(1.5f).LineColor(color);
        }



        private static void PdfCell(TableDescriptor table, string text, string bg, string? color = null, bool bold = false)
        {
            var descriptor = table.Cell().Background(bg).BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(5).Text(text).FontSize(8);
            if (color != null) descriptor.FontColor(color);
            if (bold) descriptor.Bold();
        }

        // ═══════════════════════════════════════════════════
        // MÉTODOS DE APOYO
        // ═══════════════════════════════════════════════════
        private static void AbrirArchivo(string ruta)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = ruta,
                UseShellExecute = true
            });
        }

        private static void AbrirCarpetaContenedora(string rutaArchivo)
        {
            string? folder = Path.GetDirectoryName(rutaArchivo);
            if (folder != null && Directory.Exists(folder))
                System.Diagnostics.Process.Start("explorer.exe", folder);
        }


    }
}
