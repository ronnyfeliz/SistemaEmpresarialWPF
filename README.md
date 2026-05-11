# 🛒 Sistema de Gestión Comercial | C# + WPF + SQLite

Sistema de gestión comercial multiusuario desarrollado en **C# con WPF (.NET 10)** utilizando arquitectura **MVVM**, Entity Framework Core y SQLite como base de datos local.

El sistema fue diseñado para pequeños y medianos negocios, permitiendo administrar:

* Inventario
* Ventas
* Facturación
* Clientes deudores
* Proveedores
* Historiales
* Dashboard administrativo
* Usuarios y cajeros
* Notificaciones inteligentes
* Reportes y exportaciones

Todo funcionando de manera local sin necesidad de internet.

---

# 🚀 Características Principales

## 🔐 Seguridad y Acceso

✅ Registro inicial de administrador
✅ Inicio de sesión
✅ Roles de usuario
✅ Gestión de cajeros
✅ Control de sesiones
✅ Cierre de sesión seguro

---

## 📦 Inventario

✅ CRUD completo de productos
✅ Eliminación lógica
✅ Filtrado dinámico
✅ Control de stock
✅ Notificaciones de stock bajo
✅ Búsqueda por nombre, marca y categoría
✅ Visualización detallada de productos

---

## 💰 Ventas y Facturación

✅ Registro de ventas
✅ Carrito dinámico
✅ Validación automática de stock
✅ Generación automática de facturas
✅ Historial de ventas
✅ Filtros por fecha y factura
✅ Detalle de productos vendidos

---

## 👥 Clientes Deudores

✅ Registro de clientes deudores
✅ Historial de deudas
✅ Historial de pagos
✅ Validaciones de vencimiento
✅ Notificaciones automáticas
✅ Estados dinámicos de deuda

---

## 🚚 Proveedores

✅ Registro de proveedores
✅ Activación y desactivación
✅ Eliminación lógica
✅ Gestión de pagos
✅ Registro de transacciones
✅ Historial de movimientos
✅ Exportación de transacciones
✅ Control de deudas con proveedores

---

## 📊 Dashboard Administrativo

✅ Panel estadístico principal
✅ Resumen financiero
✅ Resumen de ventas
✅ Indicadores rápidos
✅ Información del sistema en tiempo real
✅ Acceso rápido a módulos

---

## 🔔 Sistema de Notificaciones

✅ Alertas automáticas de stock bajo
✅ Alertas de deudas vencidas
✅ Notificaciones inteligentes al iniciar sesión
✅ Ventanas emergentes dinámicas
✅ Badges visuales de alerta

---

# 🖥️ Tecnologías Utilizadas

| Tecnología            | Uso                                |
| --------------------- | ---------------------------------- |
| C#                    | Lógica del sistema                 |
| WPF                   | Interfaz gráfica                   |
| XAML                  | Diseño visual                      |
| MVVM                  | Arquitectura del proyecto          |
| SQLite                | Base de datos local                |
| Entity Framework Core | ORM                                |
| CommunityToolkit.Mvvm | Commands y propiedades observables |
| .NET 10               | Plataforma principal               |

---

# 🧠 Arquitectura MVVM

El sistema fue desarrollado utilizando el patrón:

```txt
MVVM (Model - View - ViewModel)
```

## 📂 Estructura del Proyecto

```bash
Project_v1/
│
├── Modelos/
├── Vista/
├── VistaModelos/
├── Servicios/
├── Helpers/
├── Migrations/
│
├── App.xaml
├── MainWindow.xaml
└── SistemaVentas.db
```

---

# 📦 Modelos Implementados

## Principales entidades del sistema

* Producto
* Usuario
* Rol
* Venta
* DetalleVenta
* Factura
* ClienteDeudor
* Notificacion
* Proveedor
* TransaccionProveedor
* HistorialPago

---

# 🔐 Sistema de Autenticación

## Roles Disponibles

| Rol           | Permisos                |
| ------------- | ----------------------- |
| Administrador | Acceso total al sistema |
| Cajero        | Ventas y consultas      |

## Funcionalidades

* Registro automático del primer administrador
* Validación de credenciales
* Gestión de múltiples cajeros
* Persistencia de sesión
* Redirección automática de ventanas

---

# 📦 Módulo de Inventario

## Funciones principales

* Registrar productos
* Editar productos
* Eliminar productos
* Activar y desactivar productos
* Búsqueda en tiempo real
* Filtros dinámicos
* Alertas automáticas de stock

## Información manejada

* Nombre
* Marca
* Categoría
* Descripción
* Precio
* Stock

