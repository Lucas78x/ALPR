using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using F.SignalR;
using F.Models;

public class BackgroundDataChecker : BackgroundService
{
    private readonly IHubContext<DashboardHub> _hubContext;
    private readonly IWebHostEnvironment _env;
    private readonly IConfiguration _configuration;
    private readonly string _apiPath;
    private readonly string _imgPath;
    private readonly Dictionary<string, DateTime> _lastCheckTimes;

    public BackgroundDataChecker(IHubContext<DashboardHub> hubContext, IWebHostEnvironment env, IConfiguration configuration)
    {
        _hubContext = hubContext;
        _env = env;
        _configuration = configuration;
        _apiPath = Path.Combine(_env.ContentRootPath, "wwwroot", _configuration["Authentication:ApiPath"]);
        _imgPath = _configuration["Authentication:ImgPath"];
        _lastCheckTimes = new Dictionary<string, DateTime>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            bool novosDados = await VerificarNovosDadosAsync();

            if (novosDados)
            {
                await _hubContext.Clients.All.SendAsync("AtualizarDados");
            }

            await Task.Delay(30000, stoppingToken); // Verifica a cada 30 segundos
        }
    }

    private async Task<bool> VerificarNovosDadosAsync()
    {
        if (!File.Exists(_apiPath))
            return false;

        string json = await File.ReadAllTextAsync(_apiPath);
        var cameraList = JsonConvert.DeserializeObject<CameraList>(json);

        bool novosDados = false;

        int mesAtual = DateTime.Now.Month;
        int anoAtual = DateTime.Now.Year;

        var cameraTasks = cameraList.Cameras.Select(async cameraInfo =>
        {
            string folderName = $"{DateTime.Now:MM/yyyy}".Replace("/", string.Empty);
            string pastaImagens = Path.Combine(_env.WebRootPath, _imgPath, cameraInfo.Name, folderName);

            if (!Directory.Exists(pastaImagens))
                Directory.CreateDirectory(pastaImagens);

            var arquivos = Directory.GetFiles(pastaImagens);
            var lastCheckTime = _lastCheckTimes.ContainsKey(pastaImagens) ? _lastCheckTimes[pastaImagens] : DateTime.MinValue;

            var novosArquivos = arquivos.Where(a => File.GetCreationTime(a) > lastCheckTime).ToList();

            if (novosArquivos.Any())
            {
                novosDados = true;
                _lastCheckTimes[pastaImagens] = DateTime.Now;
            }
        });

        await Task.WhenAll(cameraTasks);

        return novosDados;
    }
}
