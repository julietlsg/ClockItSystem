using ClockItSystem.Models;
using ServiceReference;

namespace ClockItSystem.Services
{
    public class NetcashPaymentService
    {
        private readonly IConfiguration _configuration;
        private readonly NetcashBatchGenerator _netcashBatchGenerator;

        // Netcash is asynchronous.
        // We poll the upload report instead of immediately assuming
        // that receiving a file token means the batch was successful.
        private const int MaxReportAttempts = 10;
        private const int ReportRetryDelayMilliseconds = 3000;

        public NetcashPaymentService(
            IConfiguration configuration,
            NetcashBatchGenerator netcashBatchGenerator)
        {
            _configuration = configuration;
            _netcashBatchGenerator = netcashBatchGenerator;
        }

        public async Task<NetcashSubmissionResult> SubmitSalaryBatchAsync(
            StipendPaymentRun paymentRun)
        {
            if (paymentRun == null)
                throw new ArgumentNullException(nameof(paymentRun));

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

            var serviceKey =
                _configuration["Netcash:SalaryServiceKey"];

            if (string.IsNullOrWhiteSpace(serviceKey))
            {
                throw new InvalidOperationException(
                    "Netcash Salary Service Key has not been configured.");
            }

            /*
             * IMPORTANT
             *
             * If Netcash has already returned a file token, we must
             * NOT upload the batch again.
             *
             * The token means Netcash accepted the request for
             * asynchronous processing. We only need to poll for the
             * resulting upload report.
             */

            if (!string.IsNullOrWhiteSpace(paymentRun.NetcashFileToken))
            {
                var existingToken =
                    paymentRun.NetcashFileToken.Trim();

                if (string.Equals(
                        paymentRun.NetcashUploadStatus,
                        "Successful",
                        StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        "This payment run has already been successfully submitted to Netcash.");
                }

                if (string.Equals(
                        paymentRun.NetcashUploadStatus,
                        "SuccessfulWithErrors",
                        StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        "This payment run has already been processed by Netcash with errors. " +
                        "Review the Netcash upload report before submitting again.");
                }

                return await PollUploadReportAsync(
                    serviceKey,
                    existingToken);
            }

            /*
             * No existing token.
             *
             * This is a genuine first submission, so generate and
             * upload the Netcash batch.
             */

            var fileContent =
                _netcashBatchGenerator.Generate(
                    paymentRun,
                    serviceKey);

            if (string.IsNullOrWhiteSpace(fileContent))
            {
                throw new InvalidOperationException(
                    "The Netcash batch file is empty.");
            }

            NIWS_NIFClient? client = null;

            try
            {
                client =
                    new NIWS_NIFClient(
                        NIWS_NIFClient.EndpointConfiguration
                            .BasicHttpBinding_INIWS_NIF);

                var uploadResponse =
                    await client.BatchFileUploadAsync(
                        serviceKey,
                        fileContent);

                if (string.IsNullOrWhiteSpace(uploadResponse))
                {
                    throw new InvalidOperationException(
                        "Netcash returned an empty response when uploading the batch.");
                }

                var fileToken =
                    uploadResponse.Trim();

                /*
                 * Netcash returns numeric error codes when the web
                 * service call itself fails.
                 *
                 * 100 = authentication failure
                 * 102 = parameter error
                 * 200 = general exception
                 */
                if (IsNetcashError(
                        fileToken,
                        out var errorCode))
                {
                    throw new InvalidOperationException(
                        $"Netcash rejected the batch upload. " +
                        $"Error code: {errorCode}. " +
                        $"Response: {fileToken}");
                }

                /*
                 * The file token means the batch has entered
                 * Netcash's asynchronous processing queue.
                 *
                 * Now poll for the actual load report.
                 */
                return await PollUploadReportAsync(
                    serviceKey,
                    fileToken);
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
                if (client != null)
                {
                    try
                    {
                        client.Close();
                    }
                    catch
                    {
                        try
                        {
                            client.Abort();
                        }
                        catch
                        {
                            // Ignore cleanup errors.
                        }
                    }
                }
            }
        }

