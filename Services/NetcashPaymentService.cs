using ClockItSystem.Models;
using ServiceReference;

namespace ClockItSystem.Services
{
    public class NetcashPaymentService
    {
        private readonly IConfiguration _configuration;
        private readonly NetcashBatchGenerator _netcashBatchGenerator;

        public NetcashPaymentService(
            IConfiguration configuration,
            NetcashBatchGenerator netcashBatchGenerator)
        {
            _configuration = configuration;
            _netcashBatchGenerator = netcashBatchGenerator;
        }

        /// <summary>
        /// Generates the Netcash H/K/T/F batch file and
        /// submits it to Netcash using BatchFileUpload.
        ///
        /// A successful response is the Netcash file token.
        /// </summary>
        public async Task<string> SubmitSalaryBatchAsync(
            StipendPaymentRun paymentRun)
        {
            if (paymentRun == null)
            {
                throw new ArgumentNullException(
                    nameof(paymentRun));
            }

            if (paymentRun.Status != "Approved")
            {
                throw new InvalidOperationException(
                    "Only an approved payment run can be submitted to Netcash.");
            }

            if (paymentRun.Payments == null ||
                !paymentRun.Payments.Any())
            {
                throw new InvalidOperationException(
                    "The payment run contains no payments.");
            }

            // --------------------------------------------------------
            // PREVENT DUPLICATE SUBMISSION
            // --------------------------------------------------------

            if (!string.IsNullOrWhiteSpace(
                paymentRun.NetcashFileToken))
            {
                throw new InvalidOperationException(
                    "This payment run has already been submitted to Netcash.");
            }

            // --------------------------------------------------------
            // GET SERVICE KEY
            // --------------------------------------------------------

            var serviceKey =
                _configuration["Netcash:SalaryServiceKey"];

            if (string.IsNullOrWhiteSpace(serviceKey))
            {
                throw new InvalidOperationException(
                    "Netcash Salary Service Key has not been configured.");
            }

            // --------------------------------------------------------
            // GENERATE H/K/T/F FILE
            // --------------------------------------------------------

            var fileContent =
                _netcashBatchGenerator.Generate(
                    paymentRun,
                    serviceKey);

            NIWS_NIFClient? client = null;

            try
            {
                // ----------------------------------------------------
                // CREATE NETCASH CLIENT
                // ----------------------------------------------------

                client =
                    new NIWS_NIFClient(
                        NIWS_NIFClient.EndpointConfiguration
                            .BasicHttpBinding_INIWS_NIF);

                // ----------------------------------------------------
                // UPLOAD BATCH
                // ----------------------------------------------------

                var response =
                    await client.BatchFileUploadAsync(
                        serviceKey,
                        fileContent);

                if (string.IsNullOrWhiteSpace(response))
                {
                    throw new InvalidOperationException(
                        "Netcash returned an empty response when uploading the batch.");
                }

                var result = response.Trim();

                // ----------------------------------------------------
                // CHECK DOCUMENTED NETCASH ERROR CODES
                //
                // 100 = Authentication failure
                // 101 = Date format error
                // 102 = Parameter error
                // 200 = General exception
                // ----------------------------------------------------

                if (IsNetcashError(
                    result,
                    out var errorCode))
                {
                    throw new InvalidOperationException(
                        $"Netcash rejected the batch upload. " +
                        $"Error code: {errorCode}. " +
                        $"Response: {result}");
                }

                // ----------------------------------------------------
                // SUCCESS
                //
                // Netcash returns the file token.
                // ----------------------------------------------------

                return result;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "An error occurred while communicating with Netcash: " +
                    ex.Message,
                    ex);
            }
            finally
            {
                // ----------------------------------------------------
                // CLOSE WCF CLIENT
                // ----------------------------------------------------

                if (client != null)
                {
                    try
                    {
                        client.Close();
                    }
                    catch
                    {
                        client.Abort();
                    }
                }
            }
        }

        // ============================================================
        // NETCASH ERROR DETECTION
        // ============================================================

        private static bool IsNetcashError(
            string response,
            out string errorCode)
        {
            errorCode = string.Empty;

            var firstPart =
                response
                    .Split(
                        new[]
                        {
                            '\t',
                            '|',
                            ',',
                            ';',
                            ':',
                            ' ',
                            '\r',
                            '\n'
                        },
                        StringSplitOptions.RemoveEmptyEntries)
                    .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(firstPart))
            {
                return false;
            }

            if (!int.TryParse(
                firstPart,
                out var code))
            {
                return false;
            }

            if (code == 100 ||
                code == 101 ||
                code == 102 ||
                code == 200)
            {
                errorCode = code.ToString();

                return true;
            }

            return false;
        }
    }
}