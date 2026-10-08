using System.Globalization;
using ClockItSystem.Models;

namespace ClockItSystem.Services
{
    public class NetcashBatchGenerator
    {
        private const string FileVersion = "1";

        private const string SoftwareVendorKey =
            "24ade73c-98cf-47b3-99be-cc7b867b3080";

        public string Generate(
            StipendPaymentRun paymentRun,
            string serviceKey)
        {
            if (paymentRun == null)
                throw new ArgumentNullException(nameof(paymentRun));

            if (paymentRun.Client == null)
                throw new InvalidOperationException(
                    "The payment run client could not be loaded.");

            if (paymentRun.Payments == null ||
                !paymentRun.Payments.Any())
            {
                throw new InvalidOperationException(
                    "The payment run contains no payments.");
            }

            if (string.IsNullOrWhiteSpace(serviceKey))
            {
                throw new InvalidOperationException(
                    "Netcash Service Key has not been configured.");
            }

            ValidatePaymentDate(paymentRun.PaymentDate);

            var batchName = BuildBatchName(paymentRun);

            var lines = new List<string>();

            // ========================================================
            // HEADER
            // ========================================================

            lines.Add(string.Join(
                "\t",
                "H",
                serviceKey,
                FileVersion,
                "DatedSalaries",
                batchName,
                paymentRun.PaymentDate.ToString(
                    "yyyyMMdd",
                    CultureInfo.InvariantCulture),
                SoftwareVendorKey));

            // ========================================================
            // KEY
            // ========================================================

            lines.Add(string.Join(
                "\t",
                "K",
                "101",
                "102",
                "131",
                "132",
                "133",
                "134",
                "135",
                "136",
                "162",
                "252"));

            long totalAmountInCents = 0;
            var transactionCount = 0;

            // ========================================================
            // TRANSACTIONS
            // ========================================================

            foreach (var payment in paymentRun.Payments
                         .OrderBy(x => x.Id))
            {
                var accountReference =
                    BuildAccountReference(payment);

                var accountName =
                    ValidateAndTrim(
                        paymentRun.Client.Name,
                        25,
                        "Client name");

                var accountHolderName =
                    ValidateAndTrim(
                        payment.AccountHolderName,
                        30,
                        "Account holder name");

                var accountType =
                    MapAccountType(payment.AccountType);

                var branchCode =
                    NormalizeBranchCode(payment.BranchCode);

                var accountNumber =
                    ValidateAccountNumber(
                        payment.AccountNumber);

                var amountInCents =
                    ConvertToCents(payment.StipendAmount);

                var statementReference =
                    BuildStatementReference(
                        payment.Student?.StudentNumber,
                        paymentRun.PeriodFrom);

                lines.Add(string.Join(
                    "\t",
                    "T",
                    accountReference,
                    accountName,
                    "1",
                    accountHolderName,
                    accountType,
                    branchCode,
                    "0",
                    accountNumber,
                    amountInCents.ToString(
                        CultureInfo.InvariantCulture),
                    statementReference));

                transactionCount++;

                totalAmountInCents += amountInCents;
            }

            // ========================================================
            // FOOTER
            // ========================================================

            lines.Add(string.Join(
                "\t",
                "F",
                transactionCount.ToString(
                    CultureInfo.InvariantCulture),
                totalAmountInCents.ToString(
                    CultureInfo.InvariantCulture),
                "9999"));

            return string.Join(
                Environment.NewLine,
                lines);
        }

        private static void ValidatePaymentDate(
            DateTime paymentDate)
        {
            var date = paymentDate.Date;

            if (date == DateTime.MinValue.Date)
            {
                throw new InvalidOperationException(
                    "The Netcash payment date has not been set.");
            }

            if (date.DayOfWeek == DayOfWeek.Saturday ||
                date.DayOfWeek == DayOfWeek.Sunday)
            {
                throw new InvalidOperationException(
                    "The Netcash payment date must be a weekday.");
            }
        }

        private static string BuildBatchName(
            StipendPaymentRun paymentRun)
        {
            var clientName =
                paymentRun.Client?.Name ?? "CLIENT";

            var safeClientName =
                new string(
                    clientName
                        .Where(char.IsLetterOrDigit)
                        .ToArray());

            if (string.IsNullOrWhiteSpace(safeClientName))
                safeClientName = "CLIENT";

            var batchName =
                $"CLOCKIT_{safeClientName}_{paymentRun.PaymentDate:yyyyMMdd}";

            return batchName.Length <= 50
                ? batchName
                : batchName[..50];
        }

