# AGENTS.md - Contexto técnico del proyecto

## Dominio

Sistema de Gestión de **Autolavado (Car Wash)**.

Todo el vocabulario del catálogo, la base de datos y la interfaz debe corresponder a autolavado y vehículos: `Lavado Exterior`, `Lavado Completo`, `Encerado y Pulido`, `Limpieza de Tapiz`, `Shampoo de Motor`, `Aromatización`, `Aspirado Interior`, `Sanitizado Interior`. No existe lavandería de ropa: no usar términos como *ropa*, *planchado*, *detergente* ni *por kilo*.

## Stack tecnológico

- **Backend:** ASP.NET Core MVC en C# / .NET 10 (`net10.0`), con `AddControllersWithViews()`, sesión en memoria (`AddDistributedMemoryCache` + `AddSession`) y autenticación por formulario/sesión (sin Identity/EF).
- **Vistas:** Razor (`.cshtml`) con `_Layout.cshtml`.
- **Frontend:** JavaScript puro, sin frameworks:
  - `wwwroot/js/clientes.js` - alta/edición/eliminación de clientes y búsqueda.
  - `wwwroot/js/ventas.js` - carrito de ventas, descuento 0-20%, resumen en vivo, validación/mensajes y envío JSON.
  - `wwwroot/js/site.js` - ocultado automático de toasts.
- **Base de datos:** PostgreSQL. Acceso con **Npgsql** desde clases `Data/*DAO.cs`. **No se usa Entity Framework Core**.
- **Conexión:** clave `Pepokita` en `appsettings.json` (`Host=localhost;Port=5432;Database=Pepokita;Username=postgres;...`). **BD activa:** `Pepokita`. Los nombres `bd_pepokita` y `Bakud_pepopkita` en la raíz del workspace son dumps/respaldos históricos, no la base en uso por la app.
- **Solución/proyectos:** no hay `.sln`; proyecto principal `PrototipoCRUD/PrototipoCRUD.csproj` y pruebas `PrototipoCRUD.Tests/PrototipoCRUD.Tests.csproj` (referencia al proyecto principal).

## Reglas de ejecución

- Trabajar desde la raíz donde está `PrototipoCRUD.csproj` (`PrototipoCRUD/`) salvo que se indique lo contrario.
- Levantar la aplicación (desde `PrototipoCRUD/`):

  ```bash
  dotnet watch run
  ```

- En Windows se puede usar `iniciar.bat` (sitúa en la raíz del proyecto, ejecuta `dotnet watch run`).
- **No usar Angular ni `ng serve`**: no hay `package.json`, `angular.json` ni frontend SPA.
- Verificación obligatoria:

  ```bash
  dotnet build --nologo -v minimal
  ```

  Debe terminar con **0 advertencias y 0 errores**.

## Pruebas y comandos útiles

- **Build app (desde `PrototipoCRUD/`):** `dotnet build --nologo -v minimal`
- **Pruebas unitarias (desde la raíz del workspace):** `dotnet test PrototipoCRUD.Tests\PrototipoCRUD.Tests.csproj --nologo`
  - Alternativa desde `PrototipoCRUD/`: `dotnet test ..\PrototipoCRUD.Tests\PrototipoCRUD.Tests.csproj --nologo`
- **Ejecutar app:** `dotnet run` o `dotnet watch run` (desde `PrototipoCRUD/`)
- **Aplicar SQL CR-01 manualmente/idempotente (opcional, `AsegurarEstructura` también lo aplica al iniciar):**
  - `psql -h localhost -p 5432 -U postgres -d Pepokita -v ON_ERROR_STOP=1 -f PrototipoCRUD\Data\cr01_descuentos_vendedor.sql`

## Autenticación, roles y credenciales de prueba (CR-01)

