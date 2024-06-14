using F.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using RtspClientSharp;
using System.Text.Json;


namespace F.Controllers
{
    public class CamerasController : Controller
    {
        private CameraList cameras = new ();
        private int editingIndex = -1;

        private readonly HttpClient _httpClient;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public CamerasController(HttpClient httpClient, IWebHostEnvironment webHostEnvironment)
        {
            _httpClient = httpClient;
            _webHostEnvironment = webHostEnvironment;
        }

        [HttpGet]
        public async Task<IActionResult> Cameras()
        {
            var email = HttpContext.Session.GetString("Email");
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Index");
            }

            string filePath = @"C:\Users\CFTV\Documents\Cameras\cameras.json";


            ViewModel viewModel = new();
            if (System.IO.File.Exists(filePath))
            {
                // Lê o conteúdo do arquivo
                string json = System.IO.File.ReadAllText(filePath);

                // Desserializa o JSON em uma lista de objetos CameraInfo
                cameras  = Newtonsoft.Json.JsonConvert.DeserializeObject<CameraList>(json);

                viewModel.Cameras = cameras;
            }
            return View("Cameras", viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> RedirectAlpr()
        {
            var email = HttpContext.Session.GetString("Email");
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Index");
            }

            return RedirectToAction("Cameras");
        }


        public IActionResult ReceiveNumber([FromBody] RTSPReceive rtsp)
        {

            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };

            var client = new HttpClient(handler);
            try
            {

                var isCameraOnline = CheckRTSPStatus(rtsp.Value).Result;

                if (isCameraOnline)
                {
                    isCameraOnline = true;
                    // Retorna o status da câmera como JSON
                    return Json(new { success = true, status = isCameraOnline });
                }
                else
                {
                    return Json(new { success = true, status = isCameraOnline });
                }
            }
            catch
            {
                return Json(new { success = true, status = false });
            }
        }

        public async Task<bool> CheckRTSPStatus(string url)
        {
            try
            {
                var connectionParameters = new ConnectionParameters(new Uri(url));
                var cancellationTokenSource = new CancellationTokenSource();
                var cancellationToken = cancellationTokenSource.Token;

                using (var rtspClient = new RtspClient(connectionParameters))
                {

                    rtspClient.FrameReceived += (sender, e) =>
                    {
                    };

                    await rtspClient.ConnectAsync(cancellationToken);
                    rtspClient.Dispose();

                    return true;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        [HttpGet]
        [Route("api/cameras")]
        public async Task<IActionResult> GetCameras()
        {
            string filePath = Path.Combine(_webHostEnvironment.WebRootPath, "Cameras", "cameras.json");

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound("Cameras file not found.");
            }

            string jsonData = await System.IO.File.ReadAllTextAsync(filePath);
            var cameras = Newtonsoft.Json.JsonConvert.DeserializeObject<List<CameraInfo>>(jsonData);

            return Ok(cameras);
        }

        [HttpPost]
        public async Task<IActionResult> AddCamera([FromBody] CameraInfo camera)
        {
            string filePath = Path.Combine(_webHostEnvironment.WebRootPath, "Cameras", "cameras.json");

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound("Cameras file not found.");
            }

            string jsonData = await System.IO.File.ReadAllTextAsync(filePath);
            var cameras = Newtonsoft.Json.JsonConvert.DeserializeObject<List<CameraInfo>>(jsonData);
            if (!cameras.Any(x => x.Name.Contains(camera.Name)))
            {
                cameras.Add(camera);
            }
            else
            {
                return BadRequest();

            }


            jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(cameras, Newtonsoft.Json.Formatting.Indented);
            await System.IO.File.WriteAllTextAsync(filePath, jsonData);

            return Ok(camera);
        }

        [HttpPut]
        [Route("api/cameras/{index}")]
        public async Task<IActionResult> UpdateCamera(int index, [FromBody] CameraInfo camera)
        {
            string filePath = Path.Combine(_webHostEnvironment.WebRootPath, "Cameras", "cameras.json");

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound("Cameras file not found.");
            }

            string jsonData = await System.IO.File.ReadAllTextAsync(filePath);
            var cameras = Newtonsoft.Json.JsonConvert.DeserializeObject<List<CameraInfo>>(jsonData);

            if (index < 0 || index >= cameras.Count)
            {
                return BadRequest("Invalid camera index.");
            }

            cameras[index] = camera;

            jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(cameras, Newtonsoft.Json.Formatting.Indented);
            await System.IO.File.WriteAllTextAsync(filePath, jsonData);

            return Ok(camera);
        }

        [HttpDelete]
        [Route("api/cameras/{index}")]
        public async Task<IActionResult> DeleteCamera(int index)
        {
            string filePath = Path.Combine(_webHostEnvironment.WebRootPath, "Cameras", "cameras.json");

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound("Cameras file not found.");
            }

            string jsonData = await System.IO.File.ReadAllTextAsync(filePath);
            var cameras = Newtonsoft.Json.JsonConvert.DeserializeObject<List<CameraInfo>>(jsonData);

            if (index < 0 || index >= cameras.Count)
            {
                return BadRequest("Invalid camera index.");
            }

            cameras.RemoveAt(index);

            jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(cameras, Newtonsoft.Json.Formatting.Indented);
            await System.IO.File.WriteAllTextAsync(filePath, jsonData);

            return Ok();
        }
    }
}

public class RTSPReceive
{
    public string Value { get; set; }
}





