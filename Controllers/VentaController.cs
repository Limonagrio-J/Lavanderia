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

        private readonly VentaDAO _ventaDao;
        private readonly ClienteDAO _clienteDao;

        public VentaController(VentaDAO ventaDao, ClienteDAO clienteDao)
        {
            _ventaDao = ventaDao;
            _clienteDao = clienteDao;
        }

        public IActionResult Index() => RedirectToAction(nameof(Registrar));

        // Historial de compras por cliente (con filtros por fecha y cliente)
        [HttpGet]
        public IActionResult Historial(DateOnly? desde, DateOnly? hasta, int cliente = 0)
        {
            var modelo = new HistorialViewModel { Desde = desde, Hasta = hasta, IdCliente = cliente };
            try
            {
                modelo.ClientesFiltro = _ventaDao.ObtenerClientesConCompras();
                modelo.Ventas = _ventaDao.ObtenerHistorial(desde, hasta, cliente);
            }
            catch (Exception)
            {
                ViewBag.ErrorBD = MensajeBD;
            }
            return View(modelo);
        }

        // Pantalla de la nueva venta (cabecera + detalle en memoria)
        [HttpGet]
        public IActionResult Registrar()
        {
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
            return View(new Venta()); // Fecha = ahora, Cliente = Consumidor Final
        }

        // Confirmar venta: recibe cliente + ítems (JSON) y guarda todo en una transacción
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Registrar([FromBody] VentaRequest? peticion)
        {
            if (peticion?.Detalles == null || peticion.Detalles.Count == 0)
            {
                return Json(new { ok = false, mensaje = "Agrega al menos un producto antes de confirmar la venta." });
            }

            try
            {
                var venta = new Venta { IdCliente = peticion.IdCliente };

                // Cliente: 0 = Consumidor Final, de lo contrario debe existir y estar activo
                if (peticion.IdCliente != 0)
                {
                    var cliente = _clienteDao.ObtenerPorId(peticion.IdCliente);
                    if (cliente == null)
                    {
                        return Json(new { ok = false, mensaje = "El cliente seleccionado ya no está disponible. Elige otro." });
                    }
                    venta.Cliente = cliente.NombreCompleto;
                }

                // Los precios se toman de la base de datos (no se confía en lo que envía el navegador)
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

                // Validación: la venta debe tener detalles
                if (venta.Detalles.Count == 0)
                {
                    return Json(new { ok = false, mensaje = "Agrega al menos un producto antes de confirmar la venta." });
                }
                if (venta.Total >= 99999999.99m)
                {
                    return Json(new { ok = false, mensaje = "El total de la venta es demasiado grande. Divídela en dos ventas." });
                }

                venta.Fecha = DateTime.Now;
                int idGenerado = _ventaDao.RegistrarVenta(venta);
                string total = venta.Total.ToString("N2", CultureInfo.InvariantCulture);

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
    }
}
