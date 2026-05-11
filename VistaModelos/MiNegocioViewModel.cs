using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Project_v1.Modelos;
using Project_v1.Servicios;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using Project_v1.Vista;

namespace Project_v1.VistaModelos
{
    public partial class MiNegocioViewModel : ObservableObject
    {
        private int _negocioId = 0;

        [ObservableProperty] private string _nombreNegocio = string.Empty;
        [ObservableProperty] private string _numeroRNC = string.Empty;
        [ObservableProperty] private string _telefono = string.Empty;
        [ObservableProperty] private string _direccion = string.Empty;
        [ObservableProperty] private string _email = string.Empty;
        [ObservableProperty] private string _sitioWeb = string.Empty;
        [ObservableProperty] private string _slogan = string.Empty;
        [ObservableProperty] private string _rutaLogo = string.Empty;
        [ObservableProperty] private BitmapImage? _logoPreview;

        // Información visible en la UI sobre el estado actual de los backups.
        [ObservableProperty] private string _ultimoBackupTexto = "Calculando...";
        [ObservableProperty] private string _rutaBackupsTexto = string.Empty;

        public bool EsAdministrador => Sesion.RolActivo == Rol.Administrador;
        public MiNegocioViewModel()
        {
            CargarDatos();
            ActualizarInfoBackup();
        }

        [RelayCommand]
        private void VerHistorialBackups()
        {
            /* Abre una ventana pequeña con los últimos registros del log.
               Se usa ShowDialog para mantener el flujo enfocado en Mi Negocio.*/
            var ventana = new HistorialBackupsWindow();
            ventana.ShowDialog();
        }

        private void CargarDatos()
        {
            try
            {
                using var db = new AppDbContext();
                var negocio = db.Negocios.FirstOrDefault();
                if (negocio == null) return;

                _negocioId = negocio.Id;
                NombreNegocio = negocio.NombreNegocio;
                NumeroRNC = negocio.RNC;
                Telefono = negocio.Telefono;
                Direccion = negocio.Direccion;
                Email = negocio.Email;
                SitioWeb = negocio.SitioWeb;
                Slogan = negocio.Slogan;
                RutaLogo = negocio.RutaLogo;
                CargarVistaPrevia(RutaLogo);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar datos: {ex.Message}");
            }
        }

