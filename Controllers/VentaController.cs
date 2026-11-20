using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using PrototipoCRUD.Data;
using PrototipoCRUD.Models;

namespace PrototipoCRUD.Controllers
{
    public class VentaController : Controller
    {
        private const string MensajeBD =
            "No pudimos conectar con la base de datos. Revisa que PostgreSQL esté encendido y que la cadena de conexión de appsettings.json sea correcta.";
        private const string MensajeDescuentoInvalido =
            "El descuento máximo permitido es 20%. Ingresa un valor entre 0% y 20% para continuar.";

        private readonly VentaDAO _ventaDao;
        private readonly ClienteDAO _clienteDao;

        public VentaController(VentaDAO ventaDao, ClienteDAO clienteDao)
        {
            _ventaDao = ventaDao;
            _clienteDao = clienteDao;
        }

        public IActionResult Index() => RedirectToAction(nameof(Registrar));

        [HttpGet]
        public IActionResult Registrar()
        {
            var sesion = ObtenerSesion();
            if (sesion == null) return RedirectToAction("Login", "Account", new { returnUrl = "/Venta/Registrar" });

            var productos = new List<Producto>();
            var clientes = new List<Cliente>();
            try
            {
                productos = _ventaDao.ObtenerProductos();
                clientes = _clienteDao.ObtenerTodos();
            }
            catch (Exception)
            {
                ViewBag.ErrorBD = MensajeBD;
            }
            ViewBag.Productos = productos;
            ViewBag.Clientes = clientes;
            ViewBag.VendedorNombre = sesion.NombreCompleto;
            ViewBag.VendedorUsuario = sesion.NombreUsuario;
            return View(new Venta());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Registrar([FromBody] VentaRequest? peticion)
        {
            var sesion = ObtenerSesion();
            if (sesion == null)
            {
                return Unauthorized(new { ok = false, mensaje = "Tu sesión expiró. Inicia sesión para continuar." });
            }

            if (peticion?.Detalles == null || peticion.Detalles.Count == 0)
            {
                return Json(new { ok = false, mensaje = "Agrega al menos un producto antes de confirmar la venta." });
            }

            try
            {
                var venta = new Venta
                {
                    IdCliente = peticion.IdCliente,
                    IdVendedor = sesion.EmpleadoId,
                    Vendedor = sesion.NombreCompleto
                };

                if (peticion.IdCliente != 0)
                {
                    var cliente = _clienteDao.ObtenerPorId(peticion.IdCliente);
                    if (cliente == null)
                    {
                        return Json(new { ok = false, mensaje = "El cliente seleccionado ya no está disponible. Elige otro." });
                    }
                }

                var catalogo = _ventaDao.ObtenerProductos().ToDictionary(p => p.Id);
                foreach (var item in peticion.Detalles)
                {
                    if (item.Cantidad < 1 || item.Cantidad > 9999)
                    {
                        return Json(new { ok = false, mensaje = "Cada cantidad debe estar entre 1 y 9999." });
                    }
                    if (!catalogo.TryGetValue(item.IdProducto, out var producto))
                    {
                        return Json(new { ok = false, mensaje = "Uno de los productos ya no está disponible. Quítalo de la lista." });
                    }

                    var existente = venta.Detalles.FirstOrDefault(d => d.IdProducto == producto.Id);
                    if (existente != null)
                    {
                        existente.Cantidad += item.Cantidad;
                    }
                    else
                    {
                        venta.Detalles.Add(new DetalleVenta
                        {
                            IdProducto = producto.Id,
                            NombreProducto = producto.Nombre,
                            Cantidad = item.Cantidad,
                            PrecioUnitario = producto.Precio
                        });
                    }
                }

                if (venta.Detalles.Count == 0)
                {
                    return Json(new { ok = false, mensaje = "Agrega al menos un producto antes de confirmar la venta." });
                }

                try
                {
                    venta.AplicarDescuento(peticion.PorcentajeDescuento);
                }
                catch (ArgumentOutOfRangeException)
                {
                    return Json(new { ok = false, mensaje = MensajeDescuentoInvalido });
                }

                if (venta.TotalFinal >= 99999999.99m)
                {
                    return Json(new { ok = false, mensaje = "El total de la venta es demasiado grande. Divídela en dos ventas." });
                }

                venta.Fecha = DateTime.Now;
                int idGenerado = _ventaDao.RegistrarVenta(venta);
                string total = venta.TotalFinal.ToString("N2", CultureInfo.InvariantCulture);

                return Json(new
                {
                    ok = true,
                    id = idGenerado,
                    total,
                    mensaje = $"¡Venta #{idGenerado} registrada con éxito por un total de Bs. {total}!"
                });
            }
            catch (Exception)
            {
                return Json(new { ok = false, mensaje = "No se pudo registrar la venta; no se guardó nada. " + MensajeBD });
            }
        }

        [HttpGet]
        public IActionResult Historial()
        {
            var sesion = ObtenerSesion();
            if (sesion == null) return RedirectToAction("Login", "Account", new { returnUrl = "/Venta/Historial" });
            try
            {
                var historial = _ventaDao.ObtenerHistorial();
                return View(historial);
            }
            catch (Exception)
            {
                ViewBag.ErrorBD = MensajeBD;
                return View(new List<VentaHistorial>());
            }
        }

        [HttpGet]
        public IActionResult Auditoria()
        {
            var sesion = ObtenerSesion();
            if (sesion == null) return RedirectToAction("Login", "Account", new { returnUrl = "/Venta/Auditoria" });
            if (!PuedeVerAuditoria(sesion.Rol))
            {
                return RedirectToAction("Login", "Account");
            }
            try
            {
                var resumen = _ventaDao.ObtenerAuditoriaDescuentos();
                return View(resumen);
            }
            catch (Exception)
            {
                ViewBag.ErrorBD = MensajeBD;
                return View(new List<ResumenDescuentoVendedor>());
            }
        }

        private UsuarioSesion? ObtenerSesion()
        {
            var nombre = HttpContext.Session.GetString("UsuarioNombre");
            if (string.IsNullOrWhiteSpace(nombre)) return null;
            return new UsuarioSesion
            {
                Id = HttpContext.Session.GetInt32("UsuarioId") ?? 0,
                EmpleadoId = HttpContext.Session.GetInt32("EmpleadoId"),
                NombreUsuario = nombre,
                NombreCompleto = HttpContext.Session.GetString("NombreCompleto") ?? nombre,
                Rol = HttpContext.Session.GetString("Rol") ?? string.Empty
            };
        }

        private static bool PuedeVerAuditoria(string rol) => rol == "Administrador" || rol == "Gerente";

        private class UsuarioSesion
        {
            public int Id { get; set; }
            public int? EmpleadoId { get; set; }
            public string NombreUsuario { get; set; } = string.Empty;
            public string NombreCompleto { get; set; } = string.Empty;
            public string Rol { get; set; } = string.Empty;
        }
    }
}
