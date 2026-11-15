using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using PrototipoCRUD.Data;
using PrototipoCRUD.Models;

namespace PrototipoCRUD.Controllers
{
    public class ClienteController : Controller
    {
        private static readonly string[] TiposValidos = { "Particular", "Frecuente", "Empresa" };
        private const string MensajeBD =
            "No pudimos conectar con la base de datos. Revisa que PostgreSQL esté encendido y que la cadena de conexión de appsettings.json sea correcta.";

        private readonly ClienteDAO _dao;

        public ClienteController(ClienteDAO dao)
        {
            _dao = dao;
        }

        // READ: lista + diálogo modal Nuevo / Editar
        public IActionResult Index()
        {
            var lista = new List<Cliente>();
            try
            {
                lista = _dao.ObtenerTodos();
            }
            catch (Exception)
            {
                ViewBag.ErrorBD = MensajeBD;
            }
            return View(lista);
        }

        // CREATE / UPDATE: lo llama el diálogo (fetch) y responde JSON
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Guardar([FromForm] ClienteForm form)
        {
            Dictionary<string, string> errores;
            DateOnly fecha;
            try
            {
                errores = Validar(form, out fecha);
            }
            catch (Exception)
            {
                return Json(new { ok = false, errores = new Dictionary<string, string>(), general = MensajeBD });
            }

            if (errores.Count > 0)
            {
                return Json(new { ok = false, errores });
            }

            var cliente = new Cliente
            {
                Id = form.Id,
                Nombre = form.Nombre!.Trim(),
                Apellido = form.Apellido!.Trim(),
                Documento = form.Documento!.Trim(),
                Telefono = form.Telefono!.Trim(),
                Correo = form.Correo?.Trim(),
                Direccion = form.Direccion?.Trim(),
                TipoCliente = form.TipoCliente!,
                FechaRegistro = fecha
            };

            try
            {
                if (cliente.Id == 0)
                {
                    _dao.Insertar(cliente);
                    TempData["Mensaje"] = $"✔ Cliente «{cliente.NombreCompleto}» guardado correctamente.";
                }
                else
                {
                    _dao.Actualizar(cliente);
                    TempData["Mensaje"] = $"✔ Los datos de «{cliente.NombreCompleto}» se actualizaron.";
                }
            }
            catch (Exception)
            {
                return Json(new { ok = false, errores = new Dictionary<string, string>(), general = MensajeBD });
            }

            return Json(new { ok = true });
        }

        // DELETE: baja lógica (la confirmación se pide antes, en el diálogo)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DarDeBaja(int id)
        {
            try
            {
                _dao.Eliminar(id);
                TempData["Mensaje"] = "El cliente fue dado de baja.";
            }
            catch (Exception)
            {
                TempData["Error"] = MensajeBD;
            }
            return RedirectToAction(nameof(Index));
        }

        // ---------- Validación: mismos mensajes amables que el diálogo en el navegador ----------
        private Dictionary<string, string> Validar(ClienteForm f, out DateOnly fecha)
        {
            var e = new Dictionary<string, string>();
            fecha = default;

            f.Nombre = (f.Nombre ?? "").Trim();
            f.Apellido = (f.Apellido ?? "").Trim();
            f.Documento = (f.Documento ?? "").Trim();
            f.Telefono = (f.Telefono ?? "").Trim();
            f.Correo = (f.Correo ?? "").Trim();
            f.Direccion = (f.Direccion ?? "").Trim();
            f.TipoCliente = (f.TipoCliente ?? "").Trim();

            // Nombre y apellido
            if (f.Nombre.Length == 0) e["Nombre"] = "Escribe el nombre del cliente. Ej.: María.";
            else if (f.Nombre.Length < 2) e["Nombre"] = "El nombre es muy corto. Escríbelo completo, por favor.";
            else if (f.Nombre.Any(char.IsDigit)) e["Nombre"] = "El nombre no debería llevar números. Revisa cómo lo escribiste.";
            else if (f.Nombre.Length > 80) e["Nombre"] = "El nombre es muy largo (máximo 80 letras).";

            if (f.Apellido.Length == 0) e["Apellido"] = "Escribe el apellido del cliente. Ej.: Quispe.";
            else if (f.Apellido.Length < 2) e["Apellido"] = "El apellido es muy corto. Escríbelo completo, por favor.";
            else if (f.Apellido.Any(char.IsDigit)) e["Apellido"] = "El apellido no debería llevar números. Revisa cómo lo escribiste.";
            else if (f.Apellido.Length > 80) e["Apellido"] = "El apellido es muy largo (máximo 80 letras).";

            // Documento
            if (f.Documento.Length == 0)
                e["Documento"] = "Escribe el documento de identidad. Ej.: 10658776.";
            else if (!Regex.IsMatch(f.Documento, @"^[\w.\-]{5,20}$"))
                e["Documento"] = "Usa de 5 a 20 caracteres (números o letras, sin espacios). Ej.: 10658776.";
            else if (_dao.ExisteDocumento(f.Documento, f.Id))
                e["Documento"] = "Ya existe un cliente con este documento. Búscalo en la lista para no duplicarlo.";

            // Teléfono
            if (f.Telefono.Length == 0)
                e["Telefono"] = "Necesitamos un teléfono para avisar cuando la ropa esté lista. Ej.: 78445511.";
            else if (!Regex.IsMatch(f.Telefono, @"^\+?[\d\s\-]+$"))
                e["Telefono"] = "Usa solo números (puedes separar con espacios o guiones). Ej.: 784 455 11.";
            else
            {
                var digitos = f.Telefono.Count(char.IsDigit);
                if (digitos < 7 || digitos > 15)
                    e["Telefono"] = "El teléfono debería tener entre 7 y 15 dígitos. Revisa si falta o sobra alguno.";
                else if (f.Telefono.Length > 20)
                    e["Telefono"] = "El teléfono es muy largo. Escríbelo sin espacios de más.";
            }

            // Correo (opcional)
            if (f.Correo.Length > 0 && !Regex.IsMatch(f.Correo, @"^[^\s@]+@[^\s@]+\.[^\s@]+$"))
                e["Correo"] = "Parece que falta algo en el correo. Ej.: nombre@correo.com (o déjalo vacío).";
            else if (f.Correo.Length > 120)
                e["Correo"] = "El correo es muy largo (máximo 120 caracteres).";

            // Tipo de cliente
            if (!TiposValidos.Contains(f.TipoCliente))
                e["TipoCliente"] = "Elige un tipo de cliente de la lista.";

            // Fecha de registro
            if (!DateOnly.TryParseExact(f.FechaRegistro, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out fecha))
                e["FechaRegistro"] = "Elige la fecha de registro (por defecto es hoy).";
            else if (fecha > DateOnly.FromDateTime(DateTime.Today))
                e["FechaRegistro"] = "La fecha no puede ser futura. Elige hoy o un día anterior.";

            return e;
        }
    }
}
