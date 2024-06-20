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
        return View("Registro");
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

                    return RedirectToAction("Home","Index");
                }
                else
                {
                    ViewBag.ErrorMessage = "Erro ao registrar usuário. Verifique os dados e tente novamente.";
                    return View(model);
                }

            }
            catch
            {
                ViewBag.ErrorMessage = "Erro ao registrar usuário. Verifique os dados e tente novamente.";
                return View(model);
            }

        }

        ViewBag.ErrorMessage = "Erro ao registrar usuário. Verifique os dados e tente novamente.";
        return View(model);
    }
}
