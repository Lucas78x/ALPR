namespace F.Models
{
    public class ViewModel
    {
        public List<Imagem> AlertasRecentes { get; set; }
        public List<Imagem> ImagemRecentes { get; set; }
        public List<Imagem> FilterImagem { get; set; }
        public List<CameraInfo> Cameras { get; set; }
        public string FilterCam { get; set; }
    }
    public class Imagem
    {
        public string Camera { get; set; }
        public string Modelo { get; set; }
        public string Placa { get; set; }
        public string Cor { get; set; }
        public string Cidade { get; set; }
        public string Municipio { get; set; }
        public string Url { get; set; }
        public DateTime DateTime { get; set; }

        public Imagem(string modelo, string placa, DateTime dateTime, string url, string camera)
        {
            Modelo = modelo;
            Placa = placa;
            DateTime = dateTime;
            Url = url;            Camera = camera;

        }
    }
    public class PlaceAlertsModel
    {
        public Guid Id { get; set; }
        public string Placa { get; set; }
        public DateTime CreateDate { get; set; }

    }
    public class CameraInfo
    {
        public string Camera { get; set; }
        public string RTSP { get; set; }
        public string Status { get; set; }
    }
}
