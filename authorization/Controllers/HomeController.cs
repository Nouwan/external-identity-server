using Microsoft.AspNetCore.Mvc;

namespace authorization.Controllers
{
    public class HomeController:Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