        private static string BuildAccountReference(
            StipendPayment payment)
        {
            var reference =
                payment.NetcashAccountReference;

            if (string.IsNullOrWhiteSpace(reference))
            {
                reference =
                    payment.Student?.StudentNumber;
            }

            if (string.IsNullOrWhiteSpace(reference))
            {
                throw new InvalidOperationException(
                    $"Payment {payment.Id} does not have a Netcash account reference or student number.");
            }

            reference = reference.Trim();

            if (reference.Length < 2 ||
                reference.Length > 22)
            {
                throw new InvalidOperationException(
                    $"Payment {payment.Id} has an invalid Netcash account reference. " +
                    "It must contain between 2 and 22 characters.");
            }

            return reference;
        }

        private static string ValidateAndTrim(
            string? value,
            int maximumLength,
            string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException(
                    $"{fieldName} is required.");
            }

            value = value.Trim();

            if (value.Length > maximumLength)
            {
                throw new InvalidOperationException(
                    $"{fieldName} exceeds the Netcash maximum length of {maximumLength} characters.");
            }

            return value;
        }

        private static string MapAccountType(
            string? accountType)
        {
            if (string.IsNullOrWhiteSpace(accountType))
            {
                throw new InvalidOperationException(
                    "Bank account type is required.");
            }

            var value =
                accountType.Trim();

            if (value is "1" or "2" or "3" or "9")
                return value;

            return value.ToLowerInvariant() switch
            {
                "current" => "1",
                "checking" => "1",
                "current/checking" => "1",
                "cheque" => "1",
                "cheque account" => "1",

                "savings" => "2",
                "saving" => "2",

                "transmission" => "3",

                "public recipient" => "9",
                "public beneficiary" => "9",

                _ => throw new InvalidOperationException(
                    $"Unsupported bank account type '{accountType}'. " +
                    "Expected Current, Cheque, Savings, Transmission or Public Recipient.")
            };
        }

        private static string NormalizeBranchCode(
            string? branchCode)
        {
            if (string.IsNullOrWhiteSpace(branchCode))
            {
                throw new InvalidOperationException(
                    "Branch code is required.");
            }

            var value =
                branchCode.Trim();

            if (!value.All(char.IsDigit))
            {
                throw new InvalidOperationException(
                    $"Branch code '{branchCode}' must contain digits only.");
            }

            if (value.Length > 6)
            {
                throw new InvalidOperationException(
                    $"Branch code '{branchCode}' exceeds 6 digits.");
            }

            return value.PadLeft(6, '0');
        }

        private static string ValidateAccountNumber(
            string? accountNumber)
        {
            if (string.IsNullOrWhiteSpace(accountNumber))
            {
                throw new InvalidOperationException(
                    "Bank account number is required.");
            }

            var value =
                accountNumber.Trim();

            if (!value.All(char.IsDigit))
            {
                throw new InvalidOperationException(
                    "Bank account number must contain digits only.");
            }

            if (value.Length > 11)
            {
                throw new InvalidOperationException(
                    "Bank account number may not exceed 11 digits.");
            }

            return value;
        }

        private static long ConvertToCents(
            decimal amount)
        {
            if (amount <= 0)
            {
                throw new InvalidOperationException(
                    "Payment amount must be greater than zero.");
            }

            return decimal.ToInt64(
                Math.Round(
                    amount * 100m,
                    0,
                    MidpointRounding.AwayFromZero));
        }

        private static string BuildStatementReference(
            string? studentNumber,
            DateTime paymentPeriod)
        {
            if (string.IsNullOrWhiteSpace(studentNumber))
            {
                throw new InvalidOperationException(
                    "Student number is required to create the payment reference.");
            }

            var reference =
                $"CLOCKIT/{studentNumber}/{paymentPeriod:yyyyMM}";

            if (reference.Length < 4 ||
                reference.Length > 20)
            {
                throw new InvalidOperationException(
                    $"Payment reference '{reference}' exceeds the Netcash maximum of 20 characters.");
            }

            return reference;
        }
    }
}