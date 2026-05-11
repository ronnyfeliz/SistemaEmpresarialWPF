using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Project_v1.Modelos;
using Project_v1.Servicios;
using Project_v1.Vista;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace Project_v1.VistaModelos
{
    public partial class ClientesDeudoresViewModel : ObservableObject
    {
        private List<ClienteDeudor> _todosLosClientes = new();

        [ObservableProperty]
        private ObservableCollection<ClienteDeudor> clientes = new();

        [ObservableProperty]
        private ClienteDeudor? clienteSeleccionado;

        // ===== CAMPOS DE FILTRO =====
        [ObservableProperty] private string filtroNombre = string.Empty;
        [ObservableProperty] private string filtroCedula = string.Empty;
        [ObservableProperty] private string filtroMontoMin = string.Empty;
        [ObservableProperty] private string filtroMontoMax = string.Empty;
        [ObservableProperty] private DateTime? filtroFechaInicioDesde = null;
        [ObservableProperty] private DateTime? filtroFechaInicioHasta = null;
        [ObservableProperty] private DateTime? filtroFechaVencimientoDesde = null;
        [ObservableProperty] private DateTime? filtroFechaVencimientoHasta = null;

        // ===== CAMPOS DEL FORMULARIO =====
        [ObservableProperty] private string nombre = string.Empty;
        [ObservableProperty] private string cedula = string.Empty;
        [ObservableProperty] private string telefono = string.Empty;
        [ObservableProperty] private string montoDeuda = string.Empty;
        [ObservableProperty] private DateTime fechaInicio = DateTime.Today;
        [ObservableProperty] private DateTime fechaPago = DateTime.Today.AddDays(1);
        [ObservableProperty] private string concepto = string.Empty;
        //TOTAL A COBRAR
        [ObservableProperty] private decimal totalDeudaGeneral = 0;

        public bool EsAdministrador => Sesion.RolActivo == Rol.Administrador;

        private bool _modoEdicion = false;

        public ClientesDeudoresViewModel()
        {
            CargarClientes();
        }

        private void CargarClientes()
        {
            using var db = new AppDbContext();
            _todosLosClientes = db.ClientesDeudores
                                  .Where(c => c.Pagado == false)
                                  .ToList();
            AplicarFiltros();
        }

        private void AplicarFiltros()
        {
            var resultado = _todosLosClientes.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(FiltroNombre))
                resultado = resultado.Where(c =>
                    c.Nombre.Contains(FiltroNombre, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(FiltroCedula))
                resultado = resultado.Where(c =>
                    c.Cedula.Contains(FiltroCedula, StringComparison.OrdinalIgnoreCase));

            if (decimal.TryParse(FiltroMontoMin, out decimal montoMin))
                resultado = resultado.Where(c => c.MontoDeuda >= montoMin);

            if (decimal.TryParse(FiltroMontoMax, out decimal montoMax))
                resultado = resultado.Where(c => c.MontoDeuda <= montoMax);

            if (FiltroFechaInicioDesde.HasValue)
                resultado = resultado.Where(c => c.FechaInicio.Date >= FiltroFechaInicioDesde.Value.Date);

            if (FiltroFechaInicioHasta.HasValue)
                resultado = resultado.Where(c => c.FechaInicio.Date <= FiltroFechaInicioHasta.Value.Date);

            if (FiltroFechaVencimientoDesde.HasValue)
                resultado = resultado.Where(c => c.FechaPago.Date >= FiltroFechaVencimientoDesde.Value.Date);

            if (FiltroFechaVencimientoHasta.HasValue)
                resultado = resultado.Where(c => c.FechaPago.Date <= FiltroFechaVencimientoHasta.Value.Date);

            Clientes = new ObservableCollection<ClienteDeudor>(resultado.ToList());
            CalcularTotalDeuda();
        }

        private void CalcularTotalDeuda()
        {
            TotalDeudaGeneral = Clientes.Sum(c =>
            {
                bool moraAplicada = c.TieneMora && DateTime.Today > c.FechaPago;
                return c.MontoDeuda + (moraAplicada ? c.MontoMora : 0);
            });
        }
        partial void OnClientesChanged(ObservableCollection<ClienteDeudor> value)
        {
            CalcularTotalDeuda();
        }

        partial void OnFiltroNombreChanged(string value) => AplicarFiltros();
        partial void OnFiltroCedulaChanged(string value) => AplicarFiltros();
        partial void OnFiltroMontoMinChanged(string value) => AplicarFiltros();
        partial void OnFiltroMontoMaxChanged(string value) => AplicarFiltros();
        partial void OnFiltroFechaInicioDesdeChanged(DateTime? value) => AplicarFiltros();
        partial void OnFiltroFechaInicioHastaChanged(DateTime? value) => AplicarFiltros();
        partial void OnFiltroFechaVencimientoDesdeChanged(DateTime? value) => AplicarFiltros();
        partial void OnFiltroFechaVencimientoHastaChanged(DateTime? value) => AplicarFiltros();

        [RelayCommand]
        private void LimpiarFiltros()
        {
            FiltroNombre = string.Empty;
            FiltroCedula = string.Empty;
            FiltroMontoMin = string.Empty;
            FiltroMontoMax = string.Empty;
            FiltroFechaInicioDesde = null;
            FiltroFechaInicioHasta = null;
            FiltroFechaVencimientoDesde = null;
            FiltroFechaVencimientoHasta = null;
        }

        // Exportar TODOS los deudores (barra superior)
        [RelayCommand]
        private void ExportarDeudoresPdf()
        {
            if (Clientes.Count == 0)
            {
                MessageBox.Show("No hay clientes deudores activos para exportar.",
                    "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            FacturaPdfService.ExportarDeudoresActivos(Clientes.ToList());
        }

        // Exportar ficha de UN cliente (panel derecho)
        [RelayCommand]
        private void ExportarFichaIndividual()
        {
            if (ClienteSeleccionado == null)
            {
                MessageBox.Show("Selecciona un cliente para exportar su ficha.",
                    "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            FacturaPdfService.ExportarFichaDeudorIndividual(ClienteSeleccionado);
        }

        partial void OnClienteSeleccionadoChanged(ClienteDeudor? value)
        {
            if (value != null)
            {
                Nombre = value.Nombre;
                Cedula = value.Cedula;
                Telefono = value.Telefono;
                Concepto = value.Concepto;
                MontoDeuda = value.MontoDeuda.ToString();
                FechaInicio = value.FechaInicio;
                FechaPago = value.FechaPago;
                _modoEdicion = true;
            }
        }

        [RelayCommand]
        private void Guardar()
        {
            if (string.IsNullOrWhiteSpace(Nombre))
            {
                MessageBox.Show("El nombre del cliente es obligatorio.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(Cedula))
            {
                MessageBox.Show("La cedula del cliente es obligatoria.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(Telefono))
            {
                MessageBox.Show("El telefono del cliente es obligatorio.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(MontoDeuda, out decimal monto) || monto <= 0)
            {
                MessageBox.Show("El monto debe ser un numero valido mayor a 0.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if ((FechaPago - FechaInicio).TotalDays < 1)
            {
                MessageBox.Show("La fecha de pago debe ser al menos 1 dia despues de la fecha de inicio.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            bool tieneMora = false;
            decimal montoMora = 0;

            var dialogMora = new Project_v1.Vista.MoraDialogWindow();
            dialogMora.ShowDialog();

            tieneMora = dialogMora.AplicaMora;
            montoMora = dialogMora.MontoMora;

            using var db = new AppDbContext();

            if (_modoEdicion && ClienteSeleccionado != null)
            {
                var cliente = db.ClientesDeudores.Find(ClienteSeleccionado.Id);
                if (cliente != null)
                {
                    cliente.Nombre = Nombre;
                    cliente.Cedula = Cedula;
                    cliente.Telefono = Telefono;
                    cliente.Concepto = Concepto;
                    cliente.MontoDeuda = monto;
                    cliente.FechaInicio = FechaInicio;
                    cliente.FechaPago = FechaPago;
                    cliente.TieneMora = tieneMora;
                    cliente.MontoMora = montoMora;
                    db.SaveChanges();
                    MessageBox.Show("Cliente actualizado correctamente.",
                        "Exito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                if (FechaInicio.Date != DateTime.Today)
                {
                    MessageBox.Show("La fecha de inicio debe ser el dia de hoy.",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var nuevo = new ClienteDeudor
                {
                    Nombre = Nombre,
                    Cedula = Cedula,
                    Telefono = Telefono,
                    Concepto = Concepto,
                    MontoDeuda = monto,
                    FechaInicio = FechaInicio,
                    FechaPago = FechaPago,
                    Pagado = false,
                    TieneMora = tieneMora,
                    MontoMora = montoMora
                };
                db.ClientesDeudores.Add(nuevo);
                db.SaveChanges();

                string msg = tieneMora
                    ? $"Cliente deudor registrado correctamente.\nMora aplicada: RD$ {montoMora:N2}"
                    : "Cliente deudor registrado correctamente.\nSin clausula de mora.";

                MessageBox.Show(msg, "Exito", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            LimpiarFormulario();
            CargarClientes();
        }

        [RelayCommand]
        private void MarcarPagado()
        {
            if (ClienteSeleccionado == null)
            {
                MessageBox.Show("Selecciona un cliente primero.",
                    "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var confirmar = MessageBox.Show(
                $"¿Marcar la deuda de '{ClienteSeleccionado.Nombre}' como pagada?",
                "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirmar == MessageBoxResult.Yes)
            {
                var clientePagado = ClienteSeleccionado;

                using var db = new AppDbContext();
                var cliente = db.ClientesDeudores.Find(clientePagado.Id);
                if (cliente != null)
                {
                    cliente.Pagado = true;
                    cliente.FechaPagado = DateTime.Now;
                    db.SaveChanges();
                }

                var ventana = new Project_v1.Vista.PagoConfirmadoWindow(clientePagado);
                ventana.ShowDialog();

                LimpiarFormulario();
                CargarClientes();
            }
        }

        [RelayCommand]
        private void VerHistorialPagados()
        {
            var ventana = new Project_v1.Vista.HistorialPagadosWindow();
            ventana.ShowDialog();
        }

        [RelayCommand]
        private void Eliminar()
        {
            if (Sesion.RolActivo != Rol.Administrador)
            {
                MessageBox.Show("Solo el administrador puede eliminar registros de clientes.",
                    "Acceso denegado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (ClienteSeleccionado == null)
            {
                MessageBox.Show("Selecciona un cliente primero.",
                    "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var confirmar = MessageBox.Show(
                $"¿Eliminar el registro de '{ClienteSeleccionado.Nombre}'?",
                "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirmar == MessageBoxResult.Yes)
            {
                using var db = new AppDbContext();
                var cliente = db.ClientesDeudores.Find(ClienteSeleccionado.Id);
                if (cliente != null)
                {
                    db.ClientesDeudores.Remove(cliente);
                    db.SaveChanges();
                    MessageBox.Show("Cliente eliminado correctamente.",
                        "Exito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                LimpiarFormulario();
                CargarClientes();
            }
        }

        [RelayCommand]
        private void VerHistorial()
        {
            if (ClienteSeleccionado == null)
            {
                MessageBox.Show("Selecciona un cliente para ver su historial.",
                    "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var ventana = new Project_v1.Vista.HistorialClienteWindow(ClienteSeleccionado);
            ventana.ShowDialog();
        }

        [RelayCommand]
        private void ExportarDeudoresExcel()
        {
            if (Clientes.Count == 0)
            {
                MessageBox.Show("No hay clientes deudores activos para exportar.",
                    "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            ExcelExportService.ExportarDeudoresActivos(Clientes.ToList());
        }

        [RelayCommand]
        private void AbrirNotificaciones()
        {
            var listaNotificaciones = _todosLosClientes
                .Where(c => c.TieneMora && c.FechaPago.Date <= DateTime.Today.AddDays(1))
                .Select(c => new NotificacionResumen
                {
                    NombreCliente = c.Nombre,
                    Cedula = c.Cedula,
                    Telefono = c.Telefono,
                    Monto = c.MontoDeuda + c.MontoMora,
                    FechaPago = c.FechaPago,
                    Estado = c.FechaPago.Date < DateTime.Today
                        ? $"Vencido hace {(DateTime.Today - c.FechaPago.Date).Days} día(s)"
                        : c.FechaPago.Date == DateTime.Today
                            ? "Vence hoy"
                            : "Vence mañana"
                })
                .ToList();

            var ventana = new Project_v1.Vista.NotificacionesWindow(listaNotificaciones);
            ventana.ShowDialog();
        }

        [RelayCommand]
        private void Nuevo()
        {
            LimpiarFormulario();
        }

        private void LimpiarFormulario()
        {
            Nombre = string.Empty;
            Cedula = string.Empty;
            Telefono = string.Empty;
            Concepto = string.Empty;
            MontoDeuda = string.Empty;
            FechaInicio = DateTime.Today;
            FechaPago = DateTime.Today.AddDays(1);
            ClienteSeleccionado = null;
            _modoEdicion = false;
        }
    }
}