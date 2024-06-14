using F.Models;
using Microsoft.AspNetCore.Mvc;

public class RegistroController : Controller
{
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
    public IActionResult ConfirmarRegistro(RegisterViewModel model)
    {
        if (ModelState.IsValid)
        {
            // Lógica para registrar o usuário
            // Se bem-sucedido, redirecione para a página de login ou outra página apropriada
            return RedirectToAction("Login");
        }
        // Se houver erro, exiba a mensagem de erro
        ViewBag.ErrorMessage = "Erro ao registrar usuário. Verifique os dados e tente novamente.";
        return View(model);
    }
}
