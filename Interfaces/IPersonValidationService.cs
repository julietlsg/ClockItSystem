using static ClockItSystem.Models.Enums.DataEnums;

namespace ClockItSystem.Interfaces
{
    public interface IPersonValidationService
    {
        bool IsValidSouthAfricanId(string? idNumber);

        bool IsValidCellphone(string? cellphone);

        string NormalizeCellphone(string? cellphone);

        bool IsValidEmail(string? email);

        bool IsValidPostalCode(string? postalCode);
    }
}
