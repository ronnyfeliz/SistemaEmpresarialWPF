using System;
using System.Collections.Generic;
using System.Text;

using Project_v1.Modelos;

namespace Project_v1.Servicios
{
    public static class Sesion
    {
        public static int UsuarioId { get; set; }
        public static string NombreUsuario { get; set; } = string.Empty;
        public static Rol RolActivo { get; set; }
        public static bool EstaActivo { get; set; } = false; // Indica si hay una sesion activa
        public static void Cerrar() // Cierra la sesion y limpia los datos
        {
            UsuarioId = 0;
            NombreUsuario = string.Empty;
            EstaActivo = false;
        }
    }
}
