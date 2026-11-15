using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PrototipoCRUD.Data;
using PrototipoCRUD.Models;

namespace PrototipoCRUD.Controllers;

public class HomeController : Controller
{
    private readonly ClienteDAO _dao;

    public HomeController(ClienteDAO dao)
    {
        _dao = dao;
    }

    // Panel principal (Dashboard)
    public IActionResult Index()
    {
        try
        {
            var (activos, nuevosMes) = _dao.Resumen();
            ViewBag.Activos = activos;
            ViewBag.NuevosMes = nuevosMes;
        }
        catch (Exception)
        {
            ViewBag.ErrorBD = "No pudimos conectar con la base de datos. Revisa que PostgreSQL esté encendido y que la cadena de conexión de appsettings.json sea correcta.";
            ViewBag.Activos = 0;
            ViewBag.NuevosMes = 0;
        }
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
