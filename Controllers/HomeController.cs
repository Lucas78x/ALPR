using F.Models;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace F.Controllers
{
    public enum ErrorTypeEnum
    {
        Email = 0,
        Password = 1,
        NotFound = 2,
    }
    public class HomeController : Controller
    {
        private readonly HttpClient _httpClient;

        public HomeController(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var email = HttpContext.Session.GetString("Email");
            if (!string.IsNullOrEmpty(email))
            {

                return RedirectToAction("Dashboard");
            }

            return View("index");
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var email = HttpContext.Session.GetString("Email");
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Index");
            }

            ViewBag.Email = email;

            var places = new List<PlaceAlertsModel>();
            List<Imagem> imagensDoMes = new List<Imagem>();
            List<Imagem> Alertarecentes = new List<Imagem>();
            var viewModel = new ViewModel();

            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };

            var client = new HttpClient(handler);
            var id = HttpContext.Session.GetInt32("Id") ?? -1;

            try
            {

                var response = await client.GetAsync($"https://localhost:7788/api/v1/account/alertsinfo?AccountId={id}");

                if (response.IsSuccessStatusCode)
                {
                    places = await response.Content.ReadFromJsonAsync<List<PlaceAlertsModel>>();
                }
            }
            catch
            {

            }

            string filePath = @"C:\Users\CFTV\Documents\Cameras\cameras.json";

            List<CameraInfo> cameraList = new();

            if (System.IO.File.Exists(filePath))
            {
                // Lê o conteúdo do arquivo
                string json = System.IO.File.ReadAllText(filePath);

                // Desserializa o JSON em uma lista de objetos CameraInfo
                cameraList = Newtonsoft.Json.JsonConvert.DeserializeObject<List<CameraInfo>>(json);


                // Itera sobre a lista de objetos CameraInfo para obter os nomes das câmeras
                foreach (var cameraInfo in cameraList)
                {

                    string pastaImagens = @$"C:\Users\CFTV\Documents\Cameras\{cameraInfo.Camera}\{DateTime.Now.ToString("MM/yyyy").Replace("/", string.Empty)}";

                    if (System.IO.File.Exists(pastaImagens))
                        continue;

                    // Obter o mês e o ano atual
                    int mesAtual = DateTime.Now.Month;
                    int anoAtual = DateTime.Now.Year;


                    // Listar todos os arquivos na pasta de imagens
                    string[] arquivos = Directory.GetFiles(pastaImagens);

                    // Filtrar os arquivos pelo mês e ano atual
                    foreach (string arquivo in arquivos)
                    {
                        DateTime dataCriacao = System.IO.File.GetCreationTime(arquivo);

                        if (dataCriacao.Month == mesAtual && dataCriacao.Year == anoAtual)
                        {
                            // Extrair informações do nome do arquivo (exemplo: Modelo_Placa_2024-05-25.jpg)
                            string[] partesNomeArquivo = Path.GetFileNameWithoutExtension(arquivo).Split('_');
                            string modelo = partesNomeArquivo[0];
                            string placa = partesNomeArquivo[1];
                            DateTime dataHora = System.IO.File.GetCreationTime(arquivo);
                            string url = await GetUrlByApi(arquivo);

                            // Criar objeto Imagem e adicionar à lista
                            Imagem imagem = new Imagem(modelo, placa, dataHora, url, cameraInfo.Camera);
                            imagensDoMes.Add(imagem);

                            var alerta = places?.FirstOrDefault(x => x.Placa == placa);
                            if (alerta != null)
                            {
                                if (Alertarecentes.FirstOrDefault(x => x.Placa == placa && x.DateTime == dataHora) == null)
                                {
                                    if (imagem.DateTime.Date == DateTime.Now.Date)
                                    {
                                        Alertarecentes.Add(imagem);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            viewModel.AlertasRecentes = Alertarecentes;
            viewModel.ImagemRecentes = imagensDoMes;
            viewModel.Cameras = cameraList;

            return View("Dashboard", viewModel);
        }

        public IActionResult FilterByCamera(string camera, string ImagemRecentes)
        {
            List<Imagem> imagems = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Imagem>>(ImagemRecentes);

            var imagensFiltradas = string.IsNullOrEmpty(camera)
            ? imagems
            : imagems.Where(i => i.Camera == camera).ToList();

            // Renderize a partial view com a lista filtrada
            return PartialView("_PlacasRecentesPartial", imagensFiltradas);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string senha)
        {
            if (string.IsNullOrEmpty(email))
            {
                ViewBag.ErrorMessage = "Email inválido.";
                return View("Index");
            }
            else if (string.IsNullOrEmpty(senha))
            {
                ViewBag.ErrorMessage = "Senha inválida.";
                return View("Index");
            }

            Regex regex = new Regex(@"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)$");
            Match match = regex.Match(email);

            if (!match.Success)
            {
                ViewBag.ErrorMessage = "Email inválido.";
                return View("Index");
            }
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };

            var client = new HttpClient(handler);

            var requestBody = new
            {
                Email = email,
                Password = senha
            };

            var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            try
            {

                var response = await client.PostAsync("https://localhost:7788/api/v1/account/accountinfo", content);

                if (response.IsSuccessStatusCode)
                {
                    HttpContext.Session.SetString("Email", email);
                    var id = await response.Content.ReadAsStringAsync();
                    HttpContext.Session.SetInt32("Id", int.Parse(id));
                    return RedirectToAction("Dashboard");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    if (response.StatusCode == HttpStatusCode.NotFound && int.TryParse(errorContent, out int errorCode))
                    {
                        if (errorCode == (int)ErrorTypeEnum.Email)
                        {
                            ViewBag.ErrorMessage = "Email inválido.";
                        }
                        else if (errorCode == (int)ErrorTypeEnum.Password)
                        {
                            ViewBag.ErrorMessage = "Senha inválida.";
                        }
                        else
                        {
                            ViewBag.ErrorMessage = "Usuário ou senha inválidos.";
                        }
                    }
                    else
                    {
                        ViewBag.ErrorMessage = "Erro inesperado.";
                    }

                    return View("Index");
                }

            }
            catch (Exception ex)
            {

                ViewBag.ErrorMessage = "Host não disponivel.";
                return View("Index");
            }
        }

        static async Task<string> GetUrlByApi(string url)
        {
            string apiKey = "54d227aed6d1bbbf4bb3984192b05837"; // Substitua com sua API key
            string imagePath = url;

            byte[] imageData = System.IO.File.ReadAllBytes(imagePath);

            using (var client = new HttpClient())
            {
                using (var formData = new MultipartFormDataContent())
                {
                    formData.Add(new StringContent(apiKey), "key");
                    formData.Add(new ByteArrayContent(imageData, 0, imageData.Length), "image", "imagem.jpg");

                    HttpResponseMessage response = await client.PostAsync("https://api.imgbb.com/1/upload", formData);

                    if (response.IsSuccessStatusCode)
                    {
                        string apiResponse = await response.Content.ReadAsStringAsync();
                        JObject jsonResponse = JObject.Parse(apiResponse);
                        return jsonResponse["data"]["url"].ToString();
                    }
                    else
                    {
                        return string.Empty;
                    }
                }
            }
        }
    }

}




