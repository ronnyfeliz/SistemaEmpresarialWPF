using System;
using System.IO;
using System.Linq;
using System.Windows;

namespace Project_v1.Servicios
{
    /* Servicio estático para gestionar los respaldos automáticos de la base de datos.
       No requiere instancia — todos los métodos se llaman directamente desde la clase.*/
    public static class BackupService
    {
        // ── Configuración general ──────────────────────────────────────
        private const string NombreDB = "SistemaVentas.db";  // Nombre del archivo de base de datos
        private const int MaxDias = 30;                  // Días máximos de historial de backups
        private const string CarpetaRaiz = "SistemaVentas";    // Nombre de carpeta raíz en Documentos y AppData

        /*  Ruta base donde se guardan los backups 
            Si el usuario configuró una ruta personalizada, la usa.
            Si no, usa la carpeta Documentos del usuario como ubicación por defecto.*/
        public static string RutaBackups
        {
            get
            {
                string rutaPersonalizada = LeerRutaPersonalizada();
                if (!string.IsNullOrWhiteSpace(rutaPersonalizada))
                    return rutaPersonalizada;

                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    CarpetaRaiz,
                    "Backups");
            }
        }

        /*  Hacer backup 
            Lógica: 1 backup por día. Si ya existe uno de hoy, lo sobreescribe.
            Organiza los archivos en subcarpetas: Backups/2026/05/07/
            Al terminar, elimina backups más antiguos que MaxDias días.*/
        public static BackupResultado HacerBackup()
        {
            try
            {
                // Verificar que el archivo .db existe antes de intentar copiarlo
                string rutaDB = ObtenerRutaDB();
                if (!File.Exists(rutaDB))
                {
                    var resNoDb = new BackupResultado(false, "No se encontró la base de datos.", null);
                    RegistrarEnLog(resNoDb); // Registrar el fallo en el historial
                    return resNoDb;
                }

                // Crear la carpeta del día si no existe: Backups/2026/05/07/
                string hoy = DateTime.Now.ToString("yyyy/MM/dd");
                string carpetaHoy = Path.Combine(RutaBackups, hoy);
                Directory.CreateDirectory(carpetaHoy);

                /* El nombre del archivo incluye solo la fecha (sin hora)
                   para garantizar que sea único por día y se sobreescriba si se abre la app varias veces*/
                string fecha = DateTime.Now.ToString("yyyyMMdd");
                string nombreBackup = $"SistemaVentas_{fecha}.db";
                string rutaDestino = Path.Combine(carpetaHoy, nombreBackup);

                // Copiar el .db al destino. overwrite:true sobreescribe si ya existe hoy
                File.Copy(rutaDB, rutaDestino, overwrite: true);

                // Limpiar backups de días que superen el límite configurado
                LimpiarDiasAntiguos();

                // Registrar operación exitosa en el log y retornar resultado
                var resOk = new BackupResultado(true, "Backup realizado correctamente.", rutaDestino);
                RegistrarEnLog(resOk);
                return resOk;
            }
            catch (Exception ex)
            {
                // Registrar el error en el log antes de retornar
                var resErr = new BackupResultado(false, $"Error al hacer backup: {ex.Message}", null);
                RegistrarEnLog(resErr);
                return resErr;
            }
        }

        /* Restaurar un backup 
           Reemplaza el archivo .db activo con el backup seleccionado por el usuario.
           Después de restaurar, la app debe reiniciarse para cargar los datos restaurados.*/
        public static BackupResultado RestaurarBackup(string rutaBackupSeleccionado)
        {
            try
            {
                /* Verificar primero que el archivo seleccionado realmente exista.
                   Si no existe, se registra el fallo en el historial.*/
                if (!File.Exists(rutaBackupSeleccionado))
                {
                    var resNoExiste = new BackupResultado(false, "El archivo de backup no existe.", null);
                    RegistrarEnLog(resNoExiste);
                    return resNoExiste;
                }

                string rutaDB = ObtenerRutaDB();

                /* Antes de restaurar, se guarda una copia de seguridad del estado actual.
                   Esto permite volver atrás si el usuario restauró un archivo incorrecto.*/
                string fechaSeguridad = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string nombreSeguridad = $"SistemaVentas_antesDeRestaurar_{fechaSeguridad}.db";
                string carpetaSeguridad = Path.Combine(RutaBackups, "Seguridad");
                Directory.CreateDirectory(carpetaSeguridad);
                File.Copy(rutaDB, Path.Combine(carpetaSeguridad, nombreSeguridad), overwrite: true);

                // Aquí se reemplaza la base activa por el backup elegido por el usuario.
                File.Copy(rutaBackupSeleccionado, rutaDB, overwrite: true);

                // Si todo salió bien, se registra la restauración exitosa en el log.
                var resOk = new BackupResultado(true, "Base de datos restaurada correctamente.", rutaBackupSeleccionado);
                RegistrarEnLog(resOk);
                return resOk;
            }
            catch (Exception ex)
            {
                // Cualquier error también se registra para que aparezca en el historial.
                var resErr = new BackupResultado(false, $"Error al restaurar: {ex.Message}", null);
                RegistrarEnLog(resErr);
                return resErr;
            }
        }

