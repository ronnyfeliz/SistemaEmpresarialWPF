using Microsoft.EntityFrameworkCore;
using Project_v1.Servicios;
using Project_v1.Vista;
using System;
using System.Linq;
using System.Windows;

namespace Project_v1
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            try
            {
                using var db = new AppDbContext();
                db.Database.Migrate();

                bool hayUsuarios = db.Usuarios.Any();

                if (hayUsuarios)
                {
                    // Backup solo cuando el sistema ya está configurado con usuarios
                    var resultado = BackupService.HacerBackup();
                    if (!resultado.Exitoso)
                        MessageBox.Show($"Advertencia: no se pudo crear el backup.\n{resultado.Mensaje}",
                            "Backup", MessageBoxButton.OK, MessageBoxImage.Warning);

                    var login = new LoginWindow();
                    login.Show();
                }
                else
                {
                    // Primera ejecución: ir al registro del administrador
                    var registro = new RegistroAdminWindow();
                    registro.Show();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "ERROR",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

}