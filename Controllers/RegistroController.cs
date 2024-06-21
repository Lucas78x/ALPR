using F.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Net.Http;
using System.Text;

public class RegistroController : Controller
{
    private readonly IConfiguration _configuration;
    public RegistroController(IConfiguration configuration)
    {
       _configuration = configuration;
    }

    [HttpGet]
    public IActionResult Registro()
    {
        string clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();

        if (clientIp != null && clientIp.Contains("172.30.2"))
        {      
                return View("Registro");
        }
        else
        {
            return RedirectToAction("Index", "Home");
        }
    }

    [HttpPost]
    public IActionResult RedirectRegistro()
    {
        return RedirectToAction("Registro");
    }
    [HttpPost]
    public async Task<IActionResult> ConfirmarRegistro(RegisterViewModel model)
    {
        if (ModelState.IsValid)
        {
            var account = new AccountModel(model.RegistroType, model.Registro, model.Nome, model.Email, model.Senha);

            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };

            var client = new HttpClient(handler);

            try
            {

                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _configuration["Authentication:TokenKey"]);
                var requestBody = new
                {
                    id = account.Id,
                    alerts = account.Alerts,
                    type = account.Type,
                    createDate = account.CreateDate,    
                    registro = account.Registro,
                    username = account.Username,
                
                    email = account.Email,
                    password = account.Password
                };

                var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");



                var response = await client.PostAsync($"https://localhost:7788/api/v1/account/createaccount", content);

                if (response.IsSuccessStatusCode)
                {

                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    ViewBag.ErrorMessage = $"Erro ao registrar usuario. Email ou CPNJ/CPF em uso para {account.Username}.";
                    return View("Registro", "Registro");
                }

            }
            catch
            {
                ViewBag.ErrorMessage = "Erro ao registrar usuario. Verifique os dados e tente novamente.";
                return View("Registro", "Registro");
            }

        }

        ViewBag.ErrorMessage = "Erro ao registrar usuario. Verifique os dados e tente novamente.";
        return View("Registro", "Registro");
    }
}