        public static DateTime? FechaUltimoBackup()
        {
            try
            {
                /* La hora más confiable del último backup es la registrada en el log,
                   porque refleja el momento exacto en que la operación terminó.
                   La fecha del archivo .db puede conservar la marca de tiempo del archivo origen
                   y por eso no siempre coincide con la hora real del respaldo.*/
                var ultimoExitoso = ObtenerHistorial()
                    .Where(x => x.Exitoso &&
                                x.Mensaje.Contains("Backup realizado correctamente", StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(x => x.Fecha)
                    .FirstOrDefault();

                return ultimoExitoso?.Fecha;
            }
            catch
            {
                return null;
            }
        }


        /*  Guardar ruta personalizada de backups 
         La ruta se guarda en un archivo de texto en AppData para que persista entre sesiones.*/
        public static void GuardarRutaPersonalizada(string ruta)
        {
            try { File.WriteAllText(ObtenerRutaConfig(), ruta); }
            catch { }
        }

        /* Restablecer ruta por defecto.
           Elimina el archivo de configuración, lo que hace que RutaBackups
           vuelva a usar la carpeta Documentos automáticamente. */
        public static void RestablecerRutaDefecto()
        {
            try
            {
                string cfg = ObtenerRutaConfig();
                if (File.Exists(cfg)) File.Delete(cfg);
            }
            catch { }
        }

        //Abrir carpeta de backups en el Explorador de Windows 
        public static void AbrirCarpetaBackups()
        {
            try
            {
                Directory.CreateDirectory(RutaBackups);
                System.Diagnostics.Process.Start("explorer.exe", RutaBackups);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo abrir la carpeta: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /*  Historial de operaciones de backup 
          Devuelve los últimos 50 registros del log, del más reciente al más antiguo.*/
        public static List<RegistroBackup> ObtenerHistorial()
        {
            var lista = new List<RegistroBackup>();
            try
            {
                string ruta = ObtenerRutaLog();
                if (!File.Exists(ruta)) return lista;

                // Leer el archivo, invertir el orden y tomar los últimos 50
                foreach (var linea in File.ReadAllLines(ruta).Reverse().Take(50))
                {
                    // Formato de cada línea: fecha|OK/ERROR|mensaje|ruta
                    var partes = linea.Split('|');
                    if (partes.Length < 4) continue;
                    if (!DateTime.TryParse(partes[0], out DateTime fecha)) continue;

                    lista.Add(new RegistroBackup
                    {
                        Fecha = fecha,
                        Exitoso = partes[1] == "OK",
                        Mensaje = partes[2],
                        RutaArchivo = partes[3]
                    });
                }
            }
            catch { }
            return lista;
        }

        /* Métodos privados. Devuelve la ruta completa del archivo .db junto al ejecutable*/
        private static string ObtenerRutaDB()
            => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, NombreDB);

        // Devuelve la ruta del archivo que guarda la ubicación personalizada de backups
        private static string ObtenerRutaConfig()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string carpeta = Path.Combine(appData, CarpetaRaiz);
            Directory.CreateDirectory(carpeta);
            return Path.Combine(carpeta, "backup_path.txt");
        }

        // Devuelve la ruta del archivo de log de operaciones
        private static string ObtenerRutaLog()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, CarpetaRaiz, "backup_log.txt");
        }

