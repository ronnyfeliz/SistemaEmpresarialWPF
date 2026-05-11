using Project_v1.Modelos;
using Project_v1.Servicios;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Microsoft.EntityFrameworkCore;

namespace Project_v1.Vista
{
    // Ventana que muestra el resumen y el historial de notificaciones de un cliente deudor
    public partial class HistorialClienteWindow : Window
    {
        public HistorialClienteWindow(ClienteDeudor cliente)
        {
            InitializeComponent();
            CargarHistorial(cliente);
        }

        private void CargarHistorial(ClienteDeudor cliente)
        {
            // Datos del cliente
            TxtNombreCliente.Text = $"📋 Historial de {cliente.Nombre}";
            TxtCedula.Text = $"Cedula: {cliente.Cedula}";
            TxtTelefono.Text = $"Telefono: {cliente.Telefono}";
            TxtFechaInicio.Text = $"Registrado: {cliente.FechaInicio:dd/MM/yyyy}";

            // Resumen de la deuda
            TxtMonto.Text = cliente.MontoDeuda.ToString("C");
            TxtFechaPago.Text = cliente.FechaPago.ToString("dd/MM/yyyy");

            // Calcula el estado actual
            int dias = (DateTime.Today - cliente.FechaPago.Date).Days;
            if (cliente.Pagado)
                TxtEstado.Text = "✅ Pagado";
            else if (dias < 0)
                TxtEstado.Text = $"⏳ Faltan {Math.Abs(dias)} dias";
            else if (dias == 0)
                TxtEstado.Text = "🔴 Vence hoy";
            else
                TxtEstado.Text = $"❌ Vencida hace {dias} dias";

            // Muestra el panel de mora solo si tiene clausula activa
            if (cliente.TieneMora)
            {
                PanelMora.Visibility = Visibility.Visible;
                TxtMora.Text = cliente.MontoMora.ToString("C");
            }
            else
            {
                PanelMora.Visibility = Visibility.Collapsed;
            }

            // Carga el historial de notificaciones desde la BD
            using var db = new AppDbContext();
            var notificaciones = db.Notificaciones
                                   .Where(n => n.ClienteId == cliente.Id)
                                   .OrderByDescending(n => n.FechaEmision)
                                   .ToList();

            if (notificaciones.Count == 0)
            {
                // Sin notificaciones registradas — muestra estado actual como fila unica
                var filaEstado = new List<object>
                {
                    new { FechaEmision = DateTime.Now,
                          Mensaje = $"Estado actual: {TxtEstado.Text} (sin notificaciones registradas aun)" }
                };
                GridHistorial.ItemsSource = filaEstado;
            }
            else
            {
                GridHistorial.ItemsSource = notificaciones;
            }
        }

        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}