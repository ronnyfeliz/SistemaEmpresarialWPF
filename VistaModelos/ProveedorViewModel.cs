using ClosedXML.Excel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using Project_v1.Modelos;
using Project_v1.Servicios;
using Project_v1.Vista;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;

namespace Project_v1.VistaModelos
{
    public partial class ProveedorViewModel : ObservableObject
    {
        private List<Proveedor> _todosLosProveedores = new();
        private bool _modoEdicion;

        public bool EsAdmin => Sesion.RolActivo == Rol.Administrador;
        public bool PuedePagarYExportar => Sesion.EstaActivo;

        [ObservableProperty] private ObservableCollection<Proveedor> proveedores = new();
        [ObservableProperty] private ObservableCollection<TransaccionProveedor> transacciones = new();
        [ObservableProperty] private ObservableCollection<HistorialProveedor> historial = new();

        [ObservableProperty] private Proveedor? proveedorSeleccionado;
        [ObservableProperty] private TransaccionProveedor? transaccionSeleccionada;
        [ObservableProperty] private HistorialProveedor? movimientoSeleccionado;

        [ObservableProperty] private string filtroNombre = string.Empty;
        [ObservableProperty] private string filtroRnc = string.Empty;
        [ObservableProperty] private string filtroCantidadMercancia = string.Empty;
        [ObservableProperty] private string filtroMontoMaximo = string.Empty;
        [ObservableProperty] private string filtroEstado = "Todos";
        [ObservableProperty] private string filtroTipo = "Todos";
        [ObservableProperty] private bool mostrarInactivos;

        [ObservableProperty] private string nombre = string.Empty;
        [ObservableProperty] private string rncOCedula = string.Empty;
        [ObservableProperty] private string telefono = string.Empty;
        [ObservableProperty] private string correo = string.Empty;
        [ObservableProperty] private string direccion = string.Empty;
        [ObservableProperty] private string tipoProveedor = "Productos";
        [ObservableProperty] private bool activo = true;

        [ObservableProperty] private DateTime fechaProvision = DateTime.Today;
        [ObservableProperty] private string cantidadMercancia = string.Empty;
        [ObservableProperty] private string montoTotal = string.Empty;
        [ObservableProperty] private string montoAbonado = "0";
        [ObservableProperty] private DateTime fechaLimitePago = DateTime.Today.AddDays(30);
        [ObservableProperty] private string descripcionTransaccion = string.Empty;

        [ObservableProperty] private string montoPago = string.Empty;
        [ObservableProperty] private string observacionPago = string.Empty;

        [ObservableProperty] private decimal deudaProveedorSeleccionado;
        [ObservableProperty] private int cantidadTotalMercanciaProveedor;
        [ObservableProperty] private string estadoProveedorSeleccionado = "PAGADO";
        [ObservableProperty] private int proveedoresAdeudados;
        [ObservableProperty] private int proveedoresPagados;
        [ObservableProperty] private decimal deudaTotalProveedores;
        [ObservableProperty] private string filtroMontoMinimo = string.Empty;

        partial void OnFiltroMontoMinimoChanged(string value) => AplicarFiltros();

        public ProveedorViewModel()
        {
            CargarProveedores();
        }

        private void CargarProveedores()
        {
            using var db = new AppDbContext();
            _todosLosProveedores = db.Proveedores
                .Include(p => p.Transacciones)
                .OrderBy(p => p.Nombre)
                .ToList();

            AplicarFiltros();
        }