        [RelayCommand]
        private void Guardar()
        {
            if (Sesion.RolActivo != Rol.Administrador)
            {
                MessageBox.Show("Solo el administrador puede modificar la información del negocio.",
                    "Acceso denegado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(NombreNegocio))
            {
                MessageBox.Show("El nombre del negocio es obligatorio.",
                    "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using var db = new AppDbContext();
                Negocio? negocio = db.Negocios.Find(_negocioId);

                if (negocio == null)
                {
                    negocio = new Negocio();
                    db.Negocios.Add(negocio);
                }

                negocio.NombreNegocio = NombreNegocio;
                negocio.RNC = NumeroRNC;
                negocio.Telefono = Telefono;
                negocio.Direccion = Direccion;
                negocio.Email = Email;
                negocio.SitioWeb = SitioWeb;
                negocio.Slogan = Slogan;
                negocio.RutaLogo = RutaLogo;

                db.SaveChanges();
                _negocioId = negocio.Id;

                MessageBox.Show("Información guardada correctamente.",
                    "Guardado", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void SeleccionarLogo()
        {
            if (Sesion.RolActivo != Rol.Administrador)
            {
                MessageBox.Show("Solo el administrador puede cambiar el logo.",
                    "Acceso denegado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Seleccionar logo",
                Filter = "Imagenes (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg"
            };

            if (dialog.ShowDialog() == true)
            {
                RutaLogo = dialog.FileName;
                CargarVistaPrevia(RutaLogo);
            }
        }

        [RelayCommand]
        private void EliminarLogo()
        {
            if (Sesion.RolActivo != Rol.Administrador)
            {
                MessageBox.Show("Solo el administrador puede eliminar el logo.",
                    "Acceso denegado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            RutaLogo = string.Empty;
            LogoPreview = null;
        }

        private void CargarVistaPrevia(string ruta)
        {
            if (string.IsNullOrWhiteSpace(ruta) || !File.Exists(ruta))
            {
                LogoPreview = null;
                return;
            }

            try
            {
                BitmapImage imagen = new();
                imagen.BeginInit();
                imagen.UriSource = new Uri(ruta, UriKind.Absolute);
                imagen.CacheOption = BitmapCacheOption.OnLoad;
                imagen.EndInit();
                imagen.Freeze();
                LogoPreview = imagen;
            }
            catch
            {
                LogoPreview = null;
            }
        }

        private void ActualizarInfoBackup()
        {
            RutaBackupsTexto = BackupService.RutaBackups;

            DateTime? ultimo = BackupService.FechaUltimoBackup();
            UltimoBackupTexto = ultimo.HasValue
                ? ultimo.Value.ToString("dd/MM/yyyy  HH:mm")
                : "Sin backups aún";
        }

        [RelayCommand]
        private void HacerBackupAhora()
        {
            var resultado = BackupService.HacerBackup();

            if (resultado.Exitoso)
            {
                ActualizarInfoBackup();
                MessageBox.Show("Backup realizado correctamente.\n" + resultado.RutaArchivo,
                    "Backup", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show(resultado.Mensaje,
                    "Error en backup", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        [RelayCommand]
        private void AbrirCarpetaBackups()
            => BackupService.AbrirCarpetaBackups();

        [RelayCommand]
        private void CambiarUbicacionBackup()
        {
            if (Sesion.RolActivo != Rol.Administrador)
            {
                MessageBox.Show("Solo el administrador puede cambiar la ubicación del backup.",
                    "Acceso denegado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            /* Este truco usa SaveFileDialog solo para permitir elegir una carpeta
               sin depender de Windows Forms.*/
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Selecciona la carpeta donde se guardarán los backups",
                FileName = "Seleccionar carpeta aquí",
                Filter = "Carpeta|*.nofolder",
                CheckFileExists = false,
                CheckPathExists = true
            };

            if (dialog.ShowDialog() == true)
            {
                string carpeta = Path.GetDirectoryName(dialog.FileName)!;
                BackupService.GuardarRutaPersonalizada(carpeta);
                ActualizarInfoBackup();

                MessageBox.Show($"Nueva ubicación guardada:\n{carpeta}",
                    "Ubicación actualizada", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        [RelayCommand]
        private void RestablecerRutaDefecto()
        {
            if (Sesion.RolActivo != Rol.Administrador)
            {
                MessageBox.Show("Solo el administrador puede restablecer la ruta de backups.",
                    "Acceso denegado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            BackupService.RestablecerRutaDefecto();
            ActualizarInfoBackup();

            MessageBox.Show("Ubicación restablecida a la carpeta Documentos.",
                "Restablecido", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        [RelayCommand]
        private void RestaurarBackup()
        {
            if (Sesion.RolActivo != Rol.Administrador)
            {
                MessageBox.Show("Solo el administrador puede restaurar backups.",
                    "Acceso denegado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Solo se permiten archivos .db para reducir errores del usuario.
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Seleccionar backup para restaurar",
                Filter = "Base de datos SQLite (*.db)|*.db",
                CheckFileExists = true,
                Multiselect = false
            };

            if (dialog.ShowDialog() != true)
                return;

            // Restaurar reemplaza la base activa, por eso se confirma de forma explícita.
            var confirmar = MessageBox.Show(
                "Se reemplazará la base de datos actual por el backup seleccionado.\n\n" +
                "La aplicación se reiniciará al finalizar.\n\n" +
                "¿Deseas continuar?",
                "Confirmar restauración",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirmar != MessageBoxResult.Yes)
                return;

            var resultado = BackupService.RestaurarBackup(dialog.FileName);

            if (!resultado.Exitoso)
            {
                MessageBox.Show(resultado.Mensaje,
                    "Error al restaurar", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            MessageBox.Show(
                "Base de datos restaurada correctamente.\nLa aplicación se reiniciará ahora.",
                "Restauración completada",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            ReiniciarAplicacion();
        }

        private static void ReiniciarAplicacion()
        {
            /* Se relanza el ejecutable actual y luego se cierra la instancia activa
               para que la app cargue la base restaurada desde cero.*/
            string exe = Environment.ProcessPath
                ?? Process.GetCurrentProcess().MainModule?.FileName
                ?? throw new InvalidOperationException("No se pudo determinar la ruta de la aplicación.");

            Process.Start(new ProcessStartInfo
            {
                FileName = exe,
                UseShellExecute = true
            });

            Application.Current.Shutdown();
        }
    }
}