- Login por sesión: `AccountController` (`/Account/Login`, `/Account/Logout`), `UsuarioDAO`, `Services/ServicioAutenticacion.cs`.
- Contraseñas almacenadas solo como hash seguro **PBKDF2-HMAC-SHA256** (`v1$saltBase64$hashBase64`, 100000 iteraciones), nunca texto plano.
- Bloqueo tras **5 intentos fallidos** (`intentos_fallidos`, `bloqueado`); acceso exitoso reinicia intentos y registra `ultimo_acceso`. Mensajes genéricos (no revelan si falló usuario o contraseña).
- **Usuario inicial seed (CR-01):** usuario `cperez` / contraseña `Vendedor123!` (Carlos Perez, rol `Vendedor`, empleado `VEN-0001`).
- Permisos por rol en dominio/UI: `PuedeAplicarDescuento()` (Vendedor/Administrador) y `PuedeVerAuditoria()` (Administrador/Gerente). El menú muestra Auditoría solo a roles con permiso.
- Protección: `Venta/Registrar`, `Venta/Historial`, `Venta/Auditoria` requieren sesión.
  - GET sin sesión: redirige a `/Account/Login?returnUrl=...`
  - POST `/Venta/Registrar` sin sesión: `401` JSON `{ ok:false, mensaje:"Tu sesión expiró. Inicia sesión para continuar." }`
  - Auditoría con sesión pero sin rol permitido: redirige a login.

## Arquitectura de base de datos (3FN) y CR-01

Esquema `public`, normalizado (3FN). Llaves primarias/foráneas, índices y `CHECK` de integridad.

| Tabla | Función y relaciones principales |
| --- | --- |
| `clientes` | Datos maestros de clientes. `UNIQUE (documento)`. |
| `vehiculos` | Vehículos por cliente. FK `cliente_id` -> `clientes`, FK `tipo_vehiculo_id` -> `tipos_vehiculo`. `UNIQUE (placa/patente)`. |
| `tipos_vehiculo` | Catálogo de tipos de vehículo. `UNIQUE (nombre)`. |
| `roles` | Catálogo de roles del personal. `UNIQUE (nombre)`. Incluye `Vendedor` (CR-01). |
| `empleados` | Personal. FK `rol_id` -> `roles`. `UNIQUE (documento)`. |
| `usuarios` | Accesos/login (CR-01). FK `empleado_id` -> `empleados` (1:1). `UNIQUE (nombre_usuario)`, `contrasena_hash`, `activo`, `intentos_fallidos` (>=0), `bloqueado`, `ultimo_acceso`. |
| `servicios` | Catálogo de servicios de autolavado. `UNIQUE (nombre)`. |
| `precios_servicio` | Precio por servicio y tipo de vehículo. FKs `servicio_id`, `tipo_vehiculo_id`; `UNIQUE (servicio_id, tipo_vehiculo_id)`. |
| `ordenes_servicio` | Orden de trabajo. FKs `vehiculo_id`, `empleado_id`. |
| `orden_detalles` | Detalle de la orden. FKs `orden_id`, `servicio_id`. |
| `pagos` | Pagos por orden. FK `orden_id`; `UNIQUE (orden_id)`. |
| `productos` | Catálogo vendible. `UNIQUE (nombre)`; `CHECK` de precio >= 0. |
| `ventas` | Cabecera de venta. FK `id_cliente` -> `clientes` (`ON DELETE RESTRICT`). **Sin columna de texto `cliente`.** CR-01: `vendedor` (`VARCHAR`, NOT NULL, default `Sin registrar`), `id_vendedor` (FK -> `empleados`), `subtotal_bruto`, `porcentaje_descuento` (0-20), `monto_descuento`, `total_final`; `total` se mantiene coherente con `total_final`. |
| `detalle_ventas` | Ítems de venta. FKs `id_venta` -> `ventas`, `id_producto` -> `productos`. **Sin columna `nombre_producto`.** |

**Restricciones/índices CR-01 en `ventas`:**
- `ck_ventas_descuento_rango`: `porcentaje_descuento >= 0 AND porcentaje_descuento <= 20`
- `ck_ventas_montos_coherentes`: coherencia de `subtotal_bruto`, `monto_descuento = ROUND(subtotal_bruto * porcentaje_descuento / 100, 2)`, `total_final = subtotal_bruto - monto_descuento`, `total = total_final`
- `ck_ventas_vendedor`: `LENGTH(BTRIM(vendedor)) > 0 AND (porcentaje_descuento = 0 OR vendedor <> 'Sin registrar')`
- Índices: `idx_ventas_vendedor`, `idx_ventas_id_vendedor`
- Backfill idempotente para ventas antiguas: `subtotal_bruto = total_final = total`, `vendedor = 'Sin registrar'`, `porcentaje_descuento = 0`, `monto_descuento = 0`.

### Scripts de esquema y datos

