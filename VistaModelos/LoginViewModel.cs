using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Project_v1.Modelos;
using Project_v1.Servicios;
using Project_v1.Vista;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Project_v1.VistaModelos
{
    public partial class LoginViewModel : ObservableObject
    {
        // Referencia a la ventana actual para poder cerrarla al autenticarse
        private readonly Window _ventana;

        public LoginViewModel(Window ventana)
        {
            _ventana = ventana;
        }

        // Propiedad enlazada al campo de texto de usuario en el XAML
        [ObservableProperty]
        private string nombreUsuario = string.Empty;

        // Comando ejecutado al presionar el botón "Iniciar Sesión"
        [RelayCommand]
        private void Login(PasswordBox passwordBox)
        {
            string contrasena = passwordBox.Password;

            using var db = new AppDbContext();

            // Verifica si hay usuarios registrados en el sistema
            if (!db.Usuarios.Any())
            {
                MessageBox.Show("No hay usuarios registrados en el sistema.",
                    "Sin usuarios", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Busca un usuario activo que coincida con las credenciales ingresadas
            var usuario = db.Usuarios.FirstOrDefault(u =>
                u.Nombre == NombreUsuario &&
                u.Contrasena == contrasena &&
                u.Activo == true);

            // Si no se encontró el usuario, muestra error y detiene el proceso
            if (usuario == null)
            {
                MessageBox.Show("Usuario o contraseña incorrectos.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Guarda los datos del usuario autenticado en la sesión activa
            Sesion.UsuarioId = usuario.Id;
            Sesion.NombreUsuario = usuario.Nombre;
            Sesion.RolActivo = usuario.Rol;
            Sesion.EstaActivo = true;

            // Abre la ventana principal
            MainWindow main = new MainWindow();
            main.Show();

            // Cierra el login
            _ventana.Close();

            // Genera y muestra notificaciones de deudas DESPUÉS de autenticarse
            var notificaciones = GenerarNotificaciones(db);
            if (notificaciones.Count > 0)
            {
                var ventanaNotif = new NotificacionesWindow(notificaciones);
                ventanaNotif.ShowDialog();
            }

            // Muestra notificaciones de stock bajo si hay productos con 10 o menos unidades
            bool hayStockBajo = db.Productos.Any(p => p.Activo == true && p.Stock <= 10);
            if (hayStockBajo)
            {
                new NotificacionStockWindow().ShowDialog();
            }

            var hoyProveedor = DateTime.Today;

            var transaccionesPendientes = db.TransaccionesProveedor
                .Include(t => t.Proveedor)
                .Where(t =>
                    t.SaldoPendiente > 0 &&
                    t.FechaLimitePago.HasValue)
                .OrderBy(t => t.FechaLimitePago)
                .ToList();

            var vencimientosProveedor = transaccionesPendientes
                .Select(t =>
                {
                    int dias = (t.FechaLimitePago!.Value.Date - hoyProveedor).Days;

                    string estado = dias switch
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
                        3 or 2 => "#1A6B3C",   // verde oscuro — sin urgencia
                        1 => "#D68910",   // amarillo — próximo
                        0 => "#C0392B",   // rojo — hoy
                        _ => "#7B241C"    // rojo oscuro — vencido
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

            if (vencimientosProveedor.Count > 0)
            {
                new ProveedorVencimientoWindow(vencimientosProveedor).ShowDialog();
            }
        }

        // Genera la lista de notificaciones según las reglas de vencimiento
        private List<NotificacionResumen> GenerarNotificaciones(AppDbContext db)
        {
            var resultado = new List<NotificacionResumen>();
            var hoy = DateTime.Today;

            // Obtiene todos los clientes con deudas no pagadas
            var deudores = db.ClientesDeudores
                             .Where(c => c.Pagado == false)
                             .ToList();

            foreach (var cliente in deudores)
            {
                // Calcula cuántos días faltan o han pasado desde la fecha de pago
                int diasDiferencia = (hoy - cliente.FechaPago.Date).Days;

                string estado = string.Empty;
                bool debeNotificar = false;

                if (diasDiferencia == -1)
                {
                    // Un día antes del vencimiento
                    estado = "⚠️ Vence mañana";
                    debeNotificar = true;
                }
                else if (diasDiferencia == 0)
                {
                    // El mismo día del vencimiento
                    estado = "🔴 Vence hoy";
                    debeNotificar = true;
                }
                else if (diasDiferencia == 1)
                {
                    // Un día después del vencimiento
                    estado = "❌ Venció ayer";
                    debeNotificar = true;
                }
                else if (diasDiferencia > 1)
                {
                    // Cualquier día después del vencimiento — notifica una vez por día
                    estado = $"❌ Vencida hace {diasDiferencia} días";
                    debeNotificar = true;
                }

                if (debeNotificar)
                {
                    // Registra la notificación en la BD si no fue registrada hoy
                    bool yaRegistradaHoy = db.Notificaciones.Any(n =>
                        n.ClienteId == cliente.Id &&
                        n.FechaEmision.Date == hoy);

                    if (!yaRegistradaHoy)
                    {
                        db.Notificaciones.Add(new Notificacion
                        {
                            ClienteId = cliente.Id,
                            Mensaje = estado,
                            FechaEmision = DateTime.Now,
                            Vista = false
                        });
                    }

                    resultado.Add(new NotificacionResumen
                    {
                        NombreCliente = cliente.Nombre,
                        Cedula = cliente.Cedula,
                        Telefono = cliente.Telefono,
                        Monto = cliente.MontoDeuda,
                        FechaPago = cliente.FechaPago,
                        Estado = estado
                    });
                }
            }

            db.SaveChanges();
            return resultado;
        }

        // Comando ejecutado al presionar el botón de soporte
        [RelayCommand]
        private void ContactarSoporte()
        {
            MessageBox.Show("Para recuperar tus credenciales contacta al administrador del sistema.",
                "Contactar Soporte", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
