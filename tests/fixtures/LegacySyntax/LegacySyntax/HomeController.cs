using System.Web.Mvc; // MIG3010 — ASP.NET MVC 5

namespace LegacySyntax
{
    public class HomeController : Controller
    {
        public ActionResult Index() => View();
    }
}
