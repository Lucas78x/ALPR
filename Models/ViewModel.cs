using F.Enum;
using System.ComponentModel.DataAnnotations;

namespace F.Models
{
    public class ViewModel
    {
        public List<Imagem> AlertasRecentes { get; set; }
        public List<Imagem> ImagemRecentes { get; set; }
        public List<Imagem> FilterImagem { get; set; }
        public CameraList Cameras { get; set; }
        public string FilterCam { get; set; }
        public int alertsOld { get; set; }
        public int LastIndex { get; set; }
        public string formattedDate { get; set; }
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
            Url = url;
            Camera = camera;

        }
    }
    public class PlaceAlertsModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Placa { get; set; }
        public DateTime CreateDate { get; set; }

        public PlaceAlertsModel(string name, string placa)
        {
            Id = Guid.NewGuid();
            Name = name;
            Placa = placa;
            CreateDate = DateTime.Now;
        }
    }
    public class RegisterViewModel
    {
        [Required]
        [Display(Name = "Nome")]
        public string Nome { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [CpfCnpj]  // Adicione o atributo customizado aqui
        [Display(Name = "Registro")]
        public string Registro { get; set; }

        public CadastroTypeEnum RegistroType { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Senha")]
        public string Senha { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Confirmar Senha")]
        [Compare("Senha", ErrorMessage = "As senhas nao coincidem.")]
        public string ConfirmarSenha { get; set; }
    }


    public class AccountModel

    {    /// <summary>
         /// Unique Identifier
         /// </summary>
        public long Id { get; set; }

        public List<PlaceAlertsModel> Alerts { get; set; }

        /// <summary>
        /// Enumeração de Tipo de Cadastro.
        /// </summary>
        public CadastroTypeEnum Type { get; set; }

        public DateTime CreateDate { get; set; }

        /// <summary>
        /// Registro de Cadastro.
        /// </summary>
        /// 
        public string Registro { get; set; }

        /// <summary>
        /// Username.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Username.
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Password.
        /// </summary>
        public string Password { get; set; }

        public AccountModel()
        {

        }

        public AccountModel(CadastroTypeEnum type, string registro, string username, string email, string password)
        {
            Alerts = new List<PlaceAlertsModel>();
            Type = type;
            CreateDate = DateTime.Now;
            Registro = registro;
            Username = username;
            Email = email;
            Password = password;
        }
    }

    public class AddPlaceAlertsModel
    {
        public string Placa { get; set; }
        public string Motivo { get; set; }

    }
    public class CameraList
    {
        public List<CameraInfo> Cameras { get; set; }
    }
  
    public class CameraInfo
    {
        public string Name { get; set; }
        public string IpAddress { get; set; }
        public string Status { get; set; }
    }
}
