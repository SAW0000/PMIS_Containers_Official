using Microsoft.AspNetCore.Mvc;
using PMIS.Models;

namespace PMIS.Controllers;

public class HomeController : Controller
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel
        {
            BackUrl = User.Identity?.IsAuthenticated == true ? "/app" : "/login"
        });
    }
}