        private void AplicarFiltros()
        {
            var resultado = _todosLosProveedores.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(FiltroNombre))
                resultado = resultado.Where(p => p.Nombre.Contains(FiltroNombre, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(FiltroRnc))
                resultado = resultado.Where(p => p.RncOCedula.Contains(FiltroRnc, StringComparison.OrdinalIgnoreCase));

            if (!MostrarInactivos)
                resultado = resultado.Where(p => p.Activo);

            if (int.TryParse(FiltroCantidadMercancia, out int cantidadMin) && cantidadMin > 0)
                resultado = resultado.Where(p => p.Transacciones.Sum(t => t.CantidadMercancia) >= cantidadMin);

            if (TryParseMonto(FiltroMontoMinimo, out decimal montoMinimo) && montoMinimo > 0)
                resultado = resultado.Where(p => p.DeudaTotal >= montoMinimo);

            if (TryParseMonto(FiltroMontoMaximo, out decimal montoMaximo) && montoMaximo > 0)
                resultado = resultado.Where(p => p.DeudaTotal <= montoMaximo);

            if (FiltroTipo == "Productos")
                resultado = resultado.Where(p => p.TipoProveedor == Project_v1.Modelos.TipoProveedor.Productos);
            else if (FiltroTipo == "Servicios")
                resultado = resultado.Where(p => p.TipoProveedor == Project_v1.Modelos.TipoProveedor.Servicios);
            else if (FiltroTipo == "Productos y Servicios")
                resultado = resultado.Where(p => p.TipoProveedor == Project_v1.Modelos.TipoProveedor.ProductosYServicios);

            if (FiltroEstado == "Pagado")
                resultado = resultado.Where(p => p.DeudaTotal <= 0);
            else if (FiltroEstado == "Con deuda")
                resultado = resultado.Where(p => p.DeudaTotal > 0);

            Proveedores = new ObservableCollection<Proveedor>(resultado.ToList());
            ActualizarContadores();
        }

        partial void OnFiltroNombreChanged(string value) => AplicarFiltros();
        partial void OnFiltroRncChanged(string value) => AplicarFiltros();
        partial void OnFiltroCantidadMercanciaChanged(string value) => AplicarFiltros();
        partial void OnFiltroMontoMaximoChanged(string value) => AplicarFiltros();
        partial void OnFiltroEstadoChanged(string value) => AplicarFiltros();
        partial void OnFiltroTipoChanged(string value) => AplicarFiltros();
        partial void OnMostrarInactivosChanged(bool value) => AplicarFiltros();

        partial void OnProveedorSeleccionadoChanged(Proveedor? value)
        {
            if (value == null)
            {
                LimpiarDetalle();
                return;
            }

            Nombre = value.Nombre;
            RncOCedula = value.RncOCedula;
            Telefono = value.Telefono;
            Correo = value.Correo;
            Direccion = value.Direccion;
            TipoProveedor = value.TipoTexto;
            Activo = value.Activo;
            _modoEdicion = true;

            CargarDetalleProveedor(value.Id);
        }

        // Propiedad para habilitar el botón Ver Detalles
        public bool HaySeleccionDetalle =>
            TransaccionSeleccionada != null || MovimientoSeleccionado != null;

        partial void OnTransaccionSeleccionadaChanged(TransaccionProveedor? value)
        {
            OnPropertyChanged(nameof(HaySeleccionDetalle));
        }

        partial void OnMovimientoSeleccionadoChanged(HistorialProveedor? value)
        {
            OnPropertyChanged(nameof(HaySeleccionDetalle));
        }

