using Microsoft.AspNetCore.Mvc;

namespace Negocio.Web.Controllers
{
    public class HomeAdminController : Controller
    {
        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("ROL") != "ADMINISTRADOR")
                return RedirectToAction("Index", "Login");

            return View();
        }
    }
}