        /* Lee la ruta personalizada guardada por el usuario.
           Devuelve string.Empty si no hay ninguna configurada.*/
        private static string LeerRutaPersonalizada()
        {
            try
            {
                string cfg = ObtenerRutaConfig();
                return File.Exists(cfg) ? File.ReadAllText(cfg).Trim() : string.Empty;
            }
            catch { return string.Empty; }
        }

        /* Escribe una línea en el log con fecha, resultado y mensaje.
         Mantiene el archivo limitado a 100 líneas para no crecer indefinidamente.*/
        private static void RegistrarEnLog(BackupResultado resultado)
        {
            try
            {
                // Formato: 2026-05-07 14:30:00|OK|Backup realizado correctamente.|C:\...\archivo.db
                string linea = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}|" +
                               $"{(resultado.Exitoso ? "OK" : "ERROR")}|" +
                               $"{resultado.Mensaje}|" +
                               $"{resultado.RutaArchivo ?? ""}";

                File.AppendAllText(ObtenerRutaLog(), linea + Environment.NewLine);

                // Si supera 100 líneas, conservar solo las últimas 100
                string ruta = ObtenerRutaLog();
                var lineas = File.ReadAllLines(ruta);
                if (lineas.Length > 100)
                    File.WriteAllLines(ruta, lineas.TakeLast(100));
            }
            catch { }
        }

        // Devuelve todos los archivos .db de la carpeta de backups ordenados por fecha
        private static string[] ObtenerTodosLosBackups()
        {
            if (!Directory.Exists(RutaBackups)) return Array.Empty<string>();

            return Directory
                .GetFiles(RutaBackups, "*.db", SearchOption.AllDirectories)
                .OrderBy(f => File.GetLastWriteTime(f))
                .ToArray();
        }

        // Elimina carpetas de días cuya fecha sea anterior al límite de MaxDias
        private static void LimpiarDiasAntiguos()
        {
            try
            {
                if (!Directory.Exists(RutaBackups)) return;

                DateTime limite = DateTime.Today.AddDays(-MaxDias);

                foreach (var carpetaDia in Directory.GetDirectories(RutaBackups, "*", SearchOption.AllDirectories))
                {
                    // Convertir el path relativo "2026/05/01" a DateTime para comparar
                    string relativa = Path.GetRelativePath(RutaBackups, carpetaDia);
                    if (DateTime.TryParseExact(
                        relativa.Replace(Path.DirectorySeparatorChar, '/'),
                        "yyyy/MM/dd", null,
                        System.Globalization.DateTimeStyles.None,
                        out DateTime fechaCarpeta))
                    {
                        // Si la carpeta es más antigua que el límite, eliminarla completa
                        if (fechaCarpeta < limite)
                            Directory.Delete(carpetaDia, recursive: true);
                    }
                }

                // Limpiar carpetas de año/mes que hayan quedado vacías
                EliminarCarpetasVacias(RutaBackups);
            }
            catch { }
        }

        // Recorre recursivamente y elimina cualquier carpeta que haya quedado vacía
        private static void EliminarCarpetasVacias(string ruta)
        {
            foreach (var sub in Directory.GetDirectories(ruta))
            {
                EliminarCarpetasVacias(sub);
                if (!Directory.EnumerateFileSystemEntries(sub).Any())
                    Directory.Delete(sub);
            }
        }
    }

    /* Modelo de resultado de una operación de backup 
       Se usa para comunicar al llamador si el backup fue exitoso y con qué mensaje.*/
    public class BackupResultado
    {
        public bool Exitoso { get; }  // true si el backup se completó sin errores
        public string Mensaje { get; }  // Descripción del resultado o del error
        public string? RutaArchivo { get; }  // Ruta del archivo generado (null si falló)

        public BackupResultado(bool exitoso, string mensaje, string? ruta)
        {
            Exitoso = exitoso;
            Mensaje = mensaje;
            RutaArchivo = ruta;
        }
    }

    /* Modelo de un registro del historial de backups 
       Cada línea del log se deserializa en este objeto para mostrarlo en la UI.*/
    public class RegistroBackup
    {
        public DateTime Fecha { get; set; }  // Fecha y hora en que se realizó el backup
        public bool Exitoso { get; set; }  // true = OK, false = ERROR
        public string Mensaje { get; set; } = string.Empty;   // Mensaje del resultado
        public string RutaArchivo { get; set; } = string.Empty;   // Ruta del archivo respaldado
    }
}