using Microsoft.AspNetCore.Mvc;
using PrototipoCRUD.Data;
using PrototipoCRUD.Models;

namespace PrototipoCRUD.Controllers
{
    public class ClienteController : Controller
    {
        private readonly ClienteDAO _dao;

        public ClienteController(ClienteDAO dao)
        {
            _dao = dao;
        }

        // READ: lista
        public IActionResult Index()
        {
            return View(_dao.ObtenerTodos());
        }

        // CREATE: formulario
        public IActionResult Create()
        {
            return View();
        }

        // CREATE: guardar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Cliente cliente)
        {
            if (!string.IsNullOrWhiteSpace(cliente.Documento) && _dao.ExisteDocumento(cliente.Documento))
            {
                ModelState.AddModelError(nameof(Cliente.Documento), "Ya existe un cliente con ese documento.");
            }
            if (!ModelState.IsValid)
            {
                return View(cliente);
            }

            _dao.Insertar(cliente);
            TempData["Mensaje"] = "Cliente registrado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // UPDATE: formulario
        public IActionResult Edit(int id)
        {
            var cliente = _dao.ObtenerPorId(id);
            if (cliente == null) return NotFound();
            return View(cliente);
        }

        // UPDATE: guardar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Cliente cliente)
        {
            if (!string.IsNullOrWhiteSpace(cliente.Documento) && _dao.ExisteDocumento(cliente.Documento, cliente.Id))
            {
                ModelState.AddModelError(nameof(Cliente.Documento), "Ya existe otro cliente con ese documento.");
            }
            if (!ModelState.IsValid)
            {
                return View(cliente);
            }

            _dao.Actualizar(cliente);
            TempData["Mensaje"] = "Datos del cliente actualizados.";
            return RedirectToAction(nameof(Index));
        }

        // DELETE: confirmación
        public IActionResult Delete(int id)
        {
            var cliente = _dao.ObtenerPorId(id);
            if (cliente == null) return NotFound();
            return View(cliente);
        }

        // DELETE: baja lógica
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            _dao.Eliminar(id);
            TempData["Mensaje"] = "Cliente dado de baja.";
            return RedirectToAction(nameof(Index));
        }
    }
}
