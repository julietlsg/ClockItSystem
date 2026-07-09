using ClockItSystem.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using static ClockItSystem.Models.Enums.DataEnums;

namespace ClockItSystem.Services.Validation
{
    public class PersonValidationService : IPersonValidationService
    {
        public bool IsValidSouthAfricanId(string? idNumber)
        {
            if (string.IsNullOrWhiteSpace(idNumber))
                return false;

            idNumber = idNumber.Trim();

            // Must be exactly 13 digits
            return idNumber.Length == 13 &&
                   idNumber.All(char.IsDigit);
        }
        public bool IsValidCellphone(string? cellphone)
        {
            cellphone = NormalizeCellphone(cellphone);

            return Regex.IsMatch(
                cellphone,
                @"^0[6-8][0-9]{8}$");
        }

        public string NormalizeCellphone(string? cellphone)
        {
            if (string.IsNullOrWhiteSpace(cellphone))
                return string.Empty;

            cellphone = cellphone
                .Replace(" ", "")
                .Replace("-", "");

            if (cellphone.StartsWith("+27"))
                cellphone = "0" + cellphone.Substring(3);

            return cellphone;
        }

        public bool IsValidEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            return new EmailAddressAttribute()
                .IsValid(email);
        }

        public bool IsValidPostalCode(string? postalCode)
        {
            if (string.IsNullOrWhiteSpace(postalCode))
                return false;

            return Regex.IsMatch(
                postalCode,
                @"^\d{4}$");
        }

        private bool PassesLuhn(string id)
        {
            int sum = 0;
            bool alternate = false;

            for (int i = id.Length - 1; i >= 0; i--)
            {
                int n = id[i] - '0';

                if (alternate)
                {
                    n *= 2;

                    if (n > 9)
                        n -= 9;
                }

                sum += n;

                alternate = !alternate;
            }

            return sum % 10 == 0;
        }
    }
}