using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace F.Controllers
{
    public class DashboardController : Controller
    {

        public DashboardController(HttpClient httpClient)
        {
        }

        [HttpGet]
        public IActionResult Dashboard()
        {
            return View();
        }
    }
}