        [RelayCommand]
        private void VerDetalles()
        {
            if (TransaccionSeleccionada != null)
            {
                var ventana = new ComprobanteProveedorWindow(
                    ProveedorSeleccionado?.Nombre ?? "—",
                    TransaccionSeleccionada,
                    TransaccionSeleccionada.MontoPagado,
                    null,
                    TransaccionSeleccionada.EstadoPago == EstadoPagoProveedor.Pagado
                        ? "COMPROBANTE DE CANCELACIÓN"
                        : TransaccionSeleccionada.EstadoPago == EstadoPagoProveedor.DeudaParcial
                            ? "COMPROBANTE DE ABONO PARCIAL"
                            : "COMPROBANTE DE CRÉDITO");
                ventana.ShowDialog();
                return;
            }

            if (MovimientoSeleccionado != null)
            {
                // Usa la navegación ya cargada en memoria primero
                var transaccion = MovimientoSeleccionado.TransaccionProveedor;

                // Si no vino cargada, busca en BD
                if (transaccion == null && MovimientoSeleccionado.TransaccionProveedorId.HasValue)
                {
                    using var db = new AppDbContext();
                    transaccion = db.TransaccionesProveedor
                        .Find(MovimientoSeleccionado.TransaccionProveedorId.Value);
                }

                if (transaccion == null)
                {
                    MessageBox.Show(
                        "Este movimiento es de tipo administrativo (alta, baja o modificación de proveedor) y no tiene una transacción de compra asociada.",
                        "Sin transacción", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var ventana = new ComprobanteProveedorWindow(
                    ProveedorSeleccionado?.Nombre ?? "—",
                    transaccion,
                    MovimientoSeleccionado.Monto,
                    null,
                    $"DETALLE DE MOVIMIENTO #{MovimientoSeleccionado.Id}");
                ventana.ShowDialog();
            }
        }

        [RelayCommand]
        private void ExportarHistorial()
        {
            // El dropdown lo maneja el XAML, este comando no se usa directamente
        }

        [RelayCommand]
        private void ExportarMovimiento()
        {
            // El dropdown lo maneja el XAML, este comando no se usa directamente
        }

        private void CargarDetalleProveedor(int proveedorId)
        {
            using var db = new AppDbContext();

            var transaccionesDb = db.TransaccionesProveedor
                .Where(t => t.ProveedorId == proveedorId)
                .OrderByDescending(t => t.FechaProvision)
                .ToList();

            Transacciones = new ObservableCollection<TransaccionProveedor>(transaccionesDb);

            var historialDb = db.HistorialProveedores
                                .Include(h => h.Usuario)
                                .Include(h => h.TransaccionProveedor)   // ← esto faltaba
                                .Where(h => h.ProveedorId == proveedorId)
                                .OrderByDescending(h => h.Fecha)
                                .ToList();

            Historial = new ObservableCollection<HistorialProveedor>(historialDb);

            DeudaProveedorSeleccionado = transaccionesDb.Sum(t => t.SaldoPendiente);
            CantidadTotalMercanciaProveedor = transaccionesDb.Sum(t => t.CantidadMercancia);
            EstadoProveedorSeleccionado = DeudaProveedorSeleccionado > 0 ? "CON DEUDA" : "PAGADO";
        }

        [RelayCommand]
        private void VerNotificacionesProveedor()
        {
            var hoy = DateTime.Today;

            using var db = new AppDbContext();

            var transaccionesPendientes = db.TransaccionesProveedor
                .Include(t => t.Proveedor)
                .Where(t => t.SaldoPendiente > 0 && t.FechaLimitePago.HasValue)
                .OrderBy(t => t.FechaLimitePago)
                .ToList();

            var vencimientos = transaccionesPendientes
                .Select(t =>
                {
                    int dias = (t.FechaLimitePago!.Value.Date - hoy).Days;

                    string? estado = dias switch
                    {
                        3 => "⏰ Vence en 3 días",
                        2 => "⏰ Vence en 2 días",
                        1 => "⚠️ Vence mañana",
                        0 => "🔴 Vence hoy",
                        _ => dias < 0 ? $"❌ Vencida hace {Math.Abs(dias)} día(s)" : null
                    };

                    if (estado == null) return null;

                    string color = dias switch
                    {
                        3 or 2 => "#1A6B3C",
                        1 => "#D68910",
                        0 => "#C0392B",
                        _ => "#7B241C"
                    };

                    return new VencimientoProveedorItem
                    {
                        IdTransaccion = t.Id,
                        NombreProveedor = t.Proveedor?.Nombre ?? "—",
                        FechaLimite = t.FechaLimitePago!.Value,
                        MontoTotal = t.MontoTotal,
                        SaldoPendiente = t.SaldoPendiente,
                        EstadoAlerta = estado,
                        ColorAlerta = color
                    };
                })
                .Where(x => x != null)
                .Cast<VencimientoProveedorItem>()
                .ToList();

            if (vencimientos.Count == 0)
            {
                MessageBox.Show("No hay pagos próximos a vencer o vencidos.",
                    "Sin notificaciones", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            new ProveedorVencimientoWindow(vencimientos).ShowDialog();
        }

        [RelayCommand]
        private void Guardar()
        {
            if (!ValidarAdmin("Solo el administrador puede crear o modificar proveedores."))
                return;

            if (string.IsNullOrWhiteSpace(Nombre))
            {
                MessageBox.Show("El nombre del proveedor es obligatorio.",
                    "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using var db = new AppDbContext();

            bool duplicado = db.Proveedores.Any(p =>
                p.Id != (ProveedorSeleccionado != null ? ProveedorSeleccionado.Id : 0) &&
                p.Activo &&
                p.Nombre == Nombre);

            if (duplicado)
            {
                MessageBox.Show("Ya existe un proveedor activo con ese nombre.",
                    "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_modoEdicion && ProveedorSeleccionado != null)
            {
                var proveedor = db.Proveedores.Find(ProveedorSeleccionado.Id);
                if (proveedor == null)
                    return;

                proveedor.Nombre = Nombre.Trim();
                proveedor.RncOCedula = RncOCedula.Trim();
                proveedor.Telefono = Telefono.Trim();
                proveedor.Correo = Correo.Trim();
                proveedor.Direccion = Direccion.Trim();
                proveedor.TipoProveedor = ObtenerTipoProveedor();
                proveedor.Activo = Activo;

                RegistrarHistorial(db, proveedor.Id, null, null, TipoMovimientoProveedor.Modificacion,
                    "Datos del proveedor actualizados.", 0, ObtenerDeudaProveedor(db, proveedor.Id));

                db.SaveChanges();
                MessageBox.Show("Proveedor actualizado correctamente.",
                    "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                var proveedor = new Proveedor
                {
                    Nombre = Nombre.Trim(),
                    RncOCedula = RncOCedula.Trim(),
                    Telefono = Telefono.Trim(),
                    Correo = Correo.Trim(),
                    Direccion = Direccion.Trim(),
                    TipoProveedor = ObtenerTipoProveedor(),
                    Activo = true
                };

                db.Proveedores.Add(proveedor);
                db.SaveChanges();

                RegistrarHistorial(db, proveedor.Id, null, null, TipoMovimientoProveedor.Modificacion,
                    "Proveedor registrado.", 0, 0);
                db.SaveChanges();

                MessageBox.Show("Proveedor registrado correctamente.",
                    "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            LimpiarFormulario();
            CargarProveedores();
        }

        [RelayCommand]
        private void Eliminar()
        {
            if (!ValidarAdmin("Solo el administrador puede eliminar proveedores."))
                return;

            if (ProveedorSeleccionado == null)
            {
                MessageBox.Show("Selecciona un proveedor primero.",
                    "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // En Eliminar(), cambia esta línea:
            var confirmar = MessageBox.Show(
                $"¿Desactivar el proveedor '{ProveedorSeleccionado.Nombre}'?",
                "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirmar != MessageBoxResult.Yes)
                return;

            using var db = new AppDbContext();
            var proveedor = db.Proveedores.Find(ProveedorSeleccionado.Id);
            if (proveedor == null)
                return;

            proveedor.Activo = false;
            RegistrarHistorial(db, proveedor.Id, null, null, TipoMovimientoProveedor.Eliminacion,
                "Proveedor desactivado.", 0, ObtenerDeudaProveedor(db, proveedor.Id));

            db.SaveChanges();
            LimpiarFormulario();
            CargarProveedores();
        }

        [RelayCommand]
        private void Activar()
        {
            if (!ValidarAdmin("Solo el administrador puede activar proveedores."))
                return;

            if (ProveedorSeleccionado == null)
            {
                MessageBox.Show("Selecciona un proveedor primero.",
                    "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using var db = new AppDbContext();
            var proveedor = db.Proveedores.Find(ProveedorSeleccionado.Id);
            if (proveedor == null)
                return;

            proveedor.Activo = true;
            RegistrarHistorial(db, proveedor.Id, null, null, TipoMovimientoProveedor.Modificacion,
                "Proveedor reactivado.", 0, ObtenerDeudaProveedor(db, proveedor.Id));

            db.SaveChanges();
            CargarProveedores();
            RestaurarSeleccion(proveedor.Id);
        }

        [RelayCommand]
        private void AgregarTransaccion()
        {
            if (!ValidarAdmin("Solo el administrador puede registrar suministros."))
                return;

            if (ProveedorSeleccionado == null)
            {
                MessageBox.Show("Selecciona un proveedor primero.",
                    "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(CantidadMercancia, out int cantidad) || cantidad <= 0)
            {
                MessageBox.Show("La cantidad de mercancía debe ser un número entero mayor que 0.",
                    "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!TryParseMonto(MontoTotal, out decimal total) || total <= 0)
            {
                MessageBox.Show("El monto total debe ser un valor numérico válido.",
                    "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!TryParseMonto(MontoAbonado, out decimal abonado) || abonado < 0 || abonado > total)
            {
                MessageBox.Show("El monto abonado debe ser un valor numérico entre 0 y el total.",
                    "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using var db = new AppDbContext();
            int proveedorId = ProveedorSeleccionado.Id;
            decimal saldo = total - abonado;
            EstadoPagoProveedor estado = ObtenerEstado(total, abonado);

            var transaccion = new TransaccionProveedor
            {
                ProveedorId = proveedorId,
                FechaProvision = FechaProvision,
                CantidadMercancia = cantidad,
                MontoTotal = total,
                MontoPagado = abonado,
                SaldoPendiente = saldo,
                EstadoPago = estado,
                FechaLimitePago = saldo > 0 ? FechaLimitePago : null,
                Descripcion = DescripcionTransaccion.Trim()
            };

            db.TransaccionesProveedor.Add(transaccion);
            db.SaveChanges();

            string tituloTransaccion = transaccion.EstadoPago switch
            {
                EstadoPagoProveedor.Pagado => "COMPROBANTE DE CANCELACIÓN",
                EstadoPagoProveedor.DeudaParcial => "COMPROBANTE DE ABONO PARCIAL",
                _ => "COMPROBANTE DE CRÉDITO"
            };

            MostrarComprobanteProveedor(
                ProveedorSeleccionado.Nombre,
                transaccion,
                abonado,
                null,
                tituloTransaccion);

            LimpiarTransaccion();
            CargarProveedores();
            RestaurarSeleccion(proveedorId);
        }

        [RelayCommand]
        private void Pagar()
        {
            if (ProveedorSeleccionado == null || TransaccionSeleccionada == null)
            {
                MessageBox.Show("Selecciona un proveedor y una transacción pendiente.",
                    "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (TransaccionSeleccionada.SaldoPendiente <= 0)
            {
                MessageBox.Show("La transacción seleccionada ya está pagada.",
                    "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!TryParseMonto(MontoPago, out decimal pagoMonto) || pagoMonto <= 0)
            {
                MessageBox.Show("El pago debe ser un valor numérico válido mayor que 0.",
                    "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (pagoMonto > TransaccionSeleccionada.SaldoPendiente)
            {
                MessageBox.Show("El pago no puede ser mayor que el saldo pendiente.",
                    "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using var db = new AppDbContext();
            int proveedorId = ProveedorSeleccionado.Id;
            var transaccion = db.TransaccionesProveedor.Find(TransaccionSeleccionada.Id);
            if (transaccion == null)
                return;

            decimal saldoAnterior = transaccion.SaldoPendiente;
            decimal saldoNuevo = saldoAnterior - pagoMonto;

            transaccion.MontoPagado += pagoMonto;
            transaccion.SaldoPendiente = saldoNuevo;
            transaccion.EstadoPago = saldoNuevo == 0 ? EstadoPagoProveedor.Pagado : EstadoPagoProveedor.DeudaParcial;

            var pago = CrearPago(transaccion.ProveedorId, transaccion.Id, pagoMonto, saldoAnterior, saldoNuevo, ObservacionPago.Trim());
            db.PagosProveedor.Add(pago);
            db.SaveChanges();

            RegistrarHistorial(db, transaccion.ProveedorId, transaccion.Id, pago.Id,
                saldoNuevo == 0 ? TipoMovimientoProveedor.PagoTotal : TipoMovimientoProveedor.PagoParcial,
                saldoNuevo == 0 ? "Cancelación completa de deuda." : "Abono parcial a deuda.",
                pagoMonto, saldoNuevo);

            db.SaveChanges();

            MostrarComprobanteProveedor(
                ProveedorSeleccionado.Nombre,
                transaccion,
                pagoMonto,
                saldoAnterior,
                saldoNuevo == 0 ? "COMPROBANTE DE CANCELACIÓN" : "COMPROBANTE DE ABONO PARCIAL");

            MontoPago = string.Empty;
            ObservacionPago = string.Empty;
            CargarProveedores();
            RestaurarSeleccion(proveedorId);
        }

        [RelayCommand]
        private void Nuevo() => LimpiarFormulario();

        [RelayCommand]
        private void LimpiarFiltros()
        {
            FiltroNombre = string.Empty;
            FiltroRnc = string.Empty;
            FiltroCantidadMercancia = string.Empty;
            FiltroMontoMaximo = string.Empty;
            FiltroEstado = "Todos";
            FiltroTipo = "Todos";
            MostrarInactivos = false;
            FiltroMontoMinimo = string.Empty;
        }

        [RelayCommand]
        private void ExportarListaExcel()
        {
            ExcelExportService.ExportarProveedores(Proveedores.ToList());
        }

        [RelayCommand]
        private void ExportarListaPdf()
        {
            FacturaPdfService.ExportarProveedores(Proveedores.ToList());
        }

        [RelayCommand]
        private void ExportarHistorialExcel()
        {
            if (ProveedorSeleccionado == null)
            {
                MessageBox.Show("Selecciona un proveedor primero.",
                    "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using var db = new AppDbContext();
            var transacciones = db.TransaccionesProveedor
                .Include(t => t.Proveedor)
                .Where(t => t.ProveedorId == ProveedorSeleccionado.Id)
                .OrderByDescending(t => t.FechaProvision)
                .ToList();

            ExcelExportService.ExportarTransaccionesProveedor(
                transacciones,
                $"TRANSACCIONES — {ProveedorSeleccionado.Nombre}");
        }

        [RelayCommand]
        private void ExportarHistorialPdf()
        {
            if (ProveedorSeleccionado == null)
            {
                MessageBox.Show("Selecciona un proveedor primero.",
                    "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using var db = new AppDbContext();
            var transacciones = db.TransaccionesProveedor
                .Include(t => t.Proveedor)
                .Where(t => t.ProveedorId == ProveedorSeleccionado.Id)
                .OrderByDescending(t => t.FechaProvision)
                .ToList();

            FacturaPdfService.ExportarTransaccionesProveedor(
                transacciones,
                $"TRANSACCIONES — {ProveedorSeleccionado.Nombre}");
        }

        // ── Exportar TODOS los movimientos del proveedor seleccionado ──
        [RelayCommand]
        private void ExportarMovimientoExcel()
        {
            if (ProveedorSeleccionado == null)
            {
                MessageBox.Show("Selecciona un proveedor primero.",
                    "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ExcelExportService.ExportarHistorialProveedor(
                Historial.ToList(),
                ProveedorSeleccionado.Nombre);
        }

        [RelayCommand]
        private void ExportarMovimientoPdf()
        {
            if (ProveedorSeleccionado == null)
            {
                MessageBox.Show("Selecciona un proveedor primero.",
                    "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using var db = new AppDbContext();
            var movimientos = db.HistorialProveedores
                .Include(h => h.TransaccionProveedor)
                .Where(h => h.ProveedorId == ProveedorSeleccionado.Id)
                .OrderByDescending(h => h.Fecha)
                .ToList();

            FacturaPdfService.ExportarHistorialProveedor(
                movimientos,
                ProveedorSeleccionado.Nombre);
        }

        [RelayCommand]
        private void VerHistorialTransacciones()
        {
            var ventana = new HistorialTransaccionesProveedorWindow();
            ventana.ShowDialog();
        }

        private bool ValidarAdmin(string mensaje)
        {
            if (EsAdmin)
                return true;

            MessageBox.Show(mensaje, "Acceso denegado", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        private static bool TryParseMonto(string valor, out decimal monto)
        {
            monto = 0;

            if (!double.TryParse(valor, NumberStyles.Number, CultureInfo.CurrentCulture, out double montoDouble) &&
                !double.TryParse(valor, NumberStyles.Number, CultureInfo.InvariantCulture, out montoDouble))
                return false;

            monto = Convert.ToDecimal(montoDouble);
            return true;
        }

        private static EstadoPagoProveedor ObtenerEstado(decimal total, decimal abonado)
        {
            if (abonado == total)
                return EstadoPagoProveedor.Pagado;

            return abonado == 0 ? EstadoPagoProveedor.Debe : EstadoPagoProveedor.DeudaParcial;
        }

        private TipoProveedor ObtenerTipoProveedor()
        {
            return TipoProveedor switch
            {
                "Servicios" => Project_v1.Modelos.TipoProveedor.Servicios,
                "Productos y Servicios" => Project_v1.Modelos.TipoProveedor.ProductosYServicios,
                _ => Project_v1.Modelos.TipoProveedor.Productos
            };
        }

        private PagoProveedor CrearPago(int proveedorId, int transaccionId, decimal monto, decimal saldoAnterior, decimal saldoNuevo, string observacion)
        {
            return new PagoProveedor
            {
                ProveedorId = proveedorId,
                TransaccionProveedorId = transaccionId,
                MontoPagado = monto,
                SaldoAnterior = saldoAnterior,
                SaldoNuevo = saldoNuevo,
                UsuarioId = Sesion.UsuarioId == 0 ? null : Sesion.UsuarioId,
                Observacion = observacion
            };
        }

        private static decimal ObtenerDeudaProveedor(AppDbContext db, int proveedorId)
        {
            return db.TransaccionesProveedor
                .Where(t => t.ProveedorId == proveedorId)
                .Sum(t => (decimal?)t.SaldoPendiente) ?? 0;
        }

        private void RegistrarHistorial(AppDbContext db, int proveedorId, int? transaccionId, int? pagoId,
            TipoMovimientoProveedor tipo, string descripcion, decimal monto, decimal saldo)
        {
            db.HistorialProveedores.Add(new HistorialProveedor
            {
                ProveedorId = proveedorId,
                TransaccionProveedorId = transaccionId,
                PagoProveedorId = pagoId,
                TipoMovimiento = tipo,
                Descripcion = descripcion,
                Monto = monto,
                SaldoResultante = saldo,
                UsuarioId = Sesion.UsuarioId == 0 ? null : Sesion.UsuarioId
            });
        }

        private static string CrearDescripcionSuministro(EstadoPagoProveedor estado, decimal total, decimal abonado, decimal saldo)
        {
            return estado switch
            {
                EstadoPagoProveedor.Pagado => $"Suministro pagado completo. Total: {total:C}.",
                EstadoPagoProveedor.DeudaParcial => $"Suministro con abono parcial. Abonado: {abonado:C}. Saldo: {saldo:C}.",
                _ => $"Suministro registrado a crédito. Saldo pendiente: {saldo:C}."
            };
        }

        private void MostrarComprobanteProveedor(
              string nombreProveedor,
              TransaccionProveedor transaccion,
              decimal montoMovimiento,
              decimal? saldoAnterior,
              string titulo)
        {
            var ventana = new ComprobanteProveedorWindow(
                nombreProveedor, transaccion, montoMovimiento, saldoAnterior, titulo);
            ventana.ShowDialog();
        }

        private void ActualizarContadores()
        {
            var activos = _todosLosProveedores.Where(p => p.Activo).ToList();
            ProveedoresAdeudados = activos.Count(p => p.DeudaTotal > 0);
            ProveedoresPagados = activos.Count(p => p.DeudaTotal <= 0);
            DeudaTotalProveedores = activos.Sum(p => p.DeudaTotal);
        }

        private void RestaurarSeleccion(int proveedorId)
        {
            ProveedorSeleccionado = Proveedores.FirstOrDefault(p => p.Id == proveedorId);
            if (ProveedorSeleccionado == null)
                CargarDetalleProveedor(proveedorId);
        }

        private void LimpiarFormulario()
        {
            Nombre = string.Empty;
            RncOCedula = string.Empty;
            Telefono = string.Empty;
            Correo = string.Empty;
            Direccion = string.Empty;
            TipoProveedor = "Productos";
            Activo = true;
            ProveedorSeleccionado = null;
            _modoEdicion = false;
            LimpiarDetalle();
        }

        private void LimpiarDetalle()
        {
            Transacciones.Clear();
            Historial.Clear();
            DeudaProveedorSeleccionado = 0;
            CantidadTotalMercanciaProveedor = 0;
            EstadoProveedorSeleccionado = "PAGADO";
        }

        private void LimpiarTransaccion()
        {
            FechaProvision = DateTime.Today;
            CantidadMercancia = string.Empty;
            MontoTotal = string.Empty;
            MontoAbonado = "0";
            FechaLimitePago = DateTime.Today.AddDays(30);
            DescripcionTransaccion = string.Empty;
            TransaccionSeleccionada = null;
        }
    }
}
