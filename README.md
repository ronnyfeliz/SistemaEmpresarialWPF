# 🏪 Sistema de Gestión Comercial — C# WPF

> Sistema de gestión multiuso orientado a ventas, facturación e inventario para pequeños negocios y empresas. Desarrollado como proyecto académico del curso de **Lenguaje de Programación I** — Universidad Autónoma de Santo Domingo (UASD), 2026.

---

## 📋 Tabla de Contenidos

- [Descripción General](#descripción-general)
- [Tecnologías Utilizadas](#tecnologías-utilizadas)
- [Arquitectura del Proyecto](#arquitectura-del-proyecto)
- [Módulos del Sistema](#módulos-del-sistema)
- [Instalación y Configuración](#instalación-y-configuración)
- [Historial de Desarrollo](#historial-de-desarrollo)
- [Errores Clave y Soluciones](#errores-clave-y-soluciones)
- [Estado del Proyecto](#estado-del-proyecto)
- [Autores](#autores)

---

## 📌 Descripción General

El **Sistema de Gestión Comercial** centraliza las operaciones de ventas, facturación e inventario para pequeños negocios como colmados, cafeterías, minimarkets y tiendas. Funciona **100% local, sin conexión a internet**.

| Elemento         | Detalle                          |
|------------------|----------------------------------|
| Lenguaje         | C#                               |
| Framework        | WPF (.NET 10)                    |
| Base de datos    | SQLite (local)                   |
| ORM              | Entity Framework Core            |
| Patrón de diseño | MVVM                             |
| Roles            | Administrador y Cajero           |
| Conectividad     | 100% local, sin internet         |

---

## 🛠️ Tecnologías Utilizadas

### Paquetes NuGet

| Paquete | Función |
|--------|---------|
| `Microsoft.EntityFrameworkCore.Sqlite` | Motor SQLite integrado con EF Core |
| `Microsoft.EntityFrameworkCore.Tools` | Herramientas para migraciones |
| `Microsoft.EntityFrameworkCore.Design` | Comandos de migración desde consola |
| `CommunityToolkit.Mvvm` | Simplifica la implementación del patrón MVVM |
| `QuestPDF` | Generación profesional de documentos PDF |
| `ClosedXML` | Exportación de datos a Excel |
| `System.IO.Compression` | Exportación ZIP de facturas |

### Herramientas de Desarrollo

- Visual Studio 2022
- .NET 10
- SQLite (embebido, sin instalación separada)

---

## 🏗️ Arquitectura del Proyecto

El proyecto sigue el patrón **MVVM** (Model–View–ViewModel), organizando el código en capas con responsabilidades específicas:

```
Project_v1/
├── Modelos/          → Clases que representan tablas de la BD
├── VistaModelos/     → Lógica de cada pantalla
├── Vista/            → Pantallas XAML
├── Servicios/        → DbContext y servicios de sesión
├── Helpers/          → Utilidades y conversores
├── Migrations/       → Historial de migraciones EF Core
├── App.xaml          → Punto de entrada y recursos globales
└── MainWindow.xaml   → Ventana principal con menú lateral
```

### Modelos de la Base de Datos

| Modelo | Descripción |
|--------|-------------|
| `Producto` | Tabla de productos del inventario |
| `Usuario` | Tabla de usuarios del sistema |
| `Rol` | Enum: `Administrador` / `Cajero` |
| `Sesion` | Clase estática con datos del usuario autenticado |
| `Venta` | Registro de cada venta (genera número de factura único) |
| `DetalleVenta` | Productos incluidos en cada venta |
| `DetalleServicioVenta` | Servicios incluidos en cada venta |
| `ClienteDeudor` | Clientes que adquieren a crédito |
| `Notificacion` | Alertas internas generadas automáticamente |
| `Proveedor` | Registro de proveedores del negocio |
| `MovimientoProveedor` | Trazabilidad individual de pagos a proveedores |

---

## 🧩 Módulos del Sistema

### 🔐 Autenticación y Roles
- Registro automático del primer administrador en el primer uso.
- Login con validación de credenciales por roles.
- Sesión activa con cierre controlado.
- Soporte para hasta **5 cajeros simultáneos**.

### 📦 Inventario
- CRUD completo de productos (nombre, marca, categoría, descripción, precio, stock).
- Eliminación lógica (`Activo = false`).
- Filtros dinámicos en tiempo real por nombre, marca, categoría, precio y stock.
- **Alertas automáticas de stock bajo** (≤ 10 unidades) con badge visual rojo.

### 🛒 Ventas y Facturación
- Búsqueda de productos por nombre o marca con descripción visible.
- Carrito dinámico con validación de stock.
- Soporte para ventas de **productos y servicios** en la misma transacción.
- Generación automática de factura con número único:
  ```
  F{yyyyMMddHHmmss}  →  Ejemplo: F20260503223639
  ```
- Descuento automático de stock al confirmar la venta.
- Panel de **ventas del día** con métricas en tiempo real.

### 👥 Clientes Deudores
- Registro con nombre, cédula, teléfono, monto, fecha de inicio y fecha límite de pago.
- Validaciones: fecha de inicio = hoy, fecha de pago ≥ 30 días después.
- **Notificaciones automáticas** al iniciar sesión según estado de la deuda:

| Condición | Estado mostrado |
|-----------|----------------|
| 1 día antes del vencimiento | Vence mañana |
| El mismo día | Vence hoy |
| 1 día después | Venció ayer |
| Días posteriores | Vencida hace X días |

- Historial detallado por cliente con notificaciones ordenadas por fecha.
- Exportación a PDF y Excel.

### 🏭 Proveedores
- Registro, edición y eliminación lógica de proveedores.
- Filtros por nombre, RNC, tipo (Productos / Servicios / Ambos) y estado.
- Registro de transacciones de mercancía y pagos individuales.
- Historial detallado con exportación a Excel y PDF.
- Alertas automáticas de vencimiento de transacciones al iniciar sesión.
- Comprobantes visuales personalizados.

### 📊 Historial de Ventas
- Vista con tabla maestra-detalle.
- Filtros por rango de fechas, número de factura y usuario/cajero.
- Resumen con total de ventas y monto acumulado del período.
- Exportación individual de facturas en **PDF**.
- Exportación masiva del historial en **ZIP** organizado por año/mes/día.

### 📈 Dashboard
- Métricas clave del negocio en una sola pantalla:
  - Ventas del día y del mes.
  - Cantidad de deudores activos.
  - Productos con stock bajo.
  - Fecha del último backup.
  - Últimas 5 ventas realizadas.

### 🏢 Mi Negocio / Configuración
- Nombre, teléfono, dirección, RNC, eslogan, correo y logo del negocio.
- Información integrada automáticamente en facturas y reportes exportados.

### 💾 Respaldo y Mantenimiento
- Backup automático diario al iniciar la aplicación.
- Máximo 1 backup por día (sobreescritura).
- Conservación de los últimos **30 días** de historial.
- Organización automática: `Documentos/SistemaVentas/Backups/AÑO/MES/DÍA/`
- Ruta personalizable y persistente entre sesiones.
- Restauración de backup desde la interfaz gráfica.
- Historial de operaciones de backup con estado (éxito/error).
- Exportación del historial de backups a PDF y Excel.

### 📤 Exportaciones
| Módulo | Excel | PDF | ZIP |
|--------|-------|-----|-----|
| Historial de Ventas | ✅ | ✅ | ✅ |
| Resumen Diario | ✅ | ✅ | — |
| Clientes Deudores | ✅ | ✅ | — |
| Clientes Pagados | ✅ | ✅ | — |
| Inventario | ✅ | — | — |
| Servicios | ✅ | — | — |
| Proveedores | ✅ | ✅ | — |
| Backups | ✅ | ✅ | — |

---

## ⚙️ Instalación y Configuración

### Requisitos del Sistema
- Windows 10 / 11
- Visual Studio 2022
- .NET 10 SDK
- Sin conexión a internet requerida

### Pasos de Instalación

1. **Clonar el repositorio:**
   ```bash
   git clone https://github.com/tu-usuario/sistema-gestion-comercial.git
   cd sistema-gestion-comercial
   ```

2. **Abrir en Visual Studio:**
   - Abrir `Project_v1.sln` en Visual Studio 2022.

3. **Restaurar paquetes NuGet:**
   - Visual Studio los restaurará automáticamente al abrir la solución.
   - O manualmente: `Tools > NuGet Package Manager > Manage NuGet Packages for Solution`.

4. **Aplicar migraciones de base de datos:**
   ```powershell
   # En Package Manager Console (Tools > NuGet PM > Package Manager Console)
   Update-Database
   ```
   > El archivo `SistemaVentas.db` se genera automáticamente al ejecutar la aplicación.

5. **Ejecutar la aplicación:**
   - Presionar `F5` o `Ctrl+F5` en Visual Studio.
   - En el **primer uso**, el sistema abrirá la ventana de registro del administrador inicial.

### Configuración Inicial
Al abrir por primera vez, el sistema solicita crear el usuario **Administrador**. Este usuario tiene acceso completo al sistema. Los cajeros se gestionan desde el módulo de usuarios dentro de la aplicación.

---

## 📅 Historial de Desarrollo

El sistema fue desarrollado entre abril y mayo de 2026, en 20 fases iterativas:

| Fase | Fecha | Responsable | Descripción |
|------|-------|-------------|-------------|
| 1 | Abril 2026 | Alex Hatton | Configuración inicial, modelo `Producto`, `AppDbContext`, SQLite y migraciones |
| 2 | Mayo 01 | Ronny Feliz | Modelos `Usuario` y `Rol`, interfaz de Login y Registro de Administrador |
| 3 | Mayo 01 | Alex Hatton | Sistema completo de autenticación, `Sesion.cs`, lógica de inicio (`App.xaml.cs`) |
| 4 | Mayo 2026 | Ronny Feliz | Módulos funcionales: `Venta`, `DetalleVenta`, `Factura`, `Cliente`, `Deuda`; interfaces XAML |
| 5 | Mayo 02 | Alex Hatton | Módulo de Clientes Deudores, campos `Cedula` y `Telefono`, notificaciones automáticas, historial |
| 6 | Mayo 02 | Ronny Feliz | Integración final, corrección de errores de BD, mejoras visuales en XAML |
| 7 | Mayo 03 | Ronny Feliz | Notificaciones de stock bajo, expansión a 5 cajeros, mejoras en módulo de ventas |
| 8 | Mayo 2026 | Alex Hatton | Historial de Ventas con filtros, detalle por venta, corrección del guardado de `DetalleVenta` |
| 9 | Mayo 04 | Ronny Feliz | Optimización visual, notificaciones inteligentes al login, gestión de múltiples cajeros |
| 10 | Mayo 04 | Ronny Feliz | Inicio de exportación de facturas PDF, módulo de información del negocio |
| 11 | Mayo 2026 | Equipo | Reportes avanzados, exportación masiva ZIP, filtro por usuario en historial |
| 12 | Mayo 2026 | Ronny Feliz | Integración de QuestPDF, exportación PDF de clientes deudores, ScrollViewer, badges |
| 13 | Mayo 05 | Ronny Feliz | Integración de servicios en ventas (`DetalleServicioVenta`), panel de ventas del día |
| 14 | Mayo 2026 | Alex Hatton | Sistema de respaldo automático (`BackupService`), interfaz de mantenimiento |
| 15 | Mayo 05 | Ronny Feliz | Corrección de cierre de sesión, exportación a Excel (ClosedXML), mejoras en UI |
| 16 | Mayo 06 | Ronny Feliz | Resumen diario de ventas exportable en Excel y PDF, métricas tipo dashboard |
| 17 | Mayo 07 | Alex Hatton | Dashboard con métricas clave, restauración de backups desde UI, exportación de clientes pagados a PDF |
| 18 | Mayo 05 | Ronny Feliz | Sistema de backups mejorado, historial de backups exportable, ZIP organizado por fecha |
| 19 | Mayo 07 | Ronny Feliz | Módulo completo de Proveedores: registro, transacciones, pagos, historial, comprobantes |
| 20 | Mayo 10 | Ronny Feliz | **Fase final:** notificaciones de vencimiento de proveedores, pulido general, estabilización |

---

## 🐛 Errores Clave y Soluciones

<details>
<summary>Ver todos los errores documentados</summary>

**Error 1 — `Add-Migration` falla con `ErrorActionPreference`**
- **Causa:** Faltaba el paquete `Microsoft.EntityFrameworkCore.Design`.
- **Solución:** Instalar el paquete desde NuGet y repetir el comando.

**Error 2 — Origen del paquete NuGet desactivado**
- **Causa:** `nuget.org` no estaba habilitado en Visual Studio.
- **Solución:** `Tools > Options > NuGet Package Manager > Package Sources`. Verificar que `nuget.org` esté habilitado con la URL `https://api.nuget.org/v3/index.json`.

**Error 3 — Botón de registro no funcionaba**
- **Causa:** `BtnRegistrar_Click` vacío, ViewModel no asignado como `DataContext`.
- **Solución:** Asignar el ViewModel en el constructor y conectar el botón al comando.

**Error 4 — Notificación aparecía antes del login**
- **Causa:** La lógica estaba en `App.xaml.cs` y se ejecutaba antes de autenticarse.
- **Solución:** Mover la lógica al `LoginViewModel`.

**Error 5 — Botón Eliminar no aparecía**
- **Causa:** Grid con 16 `RowDefinitions` pero botón en `Grid.Row=16` (inexistente).
- **Solución:** Agregar una `RowDefinition` adicional.

**Error 6 — Cerrar sesión no cerraba la ventana correcta**
- **Causa:** `Application.Current.MainWindow.Close()` no siempre referencia la ventana correcta.
- **Solución:** Pasar referencia directa al ViewModel y usar `_ventana.Close()`.

**Error 7 — Notificaciones de deuda perdidas**
- **Causa:** Condición `diasDiferencia % 5 == 0` muy restrictiva.
- **Solución:** Notificar cualquier día posterior al vencimiento, con validación `yaRegistradaHoy`.

**Error 8 — CS0759: clase sin declaración `partial`**
- **Causa:** Faltaba `partial` en `HistorialVentasViewModel`, impidiendo a CommunityToolkit.Mvvm generar código.
- **Solución:** `public partial class HistorialVentasViewModel : ObservableObject`

**Error 9 — Detalles de venta no se guardaban en la BD**
- **Causa:** Referencias a objetos `Producto` en memoria causaban conflictos de seguimiento en EF Core.
- **Solución:** Crear objetos `DetalleVenta` limpios usando solo IDs, sin referencias a objetos en memoria.

**Error 10 — `HistorialVentasControl` no reconocido en `MainWindow.xaml`**
- **Causa:** La clase base en el code-behind era `Window` en lugar de `UserControl`.
- **Solución:** `public partial class HistorialVentasControl : UserControl`

</details>

---

## ✅ Estado del Proyecto

| Módulo | Estado |
|--------|--------|
| Estructura MVVM y carpetas | ✅ Completo |
| Autenticación y roles | ✅ Completo |
| Módulo de Inventario con filtros | ✅ Completo |
| Módulo de Ventas con carrito y factura | ✅ Completo |
| Módulo de Servicios en ventas | ✅ Completo |
| Módulo de Clientes Deudores | ✅ Completo |
| Módulo de Proveedores | ✅ Completo |
| Notificaciones automáticas al iniciar sesión | ✅ Completo |
| Dashboard con métricas | ✅ Completo |
| Historial de Ventas con filtros | ✅ Completo |
| Historial de Clientes Pagados | ✅ Completo |
| Módulo de Usuario (gestión de cajeros) | ✅ Completo |
| Respaldo automático y manual | ✅ Completo |
| Restauración de backup desde UI | ✅ Completo |
| Exportación PDF (facturas, deudores, proveedores) | ✅ Completo |
| Exportación Excel (inventario, ventas, deudores) | ✅ Completo |
| Exportación ZIP del historial de ventas | ✅ Completo |
| Panel de ventas del día con resumen | ✅ Completo |
| Información del negocio en facturas | ✅ Completo |

---

## 👨‍💻 Autores

| Nombre | Matrícula | Rol en el proyecto |
|--------|-----------|--------------------|
| **Alex Hatton** | 100672673 | Configuración inicial, autenticación, clientes deudores, historial de ventas, sistema de backups, dashboard |
| **Ronny Feliz** | 100704427 | Módulos funcionales, integración final, notificaciones, exportaciones PDF/Excel/ZIP, proveedores, fase final |
| **Jeison Avalo** | 100719543 | Testing del programa, búsqueda de errores y bugs, investigación teórica sobre tokens y backups |

> Proyecto académico — Lenguaje de Programación I  
> **Universidad Autónoma de Santo Domingo (UASD)** · 2026  
> Fecha de entrega final: **14 de mayo de 2026**

---

## 📄 Licencia

Este proyecto fue desarrollado con fines académicos. Para uso comercial, contactar a los autores.
