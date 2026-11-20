using Microsoft.AspNetCore.Mvc;
using PrototipoCRUD.Data;
using PrototipoCRUD.Services;

namespace PrototipoCRUD.Controllers
{
    public class AccountController : Controller
    {
        private readonly UsuarioDAO _usuarioDao;

        public AccountController(UsuarioDAO usuarioDao)
        {
            _usuarioDao = usuarioDao;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (HttpContext.Session.GetString("UsuarioNombre") != null)
                return RedirectToAction("Index", "Home");
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(string nombreUsuario, string contrasena, string? returnUrl = null)
        {
            var servicio = new ServicioAutenticacion(_usuarioDao);
            var (resultado, usuario) = servicio.Autenticar(nombreUsuario?.Trim() ?? string.Empty, contrasena ?? string.Empty);

            switch (resultado)
            {
                case ResultadoLogin.Exito:
                    HttpContext.Session.SetInt32("UsuarioId", usuario!.Id);
                    HttpContext.Session.SetInt32("EmpleadoId", usuario.EmpleadoId);
                    HttpContext.Session.SetString("UsuarioNombre", usuario.NombreUsuario);
                    HttpContext.Session.SetString("NombreCompleto", usuario.NombreCompleto);
                    HttpContext.Session.SetString("Rol", usuario.Rol);
                    if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                        return Redirect(returnUrl);
                    return RedirectToAction("Index", "Home");
                case ResultadoLogin.Bloqueado:
                    ViewBag.Error = "Tu cuenta está bloqueada por intentos fallidos. Contacta al administrador.";
                    break;
                case ResultadoLogin.Inactivo:
                    ViewBag.Error = "Tu cuenta está inactiva. Contacta al administrador.";
                    break;
                default:
                    ViewBag.Error = "Usuario o contraseña incorrectos. Inténtalo de nuevo.";
                    break;
            }

            ViewBag.ReturnUrl = returnUrl;
            ViewBag.NombreUsuario = nombreUsuario;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}
