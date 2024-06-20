using System.ComponentModel.DataAnnotations;
using CpfCnpjLibrary;
using F.Enum;
using F.Models;

public class CpfCnpjAttribute : ValidationAttribute
{
    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
        {
            return new ValidationResult("O campo Registro é obrigatório.");
        }

        var registro = value.ToString();
        var model = (RegisterViewModel)validationContext.ObjectInstance;

        if (Cpf.Validar(registro))
        {
            model.RegistroType = CadastroTypeEnum.PF;
            return ValidationResult.Success;
        }
        else if (Cnpj.Validar(registro))
        {
            model.RegistroType = CadastroTypeEnum.PJ;
            return ValidationResult.Success;
        }
        else
        {
            return new ValidationResult("O CPF ou CNPJ informado é inválido.");
        }
    }
}