- `Data/cr01_descuentos_vendedor.sql`: cambios CR-01 idempotentes (usuarios, rol Vendedor, columnas/restricciones/índices/backfill en ventas, seed usuario `cperez`).
- `Data/migracion_3fn.sql`: migración 3FN aplicada + bloque CR-01 idempotente incorporado al final.
- `Data/crear_tablas_ventas.sql`: esquema de ventas/productos/detalle 3FN + CR-01 idempotente (referencia/manual).
- `Data/seed_autolavado.sql`: seed idempotente del dominio autolavado.
- `ClienteDAO.AsegurarEstructura()` y `VentaDAO.AsegurarEstructura()` aplican preparación idempotente al iniciar la app (incluye lógica CR-01 en `VentaDAO`).
- Regla: no reintroducir columnas denormalizadas ni datos de lavandería de ropa.

## Estado actual

### Módulos implementados y activos

- **Clientes:** `Controllers/ClienteController.cs`, `Views/Cliente/Index.cshtml`, `wwwroot/js/clientes.js`. CRUD/flujo existente.
- **Autenticación (CR-01):** `Controllers/AccountController.cs`, `Views/Account/Login.cshtml`, `Data/UsuarioDAO.cs`, `Services/ServicioAutenticacion.cs`, `Models/Usuario.cs`; sesión configurada en `Program.cs`.
- **Ventas (CR-01):**
  - `Controllers/VentaController.cs`, `Data/VentaDAO.cs`, `Models/Venta.cs`, `Models/VentaRequest.cs`, `Models/VentaHistorial.cs`, `Models/ResumenDescuentoVendedor.cs`, `Models/DetalleVenta.cs`
  - Vistas: `Views/Venta/Registrar.cshtml`, `Views/Venta/Historial.cshtml`, `Views/Venta/Auditoria.cshtml`
  - JS/CSS: `wwwroot/js/ventas.js`, `wwwroot/css/site.css`
  - Registro transaccional (cabecera + detalle en misma transacción), precios desde BD, antiforgery en POST JSON (`RequestVerificationToken`), vendedor tomado de sesión (solo lectura en UI), descuento 0-20% con cálculo en dominio (`AplicarDescuento`, `Math.Round(...,2,AwayFromZero)`), resumen en vivo (Subtotal bruto, Descuento Bs., Total final).
  - Validación/UX de descuento inválido (<0 o >20): `ArgumentOutOfRangeException` capturada en controlador y mensaje de UI/servidor no punitivo en tono ámbar: `"El descuento máximo permitido es 20%. Ingresa un valor entre 0% y 20% para continuar."` sin borrar ítems cargados.
  - Historial muestra vendedor, subtotal bruto, % y monto descuento, total final. Auditoría agrupa ventas con descuento por vendedor (`ObtenerAuditoriaDescuentos`).
- **Inicio/Layout:** `Views/Home/Index.cshtml`, `Views/Shared/_Layout.cshtml` (muestra usuario/rol, cerrar sesión, enlaces a Registrar/Historial y Auditoría según rol).

### Pendiente

- Implementar **Órdenes de Servicio** y **Pagos** (tablas/datos pueden existir; no hay controladores/vistas completas). Seguir patrón DAO Npgsql + controlador + Razor + JS puro.

### Verificación reciente

- `dotnet build --nologo -v minimal` (en `PrototipoCRUD/`): 0 advertencias, 0 errores.
- `dotnet test PrototipoCRUD.Tests\PrototipoCRUD.Tests.csproj --nologo`: correcto (incluye descuento 0/15/20, >20 y <0 lanzan `ArgumentOutOfRangeException`, redondeo `AwayFromZero`, múltiples ítems, defaults y verificación hash PBKDF2 de autenticación).
- BD `Pepokita`: script `Data/cr01_descuentos_vendedor.sql` aplicado/re-ejecutable idempotentemente; columnas/constraints/índices CR-01 y tabla `usuarios` verificados; `INSERT` con descuento 25% rechazado por `ck_ventas_descuento_rango`.
- Pruebas HTTP (app en ejecución): GETs de Registrar/Historial/Auditoría sin sesión redirigen a login; login `cperez` válido OK; POST registrar con sesión y token válido acepta 0/15/20% y rechaza 25%/-5% con mensaje no punitivo; Auditoría accesible para rol con permiso y restringida para rol sin permiso. Pueden existir ventas adicionales creadas durante pruebas manuales/E2E.