---

# 💵 Módulo de Ventas

## Características

* Búsqueda avanzada de productos
* Carrito dinámico
* Facturación automática
* Descuento automático de inventario
* Validación de stock
* Historial de ventas
* Vista detallada de productos vendidos

## Formato de factura

```csharp
F20260503223639
```

---

# 📜 Historial de Ventas

## Funcionalidades implementadas

* Filtro por rango de fechas
* Filtro por número de factura
* Resumen financiero
* Detalle de ventas
* Productos vendidos por factura
* Actualización dinámica de resultados

---

# 👥 Módulo de Clientes Deudores

## Funciones

* Registrar clientes deudores
* Gestionar vencimientos
* Historial de pagos
* Alertas automáticas
* Notificaciones de mora
* Estado calculado en tiempo real

## Alertas implementadas

* Vence mañana
* Vence hoy
* Venció ayer
* Vencida hace X días

---

# 🚚 Módulo de Proveedores

## Funcionalidades principales

* Registro de proveedores
* Edición de proveedores
* Eliminación lógica
* Activación y desactivación
* Registro de pagos
* Historial de transacciones
* Gestión de deudas
* Exportación de transacciones

## Mejoras implementadas

* Organización avanzada del historial
* Mejoras visuales
* Integración completa con SQLite
* Control financiero interno

---

# 📊 Dashboard Administrativo

## Panel principal del sistema

El dashboard muestra información en tiempo real relacionada con:

* Ventas realizadas
* Productos registrados
* Stock bajo
* Clientes deudores
* Proveedores
* Resumen financiero
* Estadísticas rápidas

---

# 🔔 Sistema Inteligente de Notificaciones

## Stock Bajo

Productos detectados automáticamente con:

```csharp
Stock <= 10
```

## Funcionalidades

* Ventanas emergentes
* Badge rojo de alertas
* Integración automática al login
* Orden automático por prioridad

---

# 🛠️ Base de Datos

## Motor utilizado

```txt
SQLite
```

## ORM

```txt
Entity Framework Core
```

## Migraciones utilizadas

```powershell
Add-Migration NombreMigracion
Update-Database
```

---

# ⚙️ Instalación

## 1. Clonar repositorio

```bash
git clone https://github.com/tuusuario/tu-repositorio.git
```

---

## 2. Abrir solución

Abrir:

```txt
Project_v1.sln
```

en Visual Studio 2022.

---

## 3. Restaurar paquetes NuGet

```powershell
Restore NuGet Packages
```

---

## 4. Ejecutar migraciones

```powershell
Add-Migration InitialCreate
Update-Database
```

---

## 5. Ejecutar proyecto

```txt
F5
```

---

# 📈 Estado del Proyecto

| Módulo              | Estado           |
| ------------------- | ---------------- |
| Login y Seguridad   | ✅ Completo       |
| Gestión de Usuarios | ✅ Completo       |
| Inventario          | ✅ Completo       |
| Ventas              | ✅ Completo       |
| Facturación         | ✅ Completo       |
| Historial de Ventas | ✅ Completo       |
| Clientes Deudores   | ✅ Completo       |
| Notificaciones      | ✅ Completo       |
| Dashboard           | ✅ Completo       |
| Proveedores         | ✅ Completo       |
| Exportaciones       | ✅ Completo       |
| PDF e Impresión     | 🚧 En desarrollo |
| Backups automáticos | 🚧 Pendiente     |

---

# 📸 Capturas del Sistema

## Módulos disponibles

* Dashboard
* Login
* Inventario
* Ventas
* Historial de ventas
* Clientes deudores
* Proveedores
* Gestión de usuarios
* Notificaciones

> Próximamente se agregarán imágenes reales del sistema.

---

# 👨‍💻 Desarrolladores

## 👨‍💻 Ronny Feliz

🎓 Systems Engineering Student
💻 C# | WPF | SQLite | SQL | MVVM
🚀 Desarrollo Frontend y Backend

---

## 👨‍💻 Alex Hatton

💻 Arquitectura MVVM
🛠️ Backend y lógica del sistema
📦 Entity Framework Core y SQLite

---

# 🎯 Objetivo del Proyecto

Desarrollar una solución moderna y funcional de gestión comercial para pequeños y medianos negocios utilizando tecnologías desktop modernas con C#, WPF y SQLite.

El sistema busca optimizar:

* Control de inventario
* Procesos de venta
* Gestión de clientes
* Administración de proveedores
* Organización financiera
* Control operativo del negocio

---

# 📜 Licencia

Proyecto académico desarrollado con fines educativos y de aprendizaje.
