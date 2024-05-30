using F.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using RtspClientSharp;


namespace F.Controllers
{
    public class CamerasController : Controller
    {
        private List<CameraInfo> cameras = new List<CameraInfo>();
        private int editingIndex = -1;


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
                cameras = Newtonsoft.Json.JsonConvert.DeserializeObject<List<CameraInfo>>(json);
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

                 var isCameraOnline =  CheckRTSPStatus(rtsp.Value).Result;

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
            var connectionParameters = new ConnectionParameters(new Uri(url));
            var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;
            try
            {
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
    }

    public class RTSPReceive
    {
        public string Value { get; set; }
    }
}