        private async Task<NetcashSubmissionResult> PollUploadReportAsync(
            string serviceKey,
            string fileToken)
        {
            if (string.IsNullOrWhiteSpace(serviceKey))
            {
                throw new InvalidOperationException(
                    "Netcash Salary Service Key has not been configured.");
            }

            if (string.IsNullOrWhiteSpace(fileToken))
            {
                throw new InvalidOperationException(
                    "A Netcash file token is required to retrieve the upload report.");
            }

            string lastResponse = string.Empty;

            for (var attempt = 1;
                 attempt <= MaxReportAttempts;
                 attempt++)
            {
                NIWS_NIFClient? client = null;

                try
                {
                    client =
                        new NIWS_NIFClient(
                            NIWS_NIFClient.EndpointConfiguration
                                .BasicHttpBinding_INIWS_NIF);

                    var uploadReport =
                        await client.RequestFileUploadReportAsync(
                            serviceKey,
                            fileToken);

                    lastResponse =
                        uploadReport?.Trim() ?? string.Empty;

                    /*
                     * Netcash explicitly returns FILE NOT READY while
                     * the asynchronous report is still being generated.
                     */
                    if (string.Equals(
                            lastResponse,
                            "FILE NOT READY",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        if (attempt < MaxReportAttempts)
                        {
                            await Task.Delay(
                                ReportRetryDelayMilliseconds);

                            continue;
                        }

                        /*
                         * The batch has a valid file token, but Netcash
                         * has not produced the report yet.
                         *
                         * This is NOT a failed upload.
                         */
                        return new NetcashSubmissionResult
                        {
                            FileToken = fileToken,
                            UploadStatus = "Pending",
                            UploadReport = lastResponse,
                            ReportedAt = DateTime.Now
                        };
                    }

                    if (string.IsNullOrWhiteSpace(lastResponse))
                    {
                        if (attempt < MaxReportAttempts)
                        {
                            await Task.Delay(
                                ReportRetryDelayMilliseconds);

                            continue;
                        }

                        return new NetcashSubmissionResult
                        {
                            FileToken = fileToken,
                            UploadStatus = "Unknown",
                            UploadReport =
                                "Netcash returned an empty upload report.",
                            ReportedAt = DateTime.Now
                        };
                    }

                    if (IsNetcashError(
                            lastResponse,
                            out var errorCode))
                    {
                        throw new InvalidOperationException(
                            $"Netcash returned an error while retrieving " +
                            $"the upload report. Error code: {errorCode}. " +
                            $"Response: {lastResponse}");
                    }

                    var reportResult =
                        ParseUploadReport(lastResponse);

                    return new NetcashSubmissionResult
                    {
                        FileToken = fileToken,
                        UploadStatus = reportResult.Status,
                        UploadReport = lastResponse,
                        ReportedAt = DateTime.Now
                    };
                }
                catch (InvalidOperationException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    if (attempt >= MaxReportAttempts)
                    {
                        throw new InvalidOperationException(
                            "An error occurred while retrieving the " +
                            "Netcash upload report: " +
                            ex.Message,
                            ex);
                    }

                    await Task.Delay(
                        ReportRetryDelayMilliseconds);
                }
                finally
                {
                    if (client != null)
                    {
                        try
                        {
                            client.Close();
                        }
                        catch
                        {
                            try
                            {
                                client.Abort();
                            }
                            catch
                            {
                                // Ignore cleanup errors.
                            }
                        }
                    }
                }
            }

            return new NetcashSubmissionResult
            {
                FileToken = fileToken,
                UploadStatus = "Pending",
                UploadReport =
                    string.IsNullOrWhiteSpace(lastResponse)
                        ? "FILE NOT READY"
                        : lastResponse,
                ReportedAt = DateTime.Now
            };
        }

        private static NetcashUploadReportResult ParseUploadReport(
            string uploadReport)
        {
            if (string.IsNullOrWhiteSpace(uploadReport))
            {
                return new NetcashUploadReportResult
                {
                    Status = "Unknown"
                };
            }

            var normalizedReport =
                uploadReport
                    .Replace("\r\n", "\n")
                    .Replace('\r', '\n');

            var lines =
                normalizedReport.Split(
                    '\n',
                    StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var trimmedLine =
                    line.Trim();

                if (!trimmedLine.StartsWith(
                        "###BEGIN",
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                /*
                 * Check SUCCESSFUL WITH ERRORS before SUCCESSFUL.
                 * "SUCCESSFUL WITH ERRORS" contains the word
                 * "SUCCESSFUL".
                 */
                if (trimmedLine.Contains(
                        "SUCCESSFUL WITH ERRORS",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return new NetcashUploadReportResult
                    {
                        Status = "SuccessfulWithErrors",
                        ErrorLines =
                            ExtractReportErrors(lines)
                    };
                }

                if (trimmedLine.Contains(
                        "UNSUCCESSFUL",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return new NetcashUploadReportResult
                    {
                        Status = "Unsuccessful",
                        ErrorLines =
                            ExtractReportErrors(lines)
                    };
                }

                if (trimmedLine.Contains(
                        "SUCCESSFUL",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return new NetcashUploadReportResult
                    {
                        Status = "Successful",
                        ErrorLines =
                            ExtractReportErrors(lines)
                    };
                }
            }

            return new NetcashUploadReportResult
            {
                Status = "Unknown",
                ErrorLines =
                    lines.ToList()
            };
        }

        private static List<string> ExtractReportErrors(
            string[] lines)
        {
            var errors =
                new List<string>();

            foreach (var line in lines)
            {
                var trimmedLine =
                    line.Trim();

                if (string.IsNullOrWhiteSpace(trimmedLine))
                    continue;

                if (trimmedLine.StartsWith(
                        "###BEGIN",
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (trimmedLine.StartsWith(
                        "###END",
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                errors.Add(trimmedLine);
            }

            return errors;
        }

        private static bool IsNetcashError(
            string response,
            out string errorCode)
        {
            errorCode = string.Empty;

            if (string.IsNullOrWhiteSpace(response))
                return false;

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
                return false;

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
                errorCode =
                    code.ToString();

                return true;
            }

            return false;
        }
    }

    public class NetcashSubmissionResult
    {
        public string FileToken { get; set; } = string.Empty;

        public string UploadStatus { get; set; } = "Unknown";

        public string UploadReport { get; set; } = string.Empty;

        public DateTime ReportedAt { get; set; }
    }

    internal class NetcashUploadReportResult
    {
        public string Status { get; set; } = "Unknown";

        public List<string> ErrorLines { get; set; } =
            new();
    }
}