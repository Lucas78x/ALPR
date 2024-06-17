using F.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using System.Reflection;
using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using static System.Net.Mime.MediaTypeNames;
using System.Net.Http.Headers;

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
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _cache;
        private static readonly Dictionary<string, string> Cache = new();


        public HomeController(HttpClient httpClient, IWebHostEnvironment webHostEnvironment, IConfiguration configuration, IMemoryCache cache)
        {
            _httpClient = httpClient;
            _webHostEnvironment = webHostEnvironment;
            _configuration = configuration;
            _cache = cache;
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
            var imagensDoMes = new ConcurrentBag<Imagem>();
            var alertasRecentes = new ConcurrentBag<Imagem>();
            var viewModel = new ViewModel();

            using var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };

            using var client = new HttpClient(handler);
            var id = HttpContext.Session.GetInt32("Id") ?? -1;

            try
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _configuration["Authentication:TokenKey"]);
                var response = await client.GetAsync($"https://localhost:7788/api/v1/account/alertsinfo?AccountId={id}");

                if (response.IsSuccessStatusCode)
                {
                    places = await response.Content.ReadFromJsonAsync<List<PlaceAlertsModel>>();
                }

                CameraList cameraList = new();

                string filePath = Path.Combine(_webHostEnvironment.ContentRootPath, "wwwroot", _configuration["Authentication:ApiPath"]);
                if (System.IO.File.Exists(filePath))
                {
                    string json = await System.IO.File.ReadAllTextAsync(filePath);
                    cameraList = Newtonsoft.Json.JsonConvert.DeserializeObject<CameraList>(json);

                    int mesAtual = DateTime.Now.Month;
                    int anoAtual = DateTime.Now.Year;
                    DateTime today = DateTime.Now.Date;

                    var cameraTasks = cameraList.Cameras.Select(cameraInfo =>
                    {
                        return Task.Run(async () =>
                        {
                            string folderName = $"{DateTime.Now:MM/yyyy}".Replace("/", string.Empty);
                            string pastaImagens = Path.Combine(_webHostEnvironment.WebRootPath, _configuration["Authentication:ImgPath"], cameraInfo.Name, folderName);

                            if (!Directory.Exists(pastaImagens))
                                Directory.CreateDirectory(pastaImagens);

                            var arquivos = Directory.GetFiles(pastaImagens);

                            var arquivoTasks = arquivos.Select(async arquivo =>
                            {
                                DateTime dataCriacao = System.IO.File.GetCreationTime(arquivo);

                                if (dataCriacao.Month == mesAtual && dataCriacao.Year == anoAtual)
                                {
                                    string[] partesNomeArquivo = Path.GetFileNameWithoutExtension(arquivo).Split('_');
                                    string placa = partesNomeArquivo[1];
                                    string modelo = GetPlaca(placa);
                                    DateTime dataHora = dataCriacao;
                                    string url = await GetUrlByApi(arquivo);

                                    Imagem imagem = new Imagem(modelo, placa, dataHora, url, cameraInfo.Name);
                                    imagensDoMes.Add(imagem);

                                    var alerta = places?.FirstOrDefault(x => x.Placa == placa);
                                    if (alerta != null && dataHora.Date == today && !alertasRecentes.Any(x => x.Placa == placa && x.DateTime == dataHora))
                                    {
                                        alertasRecentes.Add(imagem);
                                    }
                                }
                            });

                            await Task.WhenAll(arquivoTasks);
                        });
                    });

                    await Task.WhenAll(cameraTasks);
                }

                viewModel.AlertasRecentes = alertasRecentes.ToList();
                viewModel.ImagemRecentes = imagensDoMes.ToList();
                viewModel.Cameras = cameraList;

                SetLastImage(viewModel);

                return View("Dashboard", viewModel);
            }
            catch
            {
                return View("Dashboard", viewModel);
            }
        }

        private static void SetLastImage(ViewModel? viewModel)
        {
            var imagemRecentes = viewModel?.ImagemRecentes;
            DateTime? lastImage = null;
            int lastindex = -1;

            if (imagemRecentes != null && imagemRecentes.Any())
            {
                lastImage = imagemRecentes
                    .OrderByDescending(imagemRecente => imagemRecente.DateTime)
                    .Select(imagemRecente => imagemRecente.DateTime)
                    .FirstOrDefault();

                if (lastImage != null)
                {
                    lastindex = Array.IndexOf(imagemRecentes.Select(imagemRecente => imagemRecente.DateTime).ToArray(), lastImage.Value);
                }
            }

            viewModel.LastIndex = lastindex;
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
                ViewBag.ErrorMessage = "Email invalido.";
                return View("Index");
            }
            else if (string.IsNullOrEmpty(senha))
            {
                ViewBag.ErrorMessage = "Senha invalida.";
                return View("Index");
            }

            Regex regex = new Regex(@"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)$");
            Match match = regex.Match(email);

            if (!match.Success)
            {
                ViewBag.ErrorMessage = "Email invalido.";
                return View("Index");
            }
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };

            var client = new HttpClient(handler);
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _configuration["Authentication:TokenKey"]);
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
                    HttpContext.Session.SetString("Password", senha);
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
                            ViewBag.ErrorMessage = "Email invalido.";
                        }
                        else if (errorCode == (int)ErrorTypeEnum.Password)
                        {
                            ViewBag.ErrorMessage = "Senha invalida.";
                        }
                        else
                        {
                            ViewBag.ErrorMessage = "Usuario ou senha inv�lidos.";
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

                ViewBag.ErrorMessage = "Host nao disponivel.";
                return View("Index");
            }
        }

        [HttpPost]
        [Route("api/ChangeEmail/{email}")]
        public async Task<IActionResult> ChangeEmail(string email)
        {
            Regex regex = new Regex(@"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)$");
            Match match = regex.Match(email);

            if (!match.Success || string.IsNullOrEmpty(email))
            {
                ViewBag.ErrorMessage = "Email invalido.";
                return View("Settings");
            }

            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };

            var client = new HttpClient(handler);
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _configuration["Authentication:TokenKey"]);
            var requestBody = new
            {
                Id = HttpContext.Session.GetInt32("Id"),
                Value = email
            };

            var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            try
            {

                var response = await client.PostAsync("https://localhost:7788/api/v1/account/changeemail", content);
                if (response.IsSuccessStatusCode)
                {
                    HttpContext.Session.SetString("Email", email);
                    ViewBag.Email = email;
                    return Ok();
                }
            }
            catch
            {
                return BadRequest();
            }

            return BadRequest();
        }
        [HttpPost]
        [Route("api/ChangePassword/{password}")]
        public async Task<IActionResult> ChangePassword(string password)
        {

            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };

            var client = new HttpClient(handler);
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _configuration["Authentication:TokenKey"]);
            var requestBody = new
            {
                Id = HttpContext.Session.GetInt32("Id"),
                Value = password
            };

            var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            try
            {

                var response = await client.PostAsync("https://localhost:7788/api/v1/account/changepassword", content);
                if (response.IsSuccessStatusCode)
                {
                    HttpContext.Session.Clear();

                    return RedirectToAction("Index");
                }
            }
            catch
            {
                return BadRequest();
            }

            return BadRequest();
        }
        [HttpGet]
        public async Task<IActionResult> Settings()
        {
            var email = HttpContext.Session.GetString("Email");
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Index");
            }

            ViewBag.Email = email;
            ViewBag.Password = HttpContext.Session.GetString("Password");
            ViewBag.Id = HttpContext.Session.GetInt32("Id");

            return View("Settings");
        }
        public async Task<List<Imagem>> BuscarImagens(string plate, string startDate, string endDate, string camera)
        {
            plate = plate.ToUpper();
            DateTime startDateTime = DateTime.Parse(startDate);
            DateTime endDateTime = DateTime.Parse(endDate);

            var imagens = new List<Imagem>();

            string filePath = Path.Combine(_webHostEnvironment.WebRootPath, _configuration["Authentication:ApiPath"]);
            if (System.IO.File.Exists(filePath))
            {
                string json = await System.IO.File.ReadAllTextAsync(filePath);
                List<CameraInfo> cameraList = Newtonsoft.Json.JsonConvert.DeserializeObject<List<CameraInfo>>(json);

                var cameraInfo = cameraList.FirstOrDefault(x => x.Name == camera);
                if (cameraInfo != null)
                {
                    string folderName = DateTime.Now.ToString("MM/yyyy").Replace("/", string.Empty);
                    var pastaImagens = Path.Combine(_webHostEnvironment.WebRootPath, _configuration["Authentication:ImgPath"], cameraInfo.Name, folderName);

                    if (System.IO.Directory.Exists(pastaImagens))
                    {
                        var arquivos = Directory.GetFiles(pastaImagens)
                                                 .Where(file => file.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || file.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase));

                        foreach (var arquivo in arquivos)
                        {
                            string[] partesNomeArquivo = Path.GetFileNameWithoutExtension(arquivo).Split('_');
                            string placa = partesNomeArquivo[1];
                            DateTime dataHora = System.IO.File.GetCreationTime(arquivo);

                            if (dataHora >= startDateTime && dataHora <= endDateTime && placa == plate)
                            {
                                string url = await GetUrlByApi(arquivo);

                                Imagem imagem = new Imagem(GetPlaca(placa), placa, dataHora, url, cameraInfo.Name);
                                imagens.Add(imagem);
                            }
                        }
                    }
                }
            }

            return imagens;
        }

        [HttpPost]
        public async Task<IActionResult> RedirectSettings()
        {
            var email = HttpContext.Session.GetString("Email");
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Index");
            }

            return RedirectToAction("Settings");
        }

        [HttpPost]
        public IActionResult RedirectAlertas()
        {
            var email = HttpContext.Session.GetString("Email");
            if (!string.IsNullOrEmpty(email))
            {

                return RedirectToAction("Alertas");
            }

            return View("index");
        }

        [HttpGet]
        public async Task<IActionResult> Alertas()

        {
            var email = HttpContext.Session.GetString("Email");
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Index");
            }

            ViewBag.Email = HttpContext.Session.GetString("Email");
            return View("Alertas");
        }
        [HttpGet]
        [Route("api/alertas")]
        public async Task<IActionResult> GetAlertas()
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };

            var client = new HttpClient(handler);
            var id = HttpContext.Session.GetInt32("Id") ?? -1;

            try
            {

                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _configuration["Authentication:TokenKey"]);

                var response = await client.GetAsync($"https://localhost:7788/api/v1/account/alertsinfo?AccountId={id}");

                if (response.IsSuccessStatusCode)
                {
                    var places = await response.Content.ReadFromJsonAsync<List<PlaceAlertsModel>>();

                    return Ok(places.OrderBy(x => x.CreateDate));
                }
            }
            catch
            {
                return BadRequest();
            }

            return BadRequest();
        }
        [HttpPost]
        [Route("api/addalerta")]
        public async Task<IActionResult> AddAlertas([FromBody] AddPlaceAlertsModel alert)
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };

            var client = new HttpClient(handler);
            var id = HttpContext.Session.GetInt32("Id") ?? -1;

            try
            {

                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _configuration["Authentication:TokenKey"]);
                var place = new PlaceAlertsModel(alert.Placa, alert.Motivo);
                var requestBody = new
                {
                    id = place.Id,
                    name = place.Name,
                    placa = place.Placa,
                    createDate = place.CreateDate
                };

                var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");



                var response = await client.PostAsync($"https://localhost:7788/api/v1/account/addalert?AccountId={id}", content);

                if (response.IsSuccessStatusCode)
                {

                    return Ok();
                }
            }
            catch
            {
                return BadRequest();
            }

            return BadRequest();
        }
        [HttpGet]
        public async Task<IActionResult> Placas()
        {
            var email = HttpContext.Session.GetString("Email");
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Index");
            }

            var viewModel = new ViewModel();
            CameraList cameraList = new();

            string filePath = Path.Combine(_webHostEnvironment.WebRootPath, _configuration["Authentication:ApiPath"]);
            if (System.IO.File.Exists(filePath))
            {
                // L� o conte�do do arquivo
                string json = System.IO.File.ReadAllText(filePath);

                // Desserializa o JSON em uma lista de objetos CameraInfo
                cameraList = Newtonsoft.Json.JsonConvert.DeserializeObject<CameraList>(json);

            }
            viewModel.Cameras = cameraList;
            ViewBag.Email = HttpContext.Session.GetString("Email");
            return View("Placas", viewModel);
        }

        [HttpPost]
        public IActionResult RedirectPlacas()
        {
            var email = HttpContext.Session.GetString("Email");
            if (!string.IsNullOrEmpty(email))
            {

                return RedirectToAction("Placas");
            }

            return View("index");
        }

        public string GetPlaca(string placa)
        {

            if (Cache.TryGetValue(placa, out var cachedResult))
            {
                return cachedResult;
            }

            HtmlWeb web = new();
            List<HtmlNode> info = Lista(web.Load($"https://placaipva.com.br/placa/{placa}"));

            if (Verifica(info))
            {
                string result = FormatResult(info);
                Cache[placa] = result;
                return result;
            }
            else
            {
                info = new List<HtmlNode>(Lista(web.Load($"https://www.keplaca.com/placa/{placa}")));
                if (Verifica(info))
                {
                    string result = FormatResult(info);
                    Cache[placa] = result;
                    return result;
                }
                else
                {
                    info = new List<HtmlNode>(Lista(web.Load($"https://placafipe.com/{placa}")));
                    if (Verifica(info))
                    {
                        string result = FormatResult(info);
                        Cache[placa] = result;
                        return result;
                    }
                    else
                    {
                        info = new List<HtmlNode>(Lista(web.Load($"https://keplaca.com/{placa}")));
                        if (Verifica(info))
                        {
                            string result = FormatResult(info);
                            Cache[placa] = result;
                            return result;
                        }
                        else
                        {
                            Cache[placa] = "Desconhecido";
                            return "Desconhecido";
                        }
                    }
                }
            }
        }
        public async Task<string> GetUrlByApi(string url)
        {
            // Check if the URL is already in the cache
            if (_cache.TryGetValue(url, out string cachedUrl))
            {
                return cachedUrl;
            }

            using var client = new HttpClient();

            var uploadUrl = "http://192.0.2.25:8080/upload"; // Altere para o seu endpoint

            using var content = new MultipartFormDataContent();

            // Carregar a imagem e comprimi-la
            using (var imageStream = new FileStream(url, FileMode.Open, FileAccess.Read))
            {
                string imageName = string.Empty;

                string regex = @"[^\\/]+$";


                Match match = Regex.Match(url, regex);

                if (match.Success)
                {
                    imageName = match.Value;
                }
                else
                {
                    return imageName;
                }

                Console.WriteLine(url);
                Console.WriteLine(imageName);

                imageName = imageName.Replace(".jpg", string.Empty);
                imageName = imageName.Replace(".png", string.Empty);
                imageName = imageName.Replace(".JPG", string.Empty);
                imageName = imageName.Replace(".PNG", string.Empty);

                using var image = SixLabors.ImageSharp.Image.Load(imageStream);

                // Comprimir a imagem para um formato mais leve, como WebP
                using var compressedStream = new MemoryStream();
                var encoder = new SixLabors.ImageSharp.Formats.Webp.WebpEncoder();
                image.Mutate(x => x.Resize(image.Width / 2, image.Height / 2)); // Redimensionar a imagem, se necessário
                image.Save(compressedStream, encoder);

                compressedStream.Seek(0, SeekOrigin.Begin);
                using var fileContent = new StreamContent(compressedStream);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/webp");

                // Adicionar a imagem comprimida à requisição
                content.Add(fileContent, "file", $"{imageName}.webp");

                // Enviar a requisição para o servidor
                var response = await client.PostAsync(uploadUrl, content);
                var responseString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = JObject.Parse(responseString);
                    var imageUrl = jsonResponse["url"].ToString().Replace("192.0.2.25", "192.0.2.25:8080");

                    _cache.Set(url, imageUrl, TimeSpan.FromMinutes(60)); // Cache for 60 minutes
                    return imageUrl ?? string.Empty;
                }
                else
                {
                    return string.Empty;
                }
            }
        }
        private string FormatResult(List<HtmlNode> info)
        {
            return $"{info[0]?.InnerText} ({info[1]?.InnerText?.Substring(0, (info[1]?.InnerText?.Length ?? 0) > 8 ? 8 : info[1]?.InnerText?.Length ?? 0)})";
        }
        public static bool Verifica(List<HtmlNode> lista)
            => lista[0] is not null;
        public static List<HtmlNode> Lista(HtmlDocument site)
        {
            List<HtmlNode> info = new();
            for (int i = 2; i <= 28; i += 2)
                info.Add(site.DocumentNode.SelectSingleNode($"(//td)[{i}]"));
            return info;
        }

    }

}




